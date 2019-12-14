using System;

namespace SolTechnology.Avro.Helpers.Codec
{
    public class SnappyCodec : Codec
    {
        public override byte[] Compress(byte[] uncompressedData)
        {
            throw new NotSupportedException("Snappy encoding is not supported");
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            throw new NotSupportedException("Snappy encoding is not supported");
        }

        public override string GetName()
        {
            throw new NotSupportedException("Snappy encoding is not supported");
        }

        public override bool Equals(object other)
        {
            throw new NotSupportedException("Snappy encoding is not supported");
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException("Snappy encoding is not supported");
        }
    }
}
