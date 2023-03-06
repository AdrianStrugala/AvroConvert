using System.Collections.Generic;
using System.IO;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.ComplexDeserialization
{
    public class DeserializeAzureEventHubTests
    {
        private readonly byte[] _azureEventHubBytes = File.ReadAllBytes("ComplexDeserialization/AzureEventHub.avro");


        [Fact]
        public void File_containing_map_with_multiple_values_is_deserialized()
        {
            //Arrange

            //just for reference
            var schema = @"{ 
   ""name"":""EventData"",
   ""namespace"":""Microsoft.ServiceBus.Messaging"",
   ""type"":""record"",
   ""fields"":[
      {
         ""name"":""SequenceNumber"",
         ""type"":""long""
      },
      {
         ""name"":""Offset"",
         ""type"":""string""
      },
      {
         ""name"":""EnqueuedTimeUtc"",
         ""type"":""string""
      },
      {
         ""name"":""SystemProperties"",
         ""type"":{
            ""type"":""map"",
            ""values"":[
               ""long"",
               ""double"",
               ""string"",
               ""bytes""
            ]
         }
      },
      {
         ""name"":""Properties"",
         ""type"":{
            ""type"":""map"",
            ""values"":[
               ""long"",
               ""double"",
               ""string"",
               ""bytes"",
               ""null""
            ]
         }
      },
      {
         ""name"":""Body"",
         ""type"":[
            ""null"",
            ""bytes""
         ]
      }
   ]
}";

            //Act
            var result = AvroConvert.Deserialize<List<EventData>>(_azureEventHubBytes);


            //Assert
            Assert.NotNull(result);
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
