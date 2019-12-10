using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    public class WriteStep
    {
        public Encoder.WriteItem WriteField { get; set; }
        public Field Field { get; set; }
    }
}