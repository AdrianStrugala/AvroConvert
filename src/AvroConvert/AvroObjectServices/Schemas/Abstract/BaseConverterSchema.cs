using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.Abstract
{
    public abstract class BaseConverterSchema : TypeSchema
    {

        internal BaseConverterSchema(Type runtimeType, IDictionary<string, string> attributes) : base(runtimeType, attributes)
        {
        }

        public BaseConverterSchema(Type runtimeType, AvroType avroType, string name, IDictionary<string, string> attributes = null) : base(runtimeType, attributes)
        {
            Type = avroType;
            Name = name;
        }
    }
}

//TODO:
// Add docs