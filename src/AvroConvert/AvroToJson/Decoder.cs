using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.FileHeader;
using SolTechnology.Avro.FileHeader.Codec;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema.Abstract;

namespace SolTechnology.Avro.AvroToJson
{
    internal class Decoder
    {
        internal object Decode(Stream stream, TypeSchema schema)
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
                if (schema == null)
                {
                    throw new MissingSchemaException("Provide valid schema for the Avro data");
                }
                var resolver = new Resolver(schema);
                stream.Seek(0, SeekOrigin.Begin);
                return resolver.Resolve(reader);
            }
            else
            {
                var header = reader.ReadHeader();

                schema = schema ?? BuildSchema.Schema.Create(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
                var resolver = new Resolver(schema);

                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

                return Read(reader, header, codec, resolver);
            }
        }


        internal object Read(Reader reader, Header header, AbstractCodec codec, Resolver resolver)
        {
            var remainingBlocks = reader.ReadLong();

            var dataBlock = reader.ReadDataBlock();

            reader.ReadAndValidateSync(header.SyncData);

            dataBlock = codec.Decompress(dataBlock);
            reader = new Reader(new MemoryStream(dataBlock));

            if (remainingBlocks > 1)
            {
                var result = new List<object>();

                for (int i = 0; i < remainingBlocks; i++)
                {
                    result.Add(resolver.Resolve(reader));
                }

                return result;
            }
            else
            {
                return resolver.Resolve(reader);
            }
        }

    }
}