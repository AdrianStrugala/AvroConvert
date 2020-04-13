using System;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveFixed(FixedSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            FixedSchema rs = (FixedSchema)readerSchema;
            if (rs.Size != writerSchema.Size)
            {
                throw new AvroException("Size mismatch between reader and writer fixed schemas. Encoder: " +
                                        writerSchema +
                                        ", reader: " + readerSchema);
            }

            Fixed ru = new Fixed(rs);
            byte[] bb = ((Fixed)ru).Value;
            d.ReadFixed(bb);

            if (type == typeof(Guid))
            {
                return new Guid(ru.Value);
            }

            return ru.Value;
        }
    }
}