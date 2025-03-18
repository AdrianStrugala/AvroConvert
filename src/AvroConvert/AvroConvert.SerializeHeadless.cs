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

using System;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Serializes given object to Avro format - <c>excluding</c> header
        /// </summary>
        public static byte[] SerializeHeadless(object obj, string schema)
        {
            MemoryStream resultStream = new MemoryStream();
            var encoder = new Writer(resultStream);
            var schemaObject = Schema.Parse(schema);
            var resolver = new WriteResolver();
            var writer = resolver.ResolveWriter(schemaObject);

            writer(obj, encoder);

            var result = resultStream.ToArray();
            return result;
        }

        /// <summary>
        /// Serializes given object to Avro format - <c>excluding</c> header
        /// </summary>
        public static byte[] SerializeHeadless(object obj, Type objectType)
        {
            MemoryStream resultStream = new MemoryStream();
            var encoder = new Writer(resultStream);
            var schemaObject = BuildSchema(objectType);
            var resolver = new WriteResolver();
            var writer = resolver.ResolveWriter(schemaObject);

            writer(obj, encoder);

            var result = resultStream.ToArray();
            return result;
        }
    }
}
