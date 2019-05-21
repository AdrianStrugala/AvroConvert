namespace AvroConvert.Write.Resolvers
{
    using System.Collections;
    using Schema;

    public class Map
    {
        public void EnsureMapObject(object value)
        {
            if (value == null || !(value is IDictionary)) if (value != null) throw new AvroException("[IDictionary] required to write against [Map] schema but found " + value.GetType());
        }

        public long GetMapSize(object value)
        {
            return ((IDictionary)value).Count;
        }

        public void WriteMapValues(object map, Encoder.WriteItem valueWriter, IWriter encoder)
        {
            foreach (DictionaryEntry entry in ((IDictionary)map))
            {
                encoder.StartItem();
                encoder.WriteString(entry.Key.ToString());
                valueWriter(entry.Value, encoder);
            }
        }
    }
}