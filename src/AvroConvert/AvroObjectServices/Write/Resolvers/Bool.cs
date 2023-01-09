using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers;

internal class Bool
{
    internal void Resolve(object value, IWriter encoder)
    {
        value ??= default(int);

        if (value is not bool convertedValue)
        {
            throw new AvroTypeMismatchException(
                $"[{typeof(bool)}] required to write against [{AvroType.Boolean}] schema but found type: [{value?.GetType()}]");
        }

        encoder.WriteBoolean(convertedValue);
    }
}