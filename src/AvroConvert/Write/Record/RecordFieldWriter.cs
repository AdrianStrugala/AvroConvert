namespace EhwarSoft.AvroConvert.Write.Record
{
    using Schema;

    public class RecordFieldWriter
    {
        public Encoder.WriteItem WriteField { get; set; }
        public Field Field { get; set; }
    }
}