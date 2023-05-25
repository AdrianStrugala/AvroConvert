using System.Collections.Generic;
using AvroConvertComponentTests.CodecTests.Snappy;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FilesDeserialization.Snappy.UserData
{
    public class Tests
    {
        private readonly string userData1 = "FilesDeserialization/Snappy/UserData/userdata1.avro";
        private readonly string userData2 = "FilesDeserialization/Snappy/UserData/userdata2.avro";
        private readonly string userData3 = "FilesDeserialization/Snappy/UserData/userdata3.avro";
        private readonly string userData4 = "FilesDeserialization/Snappy/UserData/userdata4.avro";
        private readonly string userData5 = "FilesDeserialization/Snappy/UserData/userdata5.avro";

        [Fact]
        public void Deserialize_SnappyFile1_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData1)));

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_SnappyFile2_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData2)));

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_SnappyFile3_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData3)));

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_SnappyFile4_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData4)));

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_SnappyFile5_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData5)));

            //Assert
            Assert.Null(result);
        }
    }
}
