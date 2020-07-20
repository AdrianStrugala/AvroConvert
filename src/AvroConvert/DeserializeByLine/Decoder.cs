using System.IO;
using System.Linq;
using SolTechnology.Avro.Codec;
using SolTechnology.Avro.Constants;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Helpers;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.DeserializeByLine
{
    internal class Decoder
    {
        internal static BlockLineReader<T> OpenReader<T>(Stream stream, Schema.Schema readSchema)
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
                if (readSchema == null)
                {
                    throw new MissingSchemaException("Provide valid schema for the Avro data");
                }
                var resolver = new Resolver(readSchema, readSchema);
                stream.Seek(0, SeekOrigin.Begin);
                return new BlockLineReader<T>(reader, resolver, 0);
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
                            header.MetaData.Add(key, val);
                        }
                    } while ((len = reader.ReadMapNext()) != 0);
                }

                readSchema = readSchema ?? Schema.Schema.Parse(GetMetaString(header.MetaData[DataFileConstants.SchemaMetadataKey]));
                Schema.Schema writeSchema = Schema.Schema.Parse(GetMetaString(header.MetaData[DataFileConstants.SchemaMetadataKey]));

                if (writeSchema.Tag == Schema.Schema.Type.Array)
                {
                    writeSchema = ((ArraySchema)writeSchema).ItemSchema;
                }

                var resolver = new Resolver(writeSchema, readSchema);

                // read in sync data 
                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(GetMetaString(header.MetaData[DataFileConstants.CodecMetadataKey]));


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

                return new BlockLineReader<T>(reader, resolver, remainingBlocks);
            }
        }


        internal static string GetMetaString(byte[] value)
        {
            if (value == null)
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(value);
        }
    }
}