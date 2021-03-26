using System.Collections.Generic;
using SolTechnology.Avro;
using SolTechnology.Avro.Exceptions;
using Xunit;

namespace AvroConvertUnitTests
{
    public class Exceptions
    {
        [Fact]
        public void Deserialize_RecordInsteadOfArray_MeaningfulExceptionIsThrown()
        {
            //Arrange
            List<int> testObject = new List<int> { 3, 6, 8 };

            //Act
            var result = AvroConvert.Serialize(testObject);

            var exception = Record.Exception(() => AvroConvert.Deserialize<int>(result));

            //Assert
            Assert.IsType<AvroTypeMismatchException>(exception);
        }
    }
}
