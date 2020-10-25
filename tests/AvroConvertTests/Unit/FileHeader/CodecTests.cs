using SolTechnology.Avro.FileHeader.Codec;
using Xunit;

namespace AvroConvertTests.Unit.FileHeader
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
    }
}
