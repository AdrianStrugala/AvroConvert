﻿#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System;
using System.IO;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.Deserialize;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Deserializes Avro data from a byte array into a specified .NET type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize into.</typeparam>
        /// <param name="avroBytes">The byte array containing the Avro data to be deserialized.</param>
        /// <returns>The deserialized object of the specified type.</returns>
        /// <remarks>
        /// This method takes a byte array containing Avro data and deserializes it into an object of the specified type (generic parameter T).
        /// </remarks>
        public static T Deserialize<T>(byte[] avroBytes)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode<T>(
                    stream,
                    Schema.Create(typeof(T))
                );
                return deserialized;
            }
        }
        

        /// <summary>
        /// Deserializes Avro data from a byte array into a specified .NET type using the provided Avro conversion options.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize into.</typeparam>
        /// <param name="avroBytes">The byte array containing the Avro data to be deserialized.</param>
        /// <param name="options">The Avro conversion options that control the deserialization process.</param>
        /// <returns>The deserialized object of the specified type.</returns>
        /// <remarks>
        /// This method takes a byte array containing Avro data and deserializes it into an object of the specified type (generic parameter T).
        /// The deserialization process is controlled by the provided Avro conversion options.
        /// </remarks>
        public static T Deserialize<T>(byte[] avroBytes, AvroConvertOptions options)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                var decoder = new Decoder(options);
                var deserialized = decoder.Decode<T>(
                    stream,
                    Schema.Create(typeof(T))
                );
                return deserialized;
            }
        }

        /// <summary>
        /// Deserializes Avro data from a byte array into an object of the specified target type using reflection.
        /// </summary>
        /// <param name="avroBytes">The byte array containing the Avro data to be deserialized.</param>
        /// <param name="targetType">The target type into which the Avro data should be deserialized.</param>
        /// <returns>The deserialized object of the specified target type.</returns>
        /// <remarks>
        /// This method uses reflection to dynamically invoke the generic <see cref="Deserialize{T}"/> method
        /// to deserialize Avro data from a byte array into an object of the specified target type.
        /// </remarks>
        public static dynamic Deserialize(byte[] avroBytes, Type targetType)
        {
            object result;
            try
            {
                result = typeof(AvroConvert)
                    .GetMethod(nameof(Deserialize), new[] { typeof(byte[]) })
                    ?.MakeGenericMethod(targetType)
                    .Invoke(null, new object[] { avroBytes });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }

            return result;
        }
        

        /// <summary>
        /// Deserializes Avro data from a <see cref="ReadOnlySpan{T}"/> of bytes into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize into.</typeparam>
        /// <param name="avroBytes">The <see cref="ReadOnlySpan{T}"/> of bytes containing the Avro data to be deserialized.</param>
        /// <returns>The deserialized object of the specified type.</returns>
        /// <remarks>
        /// This method performs Avro data deserialization from a <see cref="ReadOnlySpan{T}"/> of bytes into an object of the specified type.
        /// It is suitable for scenarios where minimizing memory allocation is crucial.
        /// </remarks>
        public static unsafe T Deserialize<T>(ReadOnlySpan<byte> avroBytes)
        {
            fixed (byte* ptr = avroBytes)
            {
                using UnmanagedMemoryStream stream = new(ptr, avroBytes.Length);
                var decoder = new Decoder();
                var obj = decoder.Decode<T>(
                    stream,
                    Schema.Create(typeof(T))
                );

                return obj;
            }
        }
    }
}
