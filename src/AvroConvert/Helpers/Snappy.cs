using System;
using System.Linq;
using System.IO;
using SolTechnology.Avro.Constants;
using SolTechnology.Avro.Helpers.Codec;

namespace Snappy.Sharp
{
    public class Snappy : Codec
    {
        internal const int LITERAL = 0;
        internal const int COPY_1_BYTE_OFFSET = 1; // 3 bit length + 3 bits of offset in opcode
        internal const int COPY_2_BYTE_OFFSET = 2;
        internal const int COPY_4_BYTE_OFFSET = 3;

        public int MaxCompressedLength(int sourceLength)
        {
            var compressor = new SnappyCompressor();
            return compressor.MaxCompressedLength(sourceLength);
        }

        public override byte[] Compress(byte[] uncompressed)
        {
            var target = new SnappyCompressor();
            var result = new byte[target.MaxCompressedLength(uncompressed.Length)];
            var count = target.Compress(uncompressed, 0, uncompressed.Length, result);
            return result.Take(count).ToArray();
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            var target = new SnappyDecompressor();
            return target.Decompress(compressedData, 0, compressedData.Length);
        }

        public override string GetName()
        {
            return DataFileConstants.SnappyCodec;
        }

        public override bool Equals(object other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
