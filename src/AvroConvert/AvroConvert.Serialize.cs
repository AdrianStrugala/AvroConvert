#region license
/**Copyright (c) 2020 Adrian Strugała
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System.ComponentModel;
using System.IO;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read.Typed;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Features.JsonToAvro;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Policies;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static byte[] SerializeToArray(object obj, CodecType codecType, AvroConvertOptions options, TypeSchema writerSchema = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            // A container starts with the schema in its header, so it is rarely small.
            using var output = new Writer(Writer.TargetBufferSize);
            SerializeTo(obj, output, codecType, options, writerSchema, metadata);
            return output.ToArray();
        }

        /// <summary>Writes an Avro container with <paramref name="obj"/> to <paramref name="output"/> and flushes it.</summary>
        /// <param name="writerSchema">The schema given by the caller; null takes it from the value.</param>
        /// <param name="metadata">Additional keys of the header.</param>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeTo(object obj, Writer output, CodecType codecType, AvroConvertOptions options, TypeSchema writerSchema = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            var itemType = options == null || options.CollectionMode == AvroCollectionMode.Entries ? EntryItemType(obj, writerSchema != null) : null;
            if (itemType != null && (writerSchema == null || !ReadCompiler.SchemaHoldsCollection(writerSchema, obj.GetType())))
            {
                SerializeEntries((IEnumerable)obj, itemType, output, codecType, options, writerSchema, metadata);
                return;
            }

            var schema = writerSchema ?? Schema.Create(obj, options);
            using var writer = new Encoder(schema, output, codecType, options, metadata);
            writer.Append(obj);
        }

        /// <summary>
        /// Writes every element of a top-level collection as its own container entry (schema = element schema).
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeEntries(IEnumerable items, Type itemType, Writer output, CodecType codecType, AvroConvertOptions options, TypeSchema writerSchema, IReadOnlyDictionary<string, AvroMetadataValue> metadata)
        {
            var schema = writerSchema;
            if (schema == null)
            {
                var declared = itemType == typeof(ExpandoObject) ? null : Schema.Create(itemType, options);
                if (declared == null || RuntimeSchema.IsOpen(declared))
                {
                    // The schema comes from the items, so they are read before the header: a lazy sequence must run only once.
                    items = Materialize(items, itemType);
                }

                schema = declared == null ? RuntimeSchema.ResolveExpandos(items.Cast<ExpandoObject>(), options) : RuntimeSchema.Resolve(declared, items, options);
            }

            using var writer = new Encoder(schema, output, codecType, options, metadata);
            EntryAppenders.GetOrAdd(itemType, CompileAppender)(writer, items);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static IEnumerable Materialize(IEnumerable items, Type itemType)
        {
            if (items is ICollection)
            {
                return items;
            }

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
            foreach (var item in items)
            {
                list.Add(item);
            }

            return list;
        }

        private static readonly ConcurrentDictionary<Type, Action<Encoder, IEnumerable>> EntryAppenders = new();

        /// <summary>(encoder, items) => encoder.AppendAll&lt;T&gt;((IEnumerable&lt;T&gt;)items) for the runtime element type.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static Action<Encoder, IEnumerable> CompileAppender(Type itemType)
        {
            var encoder = Expression.Parameter(typeof(Encoder), "encoder");
            var items = Expression.Parameter(typeof(IEnumerable), "items");
            var method = typeof(Encoder).GetMethod(nameof(Encoder.AppendAll), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(itemType);
            var call = Expression.Call(encoder, method, Expression.Convert(items, typeof(IEnumerable<>).MakeGenericType(itemType)));
            return Expression.Lambda<Action<Encoder, IEnumerable>>(call, encoder, items).Compile();
        }

        /// <summary>
        /// Element type of a generic collection suitable for entry-wise serialization; null otherwise. An interface or an
        /// abstract class tells nothing about the schema of its items, so it is enough only when the schema is given.
        /// The schema of <c>object</c> items comes from their values.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static Type EntryItemType(object obj, bool schemaGiven)
        {
            if (obj is not IEnumerable || obj is string || obj is byte[] || obj is IDictionary)
            {
                return null;
            }

            var itemType = ReadCompiler.CollectionItemType(obj.GetType());
            if (itemType == null || !schemaGiven && (itemType.IsInterface || itemType.IsAbstract))
            {
                return null;
            }

            return itemType;
        }

        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using a default codec.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Serialize(object obj)
        {
            return SerializeToArray(obj, CodecType.Null, null);
        }
        
        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using the specified codec type.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="codecType">The codec type to be used for serialization. Choosing a specific codec may reduce the output size.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroConvertOptions"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Serialize(object obj, CodecType codecType)
        {
            return SerializeToArray(obj, codecType, null);
        }
        

        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using the specified Avro conversion options.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">The Avro conversion options that control the serialization process, including codec and other settings.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Serialize(object obj, AvroConvertOptions options)
        {
            return SerializeToArray(obj, options.Codec, options);
        }

        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) with the given schema, instead of the
        /// schema of its type. The schema goes to the header unchanged, with the attributes the Avro specification does not
        /// define (e.g. <c>field-id</c> of Apache Iceberg). A collection is written as one entry per item unless the schema is an array.
        /// </summary>
        /// <param name="obj">The object to serialize: a class, an <c>ExpandoObject</c> or a <c>Dictionary&lt;string, object&gt;</c> for a record.</param>
        /// <param name="schema">The schema to write with.</param>
        /// <param name="options">The Avro conversion options that control the serialization process, including codec and other settings.</param>
        /// <param name="metadata">Additional keys of the header of this file, read with <see cref="GetMetadata(byte[])"/>. Text goes in as UTF-8. Keys starting with <c>avro.</c> are reserved by the specification.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        /// <exception cref="ArgumentException">A key of <paramref name="metadata"/> is reserved.</exception>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Serialize(object obj, AvroSchema schema, AvroConvertOptions options = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            ArgumentNullException.ThrowIfNull(schema);
            return SerializeToArray(obj, options?.Codec ?? CodecType.Null, options, schema.TypeSchema, metadata);
        }
    }
}
