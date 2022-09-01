using SolTechnology.Avro.Infrastructure.Attributes;

namespace KafkaApp
{
    public class RecordModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BatchId { get; set; }

        [NullableSchema]
        public string TextData { get; set; }

        [NullableSchema]
        public long NumericData { get; set; }
    }
}
