﻿#region license
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

using System.IO;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Merge
{
    internal class MergeDecoder
    {
        internal AvroObjectContent ExtractAvroObjectContent(byte[] avroObject)
        {
            using (var stream = new MemoryStream(avroObject))
            {
                var reader = new Reader(stream);

                // validate header 
                byte[] firstBytes = new byte[DataFileConstants.AvroHeader.Length];

                try
                {
                    reader.ReadFixed(firstBytes);
                }
                catch (EndOfStreamException)
                {
                    //stream shorter than AvroHeader
                }

                //does not contain header
                if (!firstBytes.SequenceEqual(DataFileConstants.AvroHeader))
                {
                    throw new InvalidAvroObjectException("Object does not contain Avro Header");
                }
                else
                {
                    AvroObjectContent result = new AvroObjectContent();
                    var header = reader.ReadHeader();
                    result.Codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

                    reader.ReadFixed(header.SyncData);

                    result.Header = header;
                    result.Header.Schema = Schema.Parse(result.Header.GetMetadata(DataFileConstants.SchemaMetadataKey));

                    if (reader.IsReadToEnd())
                    {
                        return result;
                    }

                    do
                    {
                        var blockContent = new DataBlock
                        {
                            ItemsCount = reader.ReadLong(),
                            Data = reader.ReadDataBlock(header.SyncData, result.Codec)
                        };

                        result.DataBlocks.Add(blockContent);

                    } while (!reader.IsReadToEnd());

                    return result;
                }
            }
        }
    }
}
