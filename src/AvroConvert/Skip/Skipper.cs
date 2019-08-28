using AvroConvert.Read;
using AvroConvert.Schema;

namespace AvroConvert.Skip
{
    public class Skipper
    {
        public void Skip(Schema.Schema schema, IReader d)
        {
            switch (schema.Tag)
            {
                case Schema.Schema.Type.Null:
                    d.SkipNull();
                    break;
                case Schema.Schema.Type.Boolean:
                    d.SkipBoolean();
                    break;
                case Schema.Schema.Type.Int:
                    d.SkipInt();
                    break;
                case Schema.Schema.Type.Long:
                    d.SkipLong();
                    break;
                case Schema.Schema.Type.Float:
                    d.SkipFloat();
                    break;
                case Schema.Schema.Type.Double:
                    d.SkipDouble();
                    break;
                case Schema.Schema.Type.String:
                    d.SkipString();
                    break;
                case Schema.Schema.Type.Bytes:
                    d.SkipBytes();
                    break;
                case Schema.Schema.Type.Record:
                    foreach (Field f in (RecordSchema)schema) Skip(f.Schema, d);
                    break;
                case Schema.Schema.Type.Enumeration:
                    d.SkipEnum();
                    break;
                case Schema.Schema.Type.Fixed:
                    d.SkipFixed(((FixedSchema)schema).Size);
                    break;
                case Schema.Schema.Type.Array:
                    {
                        Schema.Schema s = ((ArraySchema)schema).ItemSchema;
                        for (long n = d.ReadArrayStart(); n != 0; n = d.ReadArrayNext())
                        {
                            for (long i = 0; i < n; i++) Skip(s, d);
                        }
                    }
                    break;
                case Schema.Schema.Type.Map:
                    {
                        Schema.Schema s = ((MapSchema)schema).ValueSchema;
                        for (long n = d.ReadMapStart(); n != 0; n = d.ReadMapNext())
                        {
                            for (long i = 0; i < n; i++) { d.SkipString(); Skip(s, d); }
                        }
                    }
                    break;
                case Schema.Schema.Type.Union:
                    Skip(((UnionSchema)schema)[d.ReadUnionIndex()], d);
                    break;
                case Schema.Schema.Type.Error:
                    break;
                default:
                    throw new AvroException("Unknown schema type: " + schema);
            }
        }
    }
}
