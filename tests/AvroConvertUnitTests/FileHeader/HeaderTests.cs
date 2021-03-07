using SolTechnology.Avro.FileHeader;
using Xunit;

namespace AvroConvertUnitTests.FileHeader
{
    public class HeaderTests
    {
        [Fact]
        public void GetMetadata_ItExists_ValueIsReturned()
        {
            //Arrange
            var sut = new Header();
            var expectedValue = "xdddd";
            var key = "yo";

            sut.AddMetadata(key, System.Text.Encoding.UTF8.GetBytes(expectedValue));


            //Act
            var result = sut.GetMetadata(key);


            //Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void GetMetadata_ItDoesNotExist_NullIsReturned()
        {
            //Arrange
            var sut = new Header();


            //Act
            var result = sut.GetMetadata("yyyyy");


            //Assert
            Assert.Null(result);
        }
    }
}
