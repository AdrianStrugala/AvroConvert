using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>Loop and dynamic-dispatch helpers called from compiled write plans.</summary>
    internal static class WriteHelpers
    {
        internal static void WriteList<T>(Writer writer, IReadOnlyList<T> items, Action<Writer, T> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                writer.WriteItemCount(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    writeItem(writer, items[i]);
                }
            }
            writer.WriteArrayEnd();
        }

        internal static void WriteEnumerable<T>(Writer writer, IEnumerable<T> items, Action<Writer, T> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                if (items is IReadOnlyCollection<T> readOnly)
                {
                    writer.WriteItemCount(readOnly.Count);
                }
                else if (items is ICollection<T> collection)
                {
                    writer.WriteItemCount(collection.Count);
                }
                else
                {
                    // Unknown count: Avro needs the block size before its items.
                    items = new List<T>(items);
                    writer.WriteItemCount(((List<T>)items).Count);
                }

                foreach (var item in items)
                {
                    writeItem(writer, item);
                }
            }
            writer.WriteArrayEnd();
        }

        /// <summary>Non-generic enumerable (object[], ArrayList, JArray-derived arrays): items dispatched by runtime type.</summary>
        internal static void WriteUntypedEnumerable(Writer writer, IEnumerable items, Action<Writer, object> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                if (items is not ICollection collection)
                {
                    var buffered = new List<object>();
                    foreach (var item in items) buffered.Add(item);
                    collection = buffered;
                }

                writer.WriteItemCount(collection.Count);
                foreach (var item in collection)
                {
                    writeItem(writer, item);
                }
            }
            writer.WriteArrayEnd();
        }

        internal static void WriteMap<TKey, TValue>(Writer writer, IEnumerable<KeyValuePair<TKey, TValue>> entries, int count, Action<Writer, TValue> writeValue)
        {
            writer.WriteMapStart();
            writer.WriteItemCount(count);
            foreach (var entry in entries)
            {
                writer.WriteString(entry.Key is string s ? s : entry.Key.ToString());
                writeValue(writer, entry.Value);
            }
            writer.WriteMapEnd();
        }

        internal static void WriteUntypedMap(Writer writer, IDictionary map, Action<Writer, object> writeValue)
        {
            writer.WriteMapStart();
            if (map != null)
            {
                writer.WriteItemCount(map.Count);
                foreach (DictionaryEntry entry in map)
                {
                    writer.WriteString(entry.Key.ToString());
                    writeValue(writer, entry.Value);
                }
            }
            writer.WriteMapEnd();
        }

        /// <summary>Record given as a property bag (ExpandoObject / IDictionary&lt;string, object&gt;): fields looked up by name.</summary>
        internal static void WriteDictionaryRecord(Writer writer, IDictionary<string, object> values, string[] fieldNames, Action<Writer, object>[] fieldWriters)
        {
            for (int i = 0; i < fieldNames.Length; i++)
            {
                object value = null;
                if (values != null && !values.TryGetValue(fieldNames[i], out value))
                {
                    foreach (var pair in values)
                    {
                        if (string.Equals(pair.Key, fieldNames[i], StringComparison.OrdinalIgnoreCase))
                        {
                            value = pair.Value;
                            break;
                        }
                    }
                }

                fieldWriters[i](writer, value);
            }
        }

        /// <summary>Record given as a Newtonsoft JObject: values unwrapped to CLR primitives, arrays to object[].</summary>
        internal static void WriteJsonRecord(Writer writer, JObject json, string[] fieldNames, Action<Writer, object>[] fieldWriters)
        {
            for (int i = 0; i < fieldNames.Length; i++)
            {
                fieldWriters[i](writer, UnwrapJson(json?[fieldNames[i]]));
            }
        }

        internal static object UnwrapJson(JToken token)
        {
            switch (token)
            {
                case null:
                    return null;
                case JValue value:
                    return value.Value;
                case JArray array:
                    var items = new object[array.Count];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = UnwrapJson(array[i]);
                    }
                    return items;
                default:
                    return token;
            }
        }

        internal static void WriteEnumSymbol(Writer writer, string symbol, EnumSchema schema)
        {
            var position = schema.GetSymbolPosition(symbol);
            if (position < 0)
            {
                throw new AvroTypeException($"[Enum] Provided value [{symbol}] is not of the enum [{schema.Name}] members");
            }

            writer.WriteEnum(position);
        }

        /// <summary>True when <paramref name="schema"/> can hold null: the null type or a union with a null branch.</summary>
        internal static bool CanBeNull(TypeSchema schema) =>
            schema.Type == AvroType.Null || schema is UnionSchema union && union.Schemas.Any(s => s.Type == AvroType.Null);

        internal static Exception NullNotAllowed(string message) => new AvroTypeMismatchException(message);

        internal const string NullHint =
            "Declare the member as nullable (string?, int? or [NullableSchema]) so that its schema becomes a union with \"null\", " +
            "or set AvroConvertOptions.NullValueHandling = AvroNullValueHandling.WriteEmpty to write an empty value instead (AvroConvert 3.x behaviour).";

        /// <param name="context">Where the value sits ("User.name", "User.tags[]"); null for the root value.</param>
        internal static string NullMessage(string context, TypeSchema schema) =>
            $"{(context == null ? "The value" : $"Field '{context}'")} is null, but its schema {Describe(schema)} does not allow null. {NullHint}";

        internal static string Describe(TypeSchema schema) => schema switch
        {
            LogicalTypeSchema logical => $"\"{logical.LogicalTypeName}\"",
            NamedSchema named => $"{schema.Type.ToString().ToLowerInvariant()} {named.Name}",
            _ => $"\"{schema.Type.ToString().ToLowerInvariant()}\""
        };

        /// <summary>A null root value (whole object or a collection entry): null branch, error or empty value per the options.</summary>
        internal static void WriteRootNull(TypeSchema schema, Writer writer, AvroConvertOptions options)
        {
            if ((options?.NullValueHandling ?? AvroNullValueHandling.Throw) == AvroNullValueHandling.Throw && !CanBeNull(schema))
            {
                throw NullNotAllowed(NullMessage(null, schema));
            }

            WriteNullValue(schema, writer);
        }

        /// <summary>
        /// Representation of a missing (null) value for a schema: the null branch of a union, otherwise the type's empty value
        /// (empty string/bytes/array/map, zero, false; records field by field). Types without an empty value cannot be null.
        /// </summary>
        internal static void WriteNullValue(TypeSchema schema, Writer writer)
        {
            switch (schema.Type)
            {
                case AvroType.Null:
                    writer.WriteNull();
                    break;
                case AvroType.Union:
                    var union = (UnionSchema)schema;
                    for (int i = 0; i < union.Schemas.Count; i++)
                    {
                        if (union.Schemas[i].Type == AvroType.Null)
                        {
                            writer.WriteUnionIndex(i);
                            return;
                        }
                    }
                    throw NoUnionBranch(null, union);
                case AvroType.Boolean:
                    writer.WriteBoolean(false);
                    break;
                case AvroType.Int:
                    writer.WriteInt(0);
                    break;
                case AvroType.Long:
                    writer.WriteLong(0);
                    break;
                case AvroType.Float:
                    writer.WriteFloat(0);
                    break;
                case AvroType.Double:
                    writer.WriteDouble(0);
                    break;
                case AvroType.String:
                    writer.WriteString(string.Empty);
                    break;
                case AvroType.Bytes:
                    writer.WriteBytes(Array.Empty<byte>());
                    break;
                case AvroType.Array:
                    writer.WriteArrayStart();
                    writer.WriteArrayEnd();
                    break;
                case AvroType.Map:
                    writer.WriteMapStart();
                    writer.WriteMapEnd();
                    break;
                case AvroType.Record:
                case AvroType.Error:
                    foreach (var field in ((RecordSchema)schema).Fields)
                    {
                        WriteNullValue(field.TypeSchema, writer);
                    }
                    break;
                default:
                    throw TypeMismatch(schema, null);
            }
        }

        internal static Exception NoUnionBranch(object value, UnionSchema schema) =>
            new AvroException($"Cannot find a match for {value?.GetType().ToString() ?? "null"} in {schema}");

        internal static Exception TypeMismatch(TypeSchema schema, object value) =>
            new AvroTypeMismatchException($"Tried to write against [{schema}] schema, but found [{value?.GetType().ToString() ?? "null"}] type");
    }
}
