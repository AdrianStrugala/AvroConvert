using System.Collections.Generic;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.CodecTests
{
    public class TwitterExampleTests
    {
        private readonly byte[] _notCompressed;
        private readonly byte[] _snappy;

        public TwitterExampleTests()
        {
            _notCompressed = System.IO.File.ReadAllBytes("CodecTests/twitter.avro");
            _snappy = System.IO.File.ReadAllBytes("CodecTests/twitter.snappy.avro");
        }

        //TODO ComponentTestForSnappy

        [Fact]
        public void Deserialize_NotCompressed_DataIsDeserialized()
        {
            //Arrange
            List<TwitterModel> expected = new List<TwitterModel>();
            expected.Add(new TwitterModel
            {
                Timestamp = 1366150681,
                Tweet = "Rock: Nerf paper, scissors is fine.",
                Username = "miguno"
            });

            expected.Add(new TwitterModel
            {
                Timestamp = 1366154481,
                Tweet = "Works as intended.  Terran is IMBA.",
                Username = "BlizzardCS"
            });


            //Act
            var result = AvroConvert.Deserialize<List<TwitterModel>>(_notCompressed);


            //Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Deserialize_SnappyCodedFile_NotSupportedExceptionIsThrown()
        {
            //Arrange
            List<TwitterModel> expected = new List<TwitterModel>();
            expected.Add(new TwitterModel
            {
                Timestamp = 1366150681,
                Tweet = "Rock: Nerf paper, scissors is fine.",
                Username = "miguno"
            });

            expected.Add(new TwitterModel
            {
                Timestamp = 1366154481,
                Tweet = "Works as intended.  Terran is IMBA.",
                Username = "BlizzardCS"
            });


            //Act
            var result = AvroConvert.Deserialize<List<TwitterModel>>(_snappy);


            //Assert
            Assert.Equal(expected, result);
        }
    }
}
