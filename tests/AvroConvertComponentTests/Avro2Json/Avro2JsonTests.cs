using AwesomeAssertions;
using System;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.Avro2Json
{
    public class Avro2JsonTests
    {
        private readonly byte[] _headerOnlyAvroBytes = File.ReadAllBytes("header_only.avro");

        /// <summary>A container of "int" with one data block per entry count given (an empty block for 0).</summary>
        private static byte[] ContainerOfBlocks(params long[] entriesPerBlock)
        {
            var file = new System.Collections.Generic.List<byte>(RoundTrip.RawContainer("\"int\"", Array.Empty<byte>(), 0));
            var sync = file.GetRange(file.Count - 16, 16);
            file.RemoveRange(file.Count - 16 - 2, 16 + 2); // the empty block: count 0, size 0, sync
            int value = 1;
            foreach (var entries in entriesPerBlock)
            {
                var data = new System.Collections.Generic.List<byte>();
                for (int i = 0; i < entries; i++)
                {
                    data.AddRange(RoundTrip.ZigZag(value++));
                }

                file.AddRange(RoundTrip.ZigZag(entries));
                file.AddRange(RoundTrip.ZigZag(data.Count));
                file.AddRange(data);
                file.AddRange(sync);
            }

            return file.ToArray();
        }

        [TestCase(new long[] { 1 }, "1")]
        [TestCase(new long[] { 1, 1 }, "[1,2]")]
        [TestCase(new long[] { 1, 0, 1 }, "[1,2]")]
        [TestCase(new long[] { 1, 0 }, "1")]
        [TestCase(new long[] { 0 }, "null")]
        [TestCase(new long[] { 2, 1 }, "[1,2,3]")]
        public void Avro2Json_EntriesInSeveralBlocks_OneEntryIsTheValueMoreAreAnArray(long[] entriesPerBlock, string expected)
        {
            AvroConvert.Avro2Json(ContainerOfBlocks(entriesPerBlock)).Should().Be(expected);
        }

        public class Account
        {
            public string UserName { get; set; }
            public System.Collections.Generic.Dictionary<int, string> Ids { get; set; }
        }

        [Test]
        [NonParallelizable] // JsonConvert.DefaultSettings is global
        public void Avro2Json_IgnoresTheGlobalJsonSettings_NamesAreTheSchemaNames()
        {
            var avro = AvroConvert.Serialize(new Account { UserName = "x", Ids = new() { [5] = "five" } });
            var previous = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Objects
            };
            try
            {
                AvroConvert.Avro2Json(avro).Should().Be("{\"UserName\":\"x\",\"Ids\":{\"5\":\"five\"}}");
            }
            finally
            {
                JsonConvert.DefaultSettings = previous;
            }
        }

        [Test]
        public void Avro2Json_ConvertNull_ProducedDesiredJson()
        {
            //Arrange
            string nullTestObject = null;

            var expectedJson = JsonConvert.SerializeObject(nullTestObject);

            var avroSerialized = AvroConvert.Serialize(nullTestObject);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            resultJson.Should().Be(expectedJson);
        }

        [Test]
        public void Avro2Json_ConvertFileContainingHeaderOnly_NoExceptionIsThrown()
        {
            //Arrange


            //Act
            var resultJson = AvroConvert.Avro2Json(_headerOnlyAvroBytes);


            //Assert
            resultJson.Should().Be(@"""""");
        }
        
    }
}
