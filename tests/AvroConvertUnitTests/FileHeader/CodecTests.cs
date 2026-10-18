using AwesomeAssertions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using NUnit.Framework;

namespace AvroConvertUnitTests.FileHeader
{
    public class CodecTests
    {
        [Test]
        public void CreateCodecFromString_NonExistingString_DefaultCodecIsReturned()
        {
            //Arrange


            //Act
            var result = AbstractCodec.CreateCodecFromString("NonExistingCodec");


            //Assert
            result.Should().BeOfType<NullCodec>();
        }

        [Test]
        public void Zstandard_RoundTrip_UsesSpecName()
        {
            //Arrange
            var payload = Encoding.UTF8.GetBytes(string.Concat(System.Linq.Enumerable.Repeat("zstandard avro block ", 200)));


            //Act
            var codec = AbstractCodec.CreateCodecFromString("zstandard");
            using var output = new MemoryStream();
            codec.Compress(payload, output);


            //Assert
            codec.Should().BeOfType<ZstandardCodec>();
            codec.Name.Should().Be("zstandard");
            (output.Length < payload.Length).Should().BeTrue();
            output.ToArray()[..4].Should().Equal(new byte[] { 0x28, 0xB5, 0x2F, 0xFD }); // zstd frame magic
            codec.Decompress(output.ToArray()).Should().Equal(payload);
        }

        [Test]
        public void Crc32_KnownVector_MatchesIeee()
        {
            // Standard CRC-32 check value for "123456789"
            Crc32.Get(Encoding.ASCII.GetBytes("123456789")).Should().Be(0xCBF43926u);
        }

        [Test]
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
            BinaryPrimitives.ReadUInt32BigEndian(trailer).Should().Be(Crc32.Get(payload));
            codec.Decompress(compressed).Should().Equal(payload);
        }
    }
}
