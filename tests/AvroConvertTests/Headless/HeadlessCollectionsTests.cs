using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Headless
{
    // from issue: https://github.com/AdrianStrugala/AvroConvert/issues/92
    public class HeadlessCollectionsTests
    {
        [Fact]
        public void Component_SerializeHeadlessBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            var list = new ItemList();
            list.items = new Item[1] { new Item() };
            list.items[0].value = new Dummy() { value = 3.14 };

            //Act
            var schema = AvroConvert.GenerateSchema(typeof(ItemList));
            var bytes = AvroConvert.SerializeHeadless(list, schema);
            var deserialized = AvroConvert.DeserializeHeadless<ItemList>(bytes, schema);

            //Assert
            Assert.NotNull(bytes);
            Assert.NotNull(deserialized);
            Assert.Equal(3.14, deserialized.items[0].value.value);
        }
    }

    public class ItemList
    {
        public Item[] items { get; set; }
    }
    public class Dummy
    {
        public double value { get; set; }
    }
    public class Item
    {
        public Dummy value { get; set; }
    }
}
