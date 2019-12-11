using System.Collections;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    public class Map
    {
        public Encoder.WriteItem Resolve(MapSchema mapSchema)
        {
            var itemWriter = Resolver.ResolveWriter(mapSchema.ValueSchema);
            return (v, e) => WriteMap(itemWriter, v, e);
        }

        private void WriteMap(Encoder.WriteItem itemWriter, object value, IWriter encoder)
        {
            EnsureMapObject(value);
            encoder.WriteMapStart();
            encoder.SetItemCount(GetMapSize(value));
            WriteMapValues(value, itemWriter, encoder);
            encoder.WriteMapEnd();
        }

        private void EnsureMapObject(object value)
        {
            if (value == null || !(value is IDictionary)) if (value != null) throw new AvroException("[IDictionary] required to write against [Map] schema but found " + value.GetType());
        }

        private long GetMapSize(object value)
        {
            return ((IDictionary)value).Count;
        }

        private void WriteMapValues(object map, Encoder.WriteItem valueWriter, IWriter encoder)
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