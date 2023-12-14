using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write;

internal partial class WriteResolver
{
    internal void ResolveBool(object value, IWriter encoder)
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