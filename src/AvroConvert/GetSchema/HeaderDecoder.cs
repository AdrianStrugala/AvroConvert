using System.IO;
using System.Linq;
using System.Text;
using SolTechnology.Avro.Constants;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Helpers;
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro.GetSchema
{
    internal class HeaderDecoder
    {
        internal string GetSchema(Stream stream)
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

                return header.GetMetadata(DataFileConstants.SchemaMetadataKey);
            }
        }
    }
}
