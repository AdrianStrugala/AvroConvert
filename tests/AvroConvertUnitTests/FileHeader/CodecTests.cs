using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using Xunit;

namespace AvroConvertUnitTests.FileHeader
{
    public class CodecTests
    {
        [Fact]
        public void CreateCodecFromString_NonExistingString_DefaultCodecIsReturned()
        {
            //Arrange


            //Act
            var result = AbstractCodec.CreateCodecFromString("NonExistingCodec");


            //Assert
            Assert.IsType<NullCodec>(result);
        }

        [Fact]
        public void Crc32_KnownVector_MatchesIeee()
        {
            // Standard CRC-32 check value for "123456789"
            Assert.Equal(0xCBF43926u, Crc32.Get(Encoding.ASCII.GetBytes("123456789")));
        }

        [Fact]
        public void Snappy_Compress_AppendsBigEndianCrcOfUncompressedData()
        {
            //Arrange
            var codec = new SnappyCodec();
            var payload = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog.");


            //Act
            using var output = new MemoryStream();
            codec.Compress(payload, output);
            var compressed = output.ToArray();


            //Assert
            var trailer = compressed.AsSpan(compressed.Length - 4);
            Assert.Equal(Crc32.Get(payload), BinaryPrimitives.ReadUInt32BigEndian(trailer));
            Assert.Equal(payload, codec.Decompress(compressed));
        }
    }
}
