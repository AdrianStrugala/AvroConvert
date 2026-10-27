using System.IO;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace AvroConvertUnitTests
{
    public class VarintTests
    {
        [TestCase(0L, new byte[] { 0x00 })]
        [TestCase(-1L, new byte[] { 0x01 })]
        [TestCase(1L, new byte[] { 0x02 })]
        [TestCase(63L, new byte[] { 0x7e })]
        [TestCase(-64L, new byte[] { 0x7f })]
        [TestCase(64L, new byte[] { 0x80, 0x01 })]
        [TestCase(-65L, new byte[] { 0x81, 0x01 })]
        [TestCase(8191L, new byte[] { 0xfe, 0x7f })]
        [TestCase(8192L, new byte[] { 0x80, 0x80, 0x01 })]
        [TestCase(long.MaxValue, new byte[] { 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01 })]
        [TestCase(long.MinValue, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01 })]
        public void WriteLong_ZigZagVarint(long value, byte[] expected)
        {
            var stream = new MemoryStream();

            new Writer(stream).WriteLong(value);

            stream.ToArray().Should().Equal(expected);
            new Reader(stream.ToArray()).ReadLong().Should().Be(value);
        }

        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(300)]
        public void WriteInt_RoundTrips(int value)
        {
            var stream = new MemoryStream();

            new Writer(stream).WriteInt(value);

            new Reader(stream.ToArray()).ReadInt().Should().Be(value);
        }
    }
}
