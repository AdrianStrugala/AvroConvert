namespace AvroConvert.Write.Resolvers
{
    using Schema;

    public class Record
    {
        public Encoder.WriteItem WriteField { get; set; }
        public Field Field { get; set; }
    }
}