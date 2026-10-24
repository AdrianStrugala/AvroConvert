using AwesomeAssertions;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using NUnit.Framework;

namespace AvroConvertUnitTests.FileHeader
{
    public class HeaderTests
    {
        [Test]
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
            result.Should().Be(expectedValue);
        }

        [Test]
        public void GetMetadata_ItDoesNotExist_NullIsReturned()
        {
            //Arrange
            var sut = new Header();


            //Act
            var result = sut.GetMetadata("yyyyy");


            //Assert
            result.Should().BeNull();
        }
    }
}
