using System;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro.Converters
{
    public interface IAvroConverter
    {
        public TypeSchema TypeSchema { get; }

        public void Serialize(object data, IWriter writer);

        object Deserialize(IReader reader);
    }
}
