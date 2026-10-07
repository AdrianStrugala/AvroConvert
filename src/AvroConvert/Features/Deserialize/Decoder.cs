#region license
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
using System.Collections.Generic;
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
                // Header-only file: an empty collection for collection targets, default otherwise.
                var emptyPlan = resolver.GetPlan<T>().Blocks;
                return emptyPlan != null ? emptyPlan.Finish(emptyPlan.NewAccumulator()) : default;
            }

            long firstCount = reader.ReadLong();
            byte[] firstBlock = reader.ReadDataBlock(header.SyncData, codec);

            // Single block: decode straight from the decompressed buffer.
            if (reader.IsReadToEnd())
            {
                return resolver.Resolve<T>(new Reader(firstBlock), firstCount);
            }

            var plan = resolver.GetPlan<T>();
            if (plan.Blocks != null)
            {
                // Multi-block file: append each block's entries to the result collection, no buffer concatenation.
                try
                {
                    var accumulator = plan.Blocks.NewAccumulator();
                    plan.Blocks.Append(new Reader(firstBlock), firstCount, accumulator);
                    while (!reader.IsReadToEnd())
                    {
                        long count = reader.ReadLong();
                        var block = reader.ReadDataBlock(header.SyncData, codec);
                        plan.Blocks.Append(new Reader(block), count, accumulator);
                    }

                    return plan.Blocks.Finish(accumulator);
                }
                catch (Exception e)
                {
                    throw resolver.WrapFailure<T>(e);
                }
            }

            // T is not a collection: classic behaviour over the concatenated blocks.
            long itemsCount = firstCount;
            var blocks = new List<byte[]> { firstBlock };
            long totalLength = firstBlock.Length;
            while (!reader.IsReadToEnd())
            {
                itemsCount += reader.ReadLong();
                var block = reader.ReadDataBlock(header.SyncData, codec);
                blocks.Add(block);
                totalLength += block.Length;
            }

            var data = new byte[totalLength];
            int offset = 0;
            foreach (var block in blocks)
            {
                block.CopyTo(data, offset);
                offset += block.Length;
            }

            return resolver.Resolve<T>(new Reader(data), itemsCount);
        }
    }
}