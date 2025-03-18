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
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.DeserializeByLine;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Opens an Avro object deserializer which allows reading a large collection of Avro objects of type <typeparamref name="T"/> one by one.
        /// </summary>
        /// <typeparam name="T">The type of objects to be deserialized.</typeparam>
        /// <param name="stream">The stream containing the Avro data to be deserialized.</param>
        /// <returns>An <see cref="ILineReader{T}"/> instance for sequentially reading the deserialized Avro objects.</returns>
        public static ILineReader<T> OpenDeserializer<T>(Stream stream)
        {
            var reader = Decoder.OpenReader<T>(stream, Schema.Create(typeof(T)));
            return reader;
        }

        /// <summary>
        /// Opens an Avro object deserializer which allows reading a large collection of Avro objects of type <typeparamref name="T"/> one by one using the provided schema.
        /// </summary>
        /// <typeparam name="T">The type of objects to be deserialized.</typeparam>
        /// <param name="stream">The memory stream containing the Avro data to be deserialized.</param>
        /// <param name="schema">A string representation of the Avro schema to be used for deserialization.</param>
        /// <returns>An <see cref="ILineReader{T}"/> instance for sequentially reading the deserialized Avro objects.</returns>
        public static ILineReader<T> OpenDeserializer<T>(MemoryStream stream, string schema)
        {
            var reader = Decoder.OpenReader<T>(stream, Schema.Parse(schema));
            return reader;
        }

    }
}
