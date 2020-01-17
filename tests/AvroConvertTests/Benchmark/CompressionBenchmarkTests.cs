using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Avro.Codec;
using Xunit;

namespace AvroConvertTests.Benchmark
{
    public class CompressionBenchmarkTests
    {
        private readonly string rawDataset = System.IO.File.ReadAllText("Benchmark/big_dataset.json");


        [Fact]
        public void Compression_CompareSizesAndTime_VerifyDataQuality()
        {
            //Arrange
            List<Dataset> datasets = JsonConvert.DeserializeObject<List<Dataset>>(rawDataset);

            Stopwatch stopwatch = Stopwatch.StartNew();

            //Act
            var json = JsonConvert.SerializeObject(datasets);

            var timeJson = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            File.WriteAllText("10mega.json", rawDataset);


            var avro = AvroConvert.Serialize(datasets);
            File.WriteAllBytes("10mega.avro", avro);

            var timeAvro = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            var deflate = AvroConvert.Serialize(datasets, CodecType.Deflate);
            File.WriteAllBytes("10mega.deflate.avro", deflate);

            var timeDeflate = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            var snappy = AvroConvert.Serialize(datasets, CodecType.Snappy);
            File.WriteAllBytes("10mega.snappy.avro", snappy);

            var timeSnappy = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            File.WriteAllText("times.json", $"Json: {timeJson} \n" +
                                            $"Avro: {timeAvro} \n" +
                                            $"Deflate: {timeDeflate} \n" +
                                            $"Snappy: {timeSnappy}");

            //Assert
            Assert.Equal(datasets, JsonConvert.DeserializeObject<List<Dataset>>(json));
            Assert.Equal(datasets, AvroConvert.Deserialize<List<Dataset>>(avro));
            Assert.Equal(datasets, AvroConvert.Deserialize<List<Dataset>>(deflate));
            Assert.Equal(datasets, AvroConvert.Deserialize<List<Dataset>>(snappy));
        }
    }
}
