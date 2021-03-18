using System;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.Schema.Abstract;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveFixed(FixedSchema writerSchema, TypeSchema readerSchema, IReader d, Type type)
        {
            FixedSchema rs = (FixedSchema)readerSchema;

            FixedModel ru = new FixedModel(rs);
            byte[] bb = ((FixedModel)ru).Value;
            d.ReadFixed(bb);

            if (type == typeof(Guid))
            {
                return new Guid(ru.Value);
            }

            return ru.Value;
        }
    }
}