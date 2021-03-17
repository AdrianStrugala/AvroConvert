using System.IO;
using System.Linq;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.FileHeader;
using SolTechnology.Avro.FileHeader.Codec;
using SolTechnology.Avro.Schema.Abstract;

namespace SolTechnology.Avro.Read
{
    internal class Decoder
    {
        internal T Decode<T>(Stream stream, TypeSchema readSchema)
        {
            var reader = new Reader(stream);
            var header = new Header();

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
                // read meta data 
                long len = reader.ReadMapStart();
                if (len > 0)
                {
                    do
                    {
                        for (long i = 0; i < len; i++)
                        {
                            string key = reader.ReadString();
                            byte[] val = reader.ReadBytes();
                            header.AddMetadata(key, val);
                        }
                    } while ((len = reader.ReadMapNext()) != 0);
                }

                TypeSchema writeSchema = BuildSchema.Schema.Create(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
                readSchema = readSchema ?? writeSchema;
                var resolver = new Resolver(writeSchema, readSchema);

                // read in sync data 
                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));


                return Read<T>(reader, header, codec, resolver);
            }
        }


        internal T Read<T>(IReader reader, Header header, AbstractCodec codec, Resolver resolver)
        {
            var remainingBlocks = reader.ReadLong();
            var blockSize = reader.ReadLong();
            var syncBuffer = new byte[DataFileConstants.SyncSize];

            var dataBlock = new byte[blockSize];

            reader.ReadFixed(dataBlock, 0, (int)blockSize);
            reader.ReadFixed(syncBuffer);

            if (!syncBuffer.SequenceEqual(header.SyncData))
                throw new AvroRuntimeException("Invalid sync!");

            dataBlock = codec.Decompress(dataBlock);
            reader = new Reader(new MemoryStream(dataBlock));

            return resolver.Resolve<T>(reader, remainingBlocks);

        }
    }
}