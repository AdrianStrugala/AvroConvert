using System.IO;
using System.Linq;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.FileHeader;
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro.Merge
{
    internal class MergeDecoder
    {
        internal byte[] ExtractAvroData(byte[] avroObject, string schema)
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
                    var header = reader.ReadHeader();

                    reader.ReadFixed(header.SyncData);

                    var remainingBlocks = reader.ReadLong();
                    //TODO: take into consifer number of remaining blocks?
                    var dataBlock = reader.ReadDataBlock();

                    return dataBlock;
                }
            }
        }
    }
}
