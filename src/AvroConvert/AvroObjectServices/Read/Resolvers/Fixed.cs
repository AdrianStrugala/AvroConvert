using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveFixed(FixedSchema writerSchema, TypeSchema readerSchema, IReader d, Type type)
        {
            FixedSchema rs = (FixedSchema)readerSchema;

            AvroFixed ru = new AvroFixed(rs);
            byte[] bb = ((AvroFixed)ru).Value;
            d.ReadFixed(bb);

            if (type == typeof(Guid))
            {
                return new Guid(ru.Value);
            }

            return ru.Value;
        }
    }
}