using AwesomeAssertions;
using System.Collections.Generic;
using System.IO;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.FilesDeserialization.Default
{
    public class DeserializeAzureEventHubTests
    {
        private readonly byte[] _azureEventHubBytes = File.ReadAllBytes("FilesDeserialization/Default/AzureEventHub.avro");


        [Test]
        public void File_containing_map_with_multiple_values_is_deserialized()
        {
            //Arrange


            //Act
            var result = AvroConvert.Deserialize<List<EventData>>(_azureEventHubBytes);


            //Assert
            result.Should().NotBeNull();
        }


        internal class EventData
        {
            public long SequenceNumber { get; set; }
            public string Offset { get; set; }
            public string EnqueuedTimeUtc { get; set; }
            public Dictionary<string, object> SystemProperties { get; set; }
            public Dictionary<string, object> Properties { get; set; }
            public byte[] Body { get; set; }
        }
    }
}
