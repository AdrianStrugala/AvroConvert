using System;
using System.IO;
using System.IO.Compression;
using Snappy;
using SolTechnology.Avro.Helpers.Codec;

namespace xd.Sharp
{
    public class xd : Codec
    {
        public override byte[] Compress(byte[] uncompressedData)
        {

            return SnappyCodec.Compress(uncompressedData);
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            return SnappyCodec.Uncompress(compressedData);
        }

        public override string GetName()
        {
            throw new NotImplementedException();
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
