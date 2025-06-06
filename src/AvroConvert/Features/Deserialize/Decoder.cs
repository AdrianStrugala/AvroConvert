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
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Deserialize
{
    internal class Decoder
    {
        private readonly AvroConvertOptions _options;

        public Decoder(AvroConvertOptions options = null)
        {
            _options = options;
        }

        internal T Decode<T>(Stream stream, TypeSchema readSchema)
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
                var header = reader.ReadHeader();

                TypeSchema writeSchema = Schema.Parse(header.GetMetadata(DataFileConstants.SchemaMetadataKey));

                if (readSchema == null || readSchema.IsEmpty())
                {
                    readSchema = writeSchema;
                }

                var resolver = new Resolver(writeSchema, readSchema, _options);

                // read in sync data 
                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));


                return Read<T>(reader, header, codec, resolver);
            }
        }


        internal T Read<T>(Reader reader, Header header, AbstractCodec codec, Resolver resolver)
        {
            if (reader.IsReadToEnd())
            {
                return default;
            }

            long itemsCount = 0;
            byte[] data = Array.Empty<byte>();

            do
            {
                itemsCount += reader.ReadLong();
                var dataBlock = reader.ReadDataBlock(header.SyncData, codec);

                int dataBlockSize = data.Length;
                Array.Resize(ref data, dataBlockSize + dataBlock.Length);
                Array.Copy(dataBlock, 0, data, dataBlockSize, dataBlock.Length);

            } while (!reader.IsReadToEnd());


            reader = new Reader(new MemoryStream(data));

            return resolver.Resolve<T>(reader, itemsCount);
        }
    }
}