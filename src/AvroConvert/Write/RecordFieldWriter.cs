namespace EhwarSoft.AvroConvert.Write
{
    using Schema;

    public class RecordFieldWriter
    {
        public AbstractEncoder.WriteItem WriteField { get; set; }
        public Field Field { get; set; }
    }
}