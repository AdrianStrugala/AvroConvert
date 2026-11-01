#region license
/**Copyright (c) 2020 Adrian Strugala
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

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System;
using System.Buffers;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type, with the schema of <typeparamref name="T"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(byte[] avroBytes)
        {
            var schema = BuildSchema(typeof(T));
            return RuntimeSchema.IsOpen(schema) ? DeserializeWithoutObjectValues<T>(avroBytes, schema) : DeserializeHeadless<T>(avroBytes, schema, schema, null);
        }

        /// <summary>
        /// The schema of <typeparamref name="T"/> says nothing about its values of type <c>object</c>, so only data without such
        /// values can be read (e.g. an empty dictionary). Anything else fails to read, or leaves bytes behind.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static T DeserializeWithoutObjectValues<T>(byte[] avroBytes, TypeSchema schema)
        {
            static string Message() =>
                $"[{typeof(T)}] holds values of type object, whose schema is not known from the type, and headless data does not carry it. " +
                "Give the schema the data was written with: DeserializeHeadless<T>(bytes, schema).";

            var reader = new Reader(avroBytes);
            T result;
            try
            {
                result = new Resolver(schema, schema).ResolveToEnd<T>(reader);
            }
            catch (Exception e)
            {
                throw new MissingSchemaException(Message(), e);
            }

            return reader.IsReadToEnd() ? result : throw new MissingSchemaException(Message());
        }

        /// <summary>
        /// Deserializes Avro object, which does not contain header, written with <paramref name="writerSchema"/> to .NET type.
        /// When <typeparamref name="T"/> is a collection and <paramref name="writerSchema"/> describes one item, the items written
        /// one after another (e.g. rows from the BigQuery Storage API) are read until the end of the data.
        /// </summary>
        /// <param name="avroBytes">The data.</param>
        /// <param name="writerSchema">The schema the data was written with.</param>
        /// <param name="readerSchema">The schema to resolve against (schema evolution); null uses the schema of <typeparamref name="T"/>.</param>
        /// <param name="options">The Avro conversion options; null uses the defaults.</param>
        /// <param name="numberOfRows">Rows to read into a collection <typeparamref name="T"/>; null reads until the end of the data.</param>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(byte[] avroBytes, AvroSchema writerSchema, AvroSchema readerSchema = null, AvroConvertOptions options = null, int? numberOfRows = null)
        {
            ArgumentNullException.ThrowIfNull(writerSchema);
            return DeserializeHeadless<T>(avroBytes, writerSchema.TypeSchema, readerSchema?.TypeSchema ?? Schema.Create(typeof(T), options), numberOfRows, options);
        }

        /// <summary>
        /// Deserializes Avro data, which does not contain header, written with <paramref name="writerSchema"/> to .NET type. Up to
        /// 1 MB of <paramref name="avroBytes"/> is copied to a pooled buffer for the read; larger input is read in place.
        /// </summary>
        /// <param name="avroBytes">The data.</param>
        /// <param name="writerSchema">The schema the data was written with.</param>
        /// <param name="readerSchema">The schema to resolve against (schema evolution); null uses the schema of <typeparamref name="T"/>.</param>
        /// <param name="options">The Avro conversion options; null uses the defaults.</param>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(ReadOnlySpan<byte> avroBytes, AvroSchema writerSchema, AvroSchema readerSchema = null, AvroConvertOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(writerSchema);
            var readSchema = ReadSchemaOrWriter(readerSchema?.TypeSchema ?? Schema.Create(typeof(T), options), writerSchema.TypeSchema);
            return ResolveToEnd<T>(avroBytes, new Resolver(writerSchema.TypeSchema, readSchema, options));
        }

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to a type known only at runtime, with the schema of that type.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static dynamic DeserializeHeadless(byte[] avroBytes, Type targetType) =>
            BoxedHeadlessReader(targetType)(avroBytes, null, null, null, null);

        /// <summary>
        /// Deserializes Avro object, which does not contain header, written with <paramref name="writerSchema"/> to a type known
        /// only at runtime. See <see cref="DeserializeHeadless{T}(byte[], AvroSchema, AvroSchema, AvroConvertOptions, int?)"/>.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static object DeserializeHeadless(byte[] avroBytes, AvroSchema writerSchema, Type targetType, AvroSchema readerSchema = null, AvroConvertOptions options = null, int? numberOfRows = null)
        {
            ArgumentNullException.ThrowIfNull(writerSchema);
            return BoxedHeadlessReader(targetType)(avroBytes, writerSchema, readerSchema, options, numberOfRows);
        }

        private delegate object BoxedHeadlessRead(byte[] avroBytes, AvroSchema writerSchema, AvroSchema readerSchema, AvroConvertOptions options, int? numberOfRows);

        // DeserializeHeadless<T> for a type known only at runtime: the generic method is bound once per type, then called directly.
        private static readonly ConcurrentDictionary<Type, BoxedHeadlessRead> BoxedHeadlessReaders = new();

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static BoxedHeadlessRead BoxedHeadlessReader(Type targetType)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            return BoxedHeadlessReaders.GetOrAdd(targetType, static type =>
                typeof(AvroConvert).GetMethod(nameof(DeserializeHeadlessBoxed), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(type)
                    .CreateDelegate<BoxedHeadlessRead>());
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static object DeserializeHeadlessBoxed<T>(byte[] avroBytes, AvroSchema writerSchema, AvroSchema readerSchema, AvroConvertOptions options, int? numberOfRows) =>
            writerSchema == null ? DeserializeHeadless<T>(avroBytes) : DeserializeHeadless<T>(avroBytes, writerSchema, readerSchema, options, numberOfRows);

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type.
        /// When <typeparamref name="T"/> is a collection and <paramref name="schema"/> describes one item, the items written one
        /// after another (e.g. rows from the BigQuery Storage API) are read until the end of the data.
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema)
        {
            return DeserializeHeadless<T>(avroBytes, Schema.Parse(schema), BuildSchema(typeof(T)), null);
        }

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema, int numberOfRows)
        {
            return DeserializeHeadless<T>(avroBytes, Schema.Parse(schema), BuildSchema(typeof(T)), numberOfRows);
        }

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static dynamic DeserializeHeadless(byte[] avroBytes, string schema, Type targetType) =>
            BoxedHeadlessReader(targetType)(avroBytes, AvroSchema.Parse(schema), null, null, null);

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(byte[] avroBytes, string writeSchema, string readSchema, int numberOfRows)
        {
            return DeserializeHeadless<T>(avroBytes, Schema.Parse(writeSchema), Schema.Parse(readSchema), numberOfRows);
        }

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static dynamic DeserializeHeadless(byte[] avroBytes, string writeSchema, string readSchema, Type targetType) =>
            BoxedHeadlessReader(targetType)(avroBytes, AvroSchema.Parse(writeSchema), AvroSchema.Parse(readSchema), null, null);

        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static T DeserializeHeadless<T>(ReadOnlySpan<byte> avroBytes, string schema)
        {
            var writeSchema = Schema.Parse(schema);
            var readSchema = ReadSchemaOrWriter(BuildSchema(typeof(T)), writeSchema);
            return ResolveToEnd<T>(avroBytes, new Resolver(writeSchema, readSchema));
        }

        private const int PooledCopyLimit = 1024 * 1024;

        // The array reader is much faster than reading through a stream, and Reader copies every value out of the buffer,
        // so a pooled copy is safe to return. Above the limit the data is read in place instead of copied.
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static unsafe T ResolveToEnd<T>(ReadOnlySpan<byte> avroBytes, Resolver resolver)
        {
            if (avroBytes.Length <= PooledCopyLimit)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(avroBytes.Length);
                try
                {
                    avroBytes.CopyTo(buffer);
                    return resolver.ResolveToEnd<T>(new Reader(buffer, 0, avroBytes.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            fixed (byte* ptr = avroBytes)
            {
                using UnmanagedMemoryStream stream = new(ptr, avroBytes.Length);
                return resolver.ResolveToEnd<T>(new Reader(stream));
            }
        }

        /// <param name="numberOfRows">Rows to read into a collection <typeparamref name="T"/>; null reads until the end of the data.</param>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static T DeserializeHeadless<T>(byte[] avroBytes, TypeSchema writeSchema, TypeSchema readSchema, int? numberOfRows, AvroConvertOptions options = null)
        {
            var reader = new Reader(avroBytes);
            var resolver = new Resolver(writeSchema, ReadSchemaOrWriter(readSchema, writeSchema), options);
            return numberOfRows is { } rows ? resolver.Resolve<T>(reader, rows) : resolver.ResolveToEnd<T>(reader);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static TypeSchema BuildSchema(Type type) => Schema.Create(type);

        /// <summary>dynamic / ExpandoObject targets carry no shape of their own; the writer schema is read as-is.</summary>
        private static TypeSchema ReadSchemaOrWriter(TypeSchema readSchema, TypeSchema writeSchema) =>
            readSchema == null || readSchema.IsEmpty() ? writeSchema : readSchema;
    }
}
