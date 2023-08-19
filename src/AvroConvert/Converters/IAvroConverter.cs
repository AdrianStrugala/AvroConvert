using System;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro.Converters
{
    public interface IAvroConverter
    {
        public Type RuntimeType { get; }

        public TypeSchema TypeSchema { get; }

        public void Serialize(object data, IWriter writer);

        object Deserialize(Type readType, IReader reader);
    }
}
