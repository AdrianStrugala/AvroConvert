using System;

namespace SolTechnology.Avro.Codec
{
    public class SnappyCodec : AbstractCodec
    {
        public override string Name { get; } = Type.Snappy.ToString().ToLower();

        public override byte[] Compress(byte[] uncompressedData)
        {
            return Snappy.SnappyCodec.Compress(uncompressedData);

            //TODO add CRC calculation at the end and add it to dataBlock
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            byte[] dataToDecompress = new byte[compressedData.Length - 4]; // last 4 bytes are CRC
            Array.Copy(compressedData, dataToDecompress, dataToDecompress.Length);

            return Snappy.SnappyCodec.Uncompress(dataToDecompress);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
