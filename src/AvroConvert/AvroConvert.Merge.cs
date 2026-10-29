#region license
/**Copyright (c) 2021 Adrian Strugala
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Merge;
using SolTechnology.Avro.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Merge multiple Avro objects of type T into one Avro object of type IEnumerable T
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Merge<T>(IEnumerable<byte[]> avroObjects)
        {
            var itemSchema = Schema.Create(typeof(T));
            var mergeDecoder = new MergeDecoder();

            List<DataBlock> avroDataBlocks = new List<DataBlock>();
            TypeSchema writerSchema = null;
            string writerSchemaJson = null;

            avroObjects = avroObjects.ToList();
            for (int i = 0; i < avroObjects.Count(); i++)
            {
                var avroFileContent = mergeDecoder.ExtractAvroObjectContent(avroObjects.ElementAt(i));
                if (!itemSchema.CanRead(avroFileContent.Header.Schema))
                {
                    throw new InvalidAvroObjectException($"Schema from object of index [{i}] is not compatible with schema of type [{typeof(T)}]");
                }

                // Data blocks are copied without re-encoding, so all objects have to be written with the same schema,
                // logical types, precision and scale included (the parsing canonical form leaves them out).
                var schemaJson = AvroSchema.Parse(avroFileContent.Header.Schema.ToString()).Json;
                if (writerSchema == null)
                {
                    writerSchema = avroFileContent.Header.Schema;
                    writerSchemaJson = schemaJson;
                }
                else if (schemaJson != writerSchemaJson)
                {
                    throw new InvalidAvroObjectException($"Object of index [{i}] is written with another schema than the first one. Merge copies the data without re-encoding it, so all objects need the same writer schema");
                }

                avroDataBlocks.AddRange(avroFileContent.DataBlocks);
            }

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (var encoder = new MergeEncoder(resultStream))
                {
                    // The result is one array of the copied items: its items get the schema they were written with.
                    // Objects holding a whole array per entry keep the schema of List<T>.
                    var resultSchema = writerSchema != null && writerSchema.Type != AvroType.Array
                        ? "{\"type\":\"array\",\"items\":" + writerSchema + "}"
                        : Schema.Create(typeof(List<T>)).ToString();
                    encoder.WriteHeader(resultSchema, CodecType.Null);

                    encoder.WriteData(avroDataBlocks);
                }

                var result = resultStream.ToArray();
                return result;

            }
        }
    }
}
