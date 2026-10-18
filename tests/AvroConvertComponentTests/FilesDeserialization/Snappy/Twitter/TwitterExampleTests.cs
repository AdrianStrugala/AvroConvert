using AwesomeAssertions;
using System.Collections.Generic;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.FilesDeserialization.Snappy.Twitter
{
    public class TwitterExampleTests
    {
        private readonly byte[] _notCompressed;
        private readonly byte[] _snappy;

        public TwitterExampleTests()
        {
            _notCompressed = System.IO.File.ReadAllBytes("FilesDeserialization/Snappy/Twitter/twitter.avro");
            _snappy = System.IO.File.ReadAllBytes("FilesDeserialization/Snappy/Twitter/twitter.snappy.avro");
        }

        [Test]
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
            result.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void Deserialize_SnappyCodedFile_DataIsDeserialized()
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
            result.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }
    }
}
