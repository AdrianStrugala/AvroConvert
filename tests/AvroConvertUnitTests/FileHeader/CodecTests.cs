using SolTechnology.Avro.FileHeader.Codec;
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
    }
}
