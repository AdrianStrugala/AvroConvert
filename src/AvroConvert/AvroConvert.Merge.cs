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
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.Features.Merge;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Merge multiple Avro objects of type T into one Avro object of type IEnumerable T
        /// </summary>
        public static byte[] Merge<T>(IEnumerable<byte[]> avroObjects)
        {
            var itemSchema = Schema.Create(typeof(T));
            var targetSchema = Schema.Create(typeof(List<T>));
            var mergeDecoder = new MergeDecoder();

            List<DataBlock> avroDataBlocks = new List<DataBlock>();

            avroObjects = avroObjects.ToList();
            for (int i = 0; i < avroObjects.Count(); i++)
            {
                var avroFileContent = mergeDecoder.ExtractAvroObjectContent(avroObjects.ElementAt(i));
                if (!itemSchema.CanRead(avroFileContent.Header.Schema))
                {
                    throw new InvalidAvroObjectException($"Schema from object of index [{i}] is not compatible with schema of type [{typeof(T)}]");
                }

                avroDataBlocks.AddRange(avroFileContent.DataBlocks);
            }

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (var encoder = new MergeEncoder(resultStream))
                {
                    encoder.WriteHeader(targetSchema.ToString(), CodecType.Null);

                    encoder.WriteData(avroDataBlocks);
                }

                var result = resultStream.ToArray();
                return result;

            }
        }
    }
}
