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

using System.IO;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.DeserializeByLine
{
    internal class Decoder
    {
        internal static ILineReader<T> OpenReader<T>(Stream stream, TypeSchema readSchema)
        {
            var reader = new Reader(stream);
            Reader blockReader = null;

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
                if (readSchema == null)
                {
                    throw new MissingSchemaException("Provide valid schema for the Avro data");
                }
                var resolver = new Resolver(readSchema, readSchema);
                stream.Seek(0, SeekOrigin.Begin);
                return new ListLineReader<T>(reader, resolver);
            }
            else
            {
                var header = reader.ReadHeader();

                readSchema = readSchema ?? Schema.Create(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
                TypeSchema writeSchema = Schema.Create(header.GetMetadata(DataFileConstants.SchemaMetadataKey));

                var resolver = new Resolver(writeSchema, readSchema);

                // read in sync data 
                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

                var remainingBlocks = reader.ReadLong();

                var dataBlock = reader.ReadDataBlock();

                reader.ReadAndValidateSync(header.SyncData);

                dataBlock = codec.Decompress(dataBlock);
                blockReader = new Reader(new MemoryStream(dataBlock));



                // // stream.Position == stream.Length
                //
                //     //@ND
                // remainingBlocks = reader.ReadLong();
                // dataBlock = reader.ReadDataBlock();
                // reader.ReadAndValidateSync(header.SyncData);
                // dataBlock = codec.Decompress(dataBlock);
                // blockReader = new Reader(new MemoryStream(dataBlock));






                if (remainingBlocks > 1)
                {
                    return new BlockLineReader<T>(blockReader, resolver, remainingBlocks);
                }

                if (writeSchema.Type == AvroType.Array)
                {
                    return new ListLineReader<T>(reader, new Resolver(((ArraySchema)writeSchema).ItemSchema, readSchema));
                }

                return new ListLineReader<T>(reader, new Resolver(writeSchema, readSchema));
            }
        }
    }
}