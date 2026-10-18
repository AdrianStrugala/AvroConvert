using AwesomeAssertions;
using System.Collections.Generic;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using NUnit.Framework;

namespace AvroConvertUnitTests
{
    public class Exceptions
    {
        [Test]
        public void Deserialize_RecordInsteadOfArray_MeaningfulExceptionIsThrown()
        {
            //Arrange
            List<int> testObject = new List<int> { 3, 6, 8 };

            //Act
            var result = AvroConvert.Serialize(testObject);

            var act = () => AvroConvert.Deserialize<int>(result);

            //Assert
            act.Should().ThrowExactly<AvroTypeMismatchException>();
        }
    }
}
