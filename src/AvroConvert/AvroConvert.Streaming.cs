#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.JsonToAvro;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Features.Streaming;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Writes <paramref name="items"/> to <paramref name="output"/> as an Avro object container file, one entry per item.
        /// Data blocks are written while enumerating, so the collection is never held in memory.
        /// The stream is left open.
        /// </summary>
        /// <remarks>
        /// The schema is built from <typeparamref name="T"/>. When <typeparamref name="T"/> is <c>object</c>, an interface, an
        /// abstract class or <see cref="ExpandoObject"/>, or it holds <c>object</c> values (e.g. a member of type
        /// <c>Dictionary&lt;string, object&gt;</c>), the schema depends on the items, so they are collected first. Items of
        /// an interface or an abstract class must share one runtime type; <see cref="ExpandoObject"/> items are merged into one schema.
        /// A dictionary, <c>string</c> or <c>byte[]</c> is one value, written like <see cref="Serialize(object, AvroConvertOptions)"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Collected items have different runtime types.</exception>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static void Serialize<T>(IEnumerable<T> items, Stream output, AvroConvertOptions options = null)
        {
            SerializeStream(items, output, null, options, null);
        }

        /// <summary>
        /// Writes <paramref name="items"/> to <paramref name="output"/> as an Avro object container file with the given schema,
        /// one entry per item. Items are never collected: the schema does not depend on them. The stream is left open.
        /// <paramref name="metadata"/> holds additional keys of the header, see <see cref="Serialize(object, AvroSchema, AvroConvertOptions, IReadOnlyDictionary{string, AvroMetadataValue})"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static void Serialize<T>(IEnumerable<T> items, Stream output, AvroSchema schema, AvroConvertOptions options = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            ArgumentNullException.ThrowIfNull(schema);
            SerializeStream(items, output, schema.TypeSchema, options, metadata);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeStream<T>(IEnumerable<T> items, Stream output, TypeSchema writerSchema, AvroConvertOptions options, IReadOnlyDictionary<string, AvroMetadataValue> metadata)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(output);
            var codec = options?.Codec ?? CodecType.Null;
            using var writer = new Writer(output);

            if (IsSingleValue(items))
            {
                SerializeTo(items, writer, codec, options, writerSchema, metadata);
                return;
            }

            if (writerSchema == null && !HasStreamableSchema(typeof(T), options))
            {
                SerializeCollected(items.ToList(), writer, codec, options);
                return;
            }

            using var encoder = new Encoder(writerSchema ?? Schema.Create(typeof(T), options), writer, codec, options, metadata);
            encoder.AppendAll(items);
        }

        /// <summary>
        /// Writes <paramref name="items"/> to <paramref name="output"/> as an Avro object container file, one entry per item.
        /// Each data block is written with <see cref="Stream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/> as soon as
        /// it is full. The stream is left open.
        /// </summary>
        /// <remarks>Schema rules are the same as for <see cref="Serialize{T}(IEnumerable{T}, Stream, AvroConvertOptions)"/>.</remarks>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static Task Serialize<T>(IAsyncEnumerable<T> items, Stream output, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
        {
            return SerializeStreamAsync(items, output, null, options, null, cancellationToken);
        }

        /// <summary>
        /// Writes <paramref name="items"/> to <paramref name="output"/> as an Avro object container file with the given schema,
        /// one entry per item, each data block as soon as it is full. Items are never collected. The stream is left open.
        /// <paramref name="metadata"/> holds additional keys of the header, see <see cref="Serialize(object, AvroSchema, AvroConvertOptions, IReadOnlyDictionary{string, AvroMetadataValue})"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static Task Serialize<T>(IAsyncEnumerable<T> items, Stream output, AvroSchema schema, AvroConvertOptions options = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(schema);
            return SerializeStreamAsync(items, output, schema.TypeSchema, options, metadata, cancellationToken);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static async Task SerializeStreamAsync<T>(IAsyncEnumerable<T> items, Stream output, TypeSchema writerSchema, AvroConvertOptions options, IReadOnlyDictionary<string, AvroMetadataValue> metadata, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(output);
            var codec = options?.Codec ?? CodecType.Null;
            using var buffer = new Writer();

            if (writerSchema == null && !HasStreamableSchema(typeof(T), options))
            {
                var all = new List<T>();
                await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    all.Add(item);
                }

                SerializeCollected(all, buffer, codec, options);
            }
            else
            {
                using var encoder = new Encoder(writerSchema ?? Schema.Create(typeof(T), options), buffer, codec, options, metadata);
                var write = encoder.TypedWriter<T>();
                await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (encoder.Append(write, item))
                    {
                        await DrainAsync(buffer, output, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            await DrainAsync(buffer, output, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads an Avro object container file from <paramref name="input"/> item by item. Data blocks are read
        /// asynchronously as the sequence is enumerated; only the current block is held in memory.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="T"/> is the type of one entry. When the file holds whole arrays (written with
        /// <see cref="AvroCollectionMode.SingleArray"/>) and <typeparamref name="T"/> is not a collection, their items are returned.
        /// </remarks>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static IAsyncEnumerable<T> Deserialize<T>(Stream input, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(input);
            return AsyncContainerReader.Read<T>(input, options, cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="obj"/> into Avro format (including header with metadata) directly into <paramref name="output"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static void Serialize(object obj, IBufferWriter<byte> output, AvroConvertOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(output);
            using var writer = new Writer(output);
            SerializeTo(obj, writer, options?.Codec ?? CodecType.Null, options);
        }

        /// <summary>
        /// Serializes <paramref name="obj"/> into Avro format (including header with metadata) with the given schema, directly into <paramref name="output"/>.
        /// <paramref name="metadata"/> holds additional keys of the header, see <see cref="Serialize(object, AvroSchema, AvroConvertOptions, IReadOnlyDictionary{string, AvroMetadataValue})"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static void Serialize(object obj, IBufferWriter<byte> output, AvroSchema schema, AvroConvertOptions options = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            ArgumentNullException.ThrowIfNull(output);
            ArgumentNullException.ThrowIfNull(schema);
            using var writer = new Writer(output);
            SerializeTo(obj, writer, options?.Codec ?? CodecType.Null, options, schema.TypeSchema, metadata);
        }

        /// <summary>
        /// Serializes <paramref name="obj"/> to Avro format - <c>excluding</c> header - directly into <paramref name="output"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static void SerializeHeadless(object obj, IBufferWriter<byte> output, AvroSchema schema, AvroConvertOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(output);
            ArgumentNullException.ThrowIfNull(schema);
            using var writer = new Writer(output);
            ResolveWriter(schema.TypeSchema, obj, options)(obj, writer);
            writer.Flush();
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static bool HasStreamableSchema(Type type, AvroConvertOptions options) =>
            type != typeof(object) && type != typeof(ExpandoObject) && !type.IsInterface && !type.IsAbstract && !RuntimeSchema.IsOpen(Schema.Create(type, options));

        private static bool IsSingleValue(object items) => items is string or byte[] or IDictionary;

        /// <summary>
        /// Items whose schema depends on them. A type with a schema of its own is completed from the items; <c>object</c> takes
        /// what they hold; an interface or an abstract class says nothing, so the items must share one runtime type.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void SerializeCollected<T>(List<T> items, Writer output, CodecType codec, AvroConvertOptions options)
        {
            using var encoder = new Encoder(CollectedSchema(items, options), output, codec, options);
            foreach (var item in items)
            {
                encoder.Append(item);
            }
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static TypeSchema CollectedSchema<T>(List<T> items, AvroConvertOptions options)
        {
            var type = typeof(T);
            if (items.Count == 0 && (type == typeof(object) || type == typeof(ExpandoObject) || type.IsInterface || type.IsAbstract))
            {
                return new NullSchema();
            }

            if (type == typeof(ExpandoObject))
            {
                return RuntimeSchema.ResolveExpandos(items.Cast<ExpandoObject>(), options);
            }

            if (type == typeof(object) || !type.IsInterface && !type.IsAbstract)
            {
                return RuntimeSchema.Resolve(Schema.Create(type, options), items, options);
            }

            var types = items.Select(i => i?.GetType()).Distinct().ToList();
            if (types.Count > 1 || types.Contains(null))
            {
                throw new ArgumentException(
                    $"Items of [{type}] must share one runtime type to be written with one schema; found: {string.Join(", ", types.Select(t => t?.ToString() ?? "null"))}.",
                    nameof(items));
            }

            return types[0] == typeof(ExpandoObject)
                ? RuntimeSchema.ResolveExpandos(items.Cast<ExpandoObject>(), options)
                : RuntimeSchema.Resolve(Schema.Create(types[0], options), items, options);
        }

        private static async Task DrainAsync(Writer buffer, Stream output, CancellationToken cancellationToken)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            await output.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
            buffer.Reset();
        }
    }
}
