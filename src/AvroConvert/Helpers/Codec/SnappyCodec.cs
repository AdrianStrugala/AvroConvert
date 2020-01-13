using System;

namespace SolTechnology.Avro.Helpers.Codec
{
    public class SnappyCodec : Codec
    {
        public override byte[] Compress(byte[] uncompressedData)
        {

            return Snappy.SnappyCodec.Compress(uncompressedData);
        }

        public override DataBlock Read(long blockRemaining, long blockSize)
        {
            blockSize = blockSize - 4; //CRC

            var dataBlock = new DataBlock(blockRemaining, blockSize);

            _reader.ReadFixed(dataBlock.Data, 0, (int)blockSize);
            dataBlock.Data = Decompress(dataBlock.Data);

            _reader.ReadFixed(new byte[4]);

            return dataBlock;
        }

        public byte[] Decompress(byte[] compressedData)
        {
            return Snappy.SnappyCodec.Uncompress(compressedData);
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
