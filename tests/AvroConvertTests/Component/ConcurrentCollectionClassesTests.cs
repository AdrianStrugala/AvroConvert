namespace AvroConvertTests.Component
{
    using AutoFixture;
    using AvroConvert;
    using Xunit;

    public class ConcurrentCollectionClassesTests
    {
        private readonly Fixture _fixture;

        public ConcurrentCollectionClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact (Skip = "ConcurrentBagClass is not supported yet")]
        public void Component_SerializeConcurrentBagClass_ResultIsTheSameAsInput()
        {
            //Arrange
            ConcurrentBagClass toSerialize = _fixture.Create<ConcurrentBagClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<ConcurrentBagClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }
    }
}
