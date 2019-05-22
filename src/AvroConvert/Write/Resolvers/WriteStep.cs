namespace AvroConvert.Write.Resolvers
{
    using Schema;

    public class WriteStep
    {
        public Encoder.WriteItem WriteField { get; set; }
        public Field Field { get; set; }
    }
}