namespace AvroConvert.Write.Resolvers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Exceptions;
    using Generic;
    using Schema;

    public static class Factory
    {
        private static readonly Array Array;
        private static readonly Map Map;
        private static readonly Null Null;

        static Factory()
        {
            Array = new Array();
            Null = new Null();
            Map = new Map();
        }
        public static Encoder.WriteItem ResolveWriter(Schema schema)
        {
            switch (schema.Tag)
            {
                case Schema.Type.Null:
                    return Null.Resolve;
                case Schema.Type.Boolean:
                    return (v, e) => Write<bool>(v, schema.Tag, e.WriteBoolean);
                case Schema.Type.Int:
                    return (v, e) => Write<int>(v, schema.Tag, e.WriteInt);
                case Schema.Type.Long:
                    return (v, e) => Write<long>(v, schema.Tag, e.WriteLong);
                case Schema.Type.Float:
                    return (v, e) => Write<float>(v, schema.Tag, e.WriteFloat);
                case Schema.Type.Double:
                    return (v, e) => Write<double>(v, schema.Tag, e.WriteDouble);
                case Schema.Type.String:
                    return (v, e) => WriteString(v, e.WriteString);
                case Schema.Type.Bytes:
                    return (v, e) => Write<byte[]>(v, schema.Tag, e.WriteBytes);
                case Schema.Type.Error:
                case Schema.Type.Record:
                    return ResolveRecord((RecordSchema)schema);
                case Schema.Type.Enumeration:
                    return ResolveEnum(schema as EnumSchema);
                case Schema.Type.Fixed:
                    return (v, e) => WriteFixed(schema as FixedSchema, v, e);
                case Schema.Type.Array:
                    return Array.Resolve((ArraySchema)schema);
                case Schema.Type.Map:
                    return Map.Resolve((MapSchema)schema);
                case Schema.Type.Union:
                    return ResolveUnion((UnionSchema)schema);
                default:
                    return (v, e) => throw new AvroTypeMismatchException($"Tried to write against [{schema}] schema, but found [{v.GetType()}] type");
            }
        }

  

        /// <summary>
        /// A generic method to serialize primitive Avro types.
        /// </summary>
        /// <typeparam name="S">Type of the C# type to be serialized</typeparam>
        /// <param name="value">The value to be serialized</param>
        /// <param name="tag">The schema type tag</param>
        /// <param name="writer">The writer which should be used to write the given type.</param>
        private static void Write<S>(object value, Schema.Type tag, Writer<S> writer)
        {
            if (value == null)
            {
                value = default(S);
            }

            if (!(value is S)) throw new AvroTypeMismatchException($"[{ typeof(S)}] required to write against [{tag.ToString()}] schema but found " + value.GetType());

            writer((S)value);
        }

        private static void WriteString(object value, Writer<string> writer)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value is Guid)
            {
                value = value.ToString();
            }

            writer((string)value);
        }


        /// <summary>
        /// Serialized a record using the given RecordSchema. It uses GetField method
        /// to extract the field value from the given object.
        /// </summary>
        /// <param name="schema">The RecordSchema to use for serialization</param>
        private static Encoder.WriteItem ResolveRecord(RecordSchema recordSchema)
        {
            Encoder.WriteItem recordResolver;

            var writeSteps = new Record[recordSchema.Fields.Count];
            recordResolver = (v, e) => WriteRecordFields(v, writeSteps, e, recordSchema);


            int index = 0;
            foreach (Field field in recordSchema)
            {
                var record = new Record
                {
                    WriteField = ResolveWriter(field.Schema),
                    Field = field
                };
                writeSteps[index++] = record;
            }

            return recordResolver;
        }


        public static Dictionary<string, object> SplitKeyValues(object item)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (item == null)
            {
                return result;
            }

            Type objType = item.GetType();
            PropertyInfo[] properties = objType.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    // We have a List<T> or array
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    //TODO make soft get value as method
                    if (value != null)
                    {
                        result.Add(prop.Name, GetSplittedList((IList)value));
                    }
                    else
                    {
                        result.Add(prop.Name, null);
                    }

                }
                else if (prop.PropertyType == typeof(Guid))
                {
                    // We have a simple type
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    result.Add(prop.Name, value.ToString());
                }
                else if (prop.PropertyType.GetTypeInfo().IsValueType ||
                         prop.PropertyType == typeof(string))
                {
                    // We have a simple type
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    result.Add(prop.Name, value);
                }
                else
                {
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }

                    if (value != null)
                    {
                        result.Add(prop.Name, SplitKeyValues(value));
                    }
                    else
                    {
                        result.Add(prop.Name, null);
                    }

                }
            }

            return result;
        }

        static IList GetSplittedList(IList list)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var typeToCheck = list.GetType().GetProperties()[2].PropertyType;

            if (typeToCheck.GetTypeInfo().IsValueType ||
                typeToCheck == typeof(string))
            {
                return list;
            }

            List<object> result = new List<object>();

            foreach (var item in list)
            {
                result.Add(item != null ? SplitKeyValues(item) : null);
            }

            return result;
        }

        public static void WriteRecordFields(object recordObj, Record[] writers, IWriter encoder, RecordSchema schema)
        {
            GenericRecord record = new GenericRecord(schema);

            if (recordObj is Dictionary<string, object> obj)
            {
                record.contents = obj;
            }

            else
            {
                record.contents = SplitKeyValues(recordObj);
            }

            foreach (var writer in writers)
            {
                writer.WriteField(record[writer.Field.Name], encoder);
            }
        }

        public static void EnsureRecordObject(RecordSchema recordSchema, object value)
        {
            if (value == null || !(value is GenericRecord) || !((value as GenericRecord).Schema.Equals(recordSchema)))
            {
                throw new AvroTypeMismatchException("[GenericRecord] required to write against [Record] schema but found " + value.GetType());
            }
        }

        public static void WriteField(object record, string fieldName, int fieldPos, Encoder.WriteItem writer,
            IWriter encoder)
        {
            writer(((GenericRecord)record)[fieldName], encoder);
        }

        public static Encoder.WriteItem ResolveEnum(EnumSchema es)
        {
            return (value, e) =>
            {
                if (value == null || !(value is GenericEnum) || !((value as GenericEnum).Schema.Equals(es)))
                    throw new AvroTypeMismatchException("[GenericEnum] required to write against [Enum] schema but found " + value.GetType());
                e.WriteEnum(es.Ordinal((value as GenericEnum).Value));
            };
        }

        public static void WriteFixed(FixedSchema es, object value, IWriter encoder)
        {
            if (value == null || !(value is GenericFixed) || !(value as GenericFixed).Schema.Equals(es))
            {
                throw new AvroTypeMismatchException("[GenericFixed] required to write against [Fixed] schema but found " + value.GetType());
            }

            GenericFixed ba = (GenericFixed)value;
            encoder.WriteFixed(ba.Value);
        }

        /*
         * FIXME: This method of determining the Union branch has problems. If the data is IDictionary<string, object>
         * if there are two branches one with record schema and the other with map, it choose the first one. Similarly if
         * the data is byte[] and there are fixed and bytes schemas as branches, it choose the first one that matches.
         * Also it does not recognize the arrays of primitive types.
         */
        public static bool UnionBranchMatches(Schema sc, object obj)
        {
            if (obj == null && sc.Tag != Schema.Type.Null) return false;
            switch (sc.Tag)
            {
                case Schema.Type.Null:
                    return obj == null;
                case Schema.Type.Boolean:
                    return obj is bool;
                case Schema.Type.Int:
                    return obj is int;
                case Schema.Type.Long:
                    return obj is long;
                case Schema.Type.Float:
                    return obj is float;
                case Schema.Type.Double:
                    return obj is double;
                case Schema.Type.Bytes:
                    return obj is byte[];
                case Schema.Type.String:
                    return obj is string;
                case Schema.Type.Error:
                case Schema.Type.Record:
                    //return obj is GenericRecord && (obj as GenericRecord)._schema.Equals(s);
                    return obj is GenericRecord &&
                           (obj as GenericRecord).Schema.SchemaName.Equals((sc as RecordSchema).SchemaName);
                case Schema.Type.Enumeration:
                    //return obj is GenericEnum && (obj as GenericEnum)._schema.Equals(s);
                    return obj is GenericEnum &&
                           (obj as GenericEnum).Schema.SchemaName.Equals((sc as EnumSchema).SchemaName);
                case Schema.Type.Array:
                    return obj is System.Array && !(obj is byte[]);
                case Schema.Type.Map:
                    return obj is IDictionary<string, object>;
                case Schema.Type.Union:
                    return false; // Union directly within another union not allowed!
                case Schema.Type.Fixed:
                    //return obj is GenericFixed && (obj as GenericFixed)._schema.Equals(s);
                    return obj is GenericFixed &&
                           (obj as GenericFixed).Schema.SchemaName.Equals((sc as FixedSchema).SchemaName);
                default:
                    throw new AvroException("Unknown schema type: " + sc.Tag);
            }
        }






        private static Encoder.WriteItem ResolveUnion(UnionSchema unionSchema)
        {
            var branchSchemas = unionSchema.Schemas.ToArray();
            var branchWriters = new Encoder.WriteItem[branchSchemas.Length];
            int branchIndex = 0;
            foreach (var branch in branchSchemas)
            {
                branchWriters[branchIndex++] = ResolveWriter(branch);
            }


            return (v, e) => WriteUnion(unionSchema, branchSchemas, branchWriters, v, e);
        }

        /// <summary>
        /// Resolves the given value against the given UnionSchema and serializes the object against
        /// the resolved schema member.
        /// </summary>
        /// <param name="us">The UnionSchema to resolve against</param>
        /// <param name="value">The value to be serialized</param>
        /// <param name="encoder">The encoder for serialization</param>
        private static void WriteUnion(UnionSchema unionSchema, Schema[] branchSchemas, Encoder.WriteItem[] branchWriters, object value, IWriter encoder)
        {
            int index = ResolveUnion(unionSchema, branchSchemas, value);
            encoder.WriteUnionIndex(index);
            branchWriters[index](value, encoder);
        }

        /// <summary>
        /// Finds the branch within the given UnionSchema that matches the given object. The default implementation
        /// calls Matches() method in the order of branches within the UnionSchema. If nothing matches, throws
        /// an exception.
        /// </summary>
        /// <param name="us">The UnionSchema to resolve against</param>
        /// <param name="obj">The object that should be used in matching</param>
        /// <returns></returns>
        private static int ResolveUnion(UnionSchema us, Schema[] branchSchemas, object obj)
        {
            for (int i = 0; i < branchSchemas.Length; i++)
            {
                if (UnionBranchMatches(branchSchemas[i], obj)) return i;
            }
            throw new AvroException("Cannot find a match for " + obj.GetType() + " in " + us);
        }
    }
}
