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
        private static byte[] SerializeToArray(object obj, CodecType codecType, AvroConvertOptions options)
        {
            using var resultStream = new MemoryStream();
            SerializeTo(obj, resultStream, codecType, options);
            return resultStream.ToArray();
        }

        /// <summary>Writes an Avro container with <paramref name="obj"/> to <paramref name="output"/>, leaving the stream open.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeTo(object obj, Stream output, CodecType codecType, AvroConvertOptions options)
        {
            var itemType = options == null || options.CollectionMode == AvroCollectionMode.Entries ? EntryItemType(obj) : null;
            if (itemType != null)
            {
                SerializeEntries((IEnumerable)obj, itemType, output, codecType, options);
                return;
            }

            var schema = Schema.Create(obj, options);
            using var writer = new Encoder(schema, output, codecType, options, leaveOpen: true);
            writer.Append(obj);
        }

        /// <summary>
        /// Writes every element of a top-level collection as its own container entry (schema = element schema).
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeEntries(IEnumerable items, Type itemType, Stream output, CodecType codecType, AvroConvertOptions options)
        {
            var schema = itemType == typeof(ExpandoObject)
                ? new JsonSchemaBuilder().BuildSchema(items.Cast<ExpandoObject>())
                : Schema.Create(itemType, options);
            using var writer = new Encoder(schema, output, codecType, options, leaveOpen: true);
            EntryAppenders.GetOrAdd(itemType, CompileAppender)(writer, items);
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

        /// <summary>Element type of a generic collection suitable for entry-wise serialization; null otherwise.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static Type EntryItemType(object obj)
        {
            if (obj is not IEnumerable || obj is string || obj is byte[] || obj is IDictionary)
            {
                return null;
            }

            var itemType = ReadCompiler.CollectionItemType(obj.GetType());
            if (itemType == null || itemType == typeof(object) || itemType.IsInterface || itemType.IsAbstract)
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
            return Serialize(obj, CodecType.Null);
        }
        
        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using the specified codec type.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="codecType">The codec type to be used for serialization. Choosing a specific codec may reduce the output size.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
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
    }
}
