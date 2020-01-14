using System;
using System.Linq;

namespace SolTechnology.Avro.Codec
{
    public class SnappyCodec : AbstractCodec
    {
        public override string Name { get; } = Type.Snappy.ToString().ToLower();

        public override byte[] Compress(byte[] uncompressedData)
        {
            var compressedData = Snappy.SnappyCodec.Compress(uncompressedData);
            uint checksumUint = Crc32.Get(compressedData);
            byte[] checksumBytes = BitConverter.GetBytes(checksumUint);

            byte[] result = compressedData.Concat(checksumBytes).ToArray();
            return result;
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
