using System;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Resolvers2
{
    internal partial class Resolver<T>
    {
        protected virtual object ResolveFixed<T>(FixedSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            FixedSchema rs = (FixedSchema)readerSchema;

            Fixed ru = new Fixed(rs);
            byte[] bb = ((Fixed)ru).Value;
            d.ReadFixed(bb);

            if (typeof(T) == typeof(Guid))
            {
                return new Guid(ru.Value);
            }

            return ru.Value;
        }
    }
}