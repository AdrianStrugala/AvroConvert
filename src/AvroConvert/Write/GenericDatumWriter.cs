namespace EhwarSoft.AvroConvert.Write
{
    using Schema;

    public class GenericDatumWriter : AbstractEncoder
    {
        private readonly Schema _schema;

        public GenericDatumWriter(Schema schema) : base(schema, new ArrayAccess(), new DictionaryMapAccess())
        {
            _schema = schema;
        }
    }
}