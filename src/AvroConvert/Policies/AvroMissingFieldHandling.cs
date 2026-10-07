namespace SolTechnology.Avro.Policies;

/// <summary>
/// Controls what happens when the reader schema declares a field that is absent from the writer schema
/// and the reader field has no <c>default</c> value.
/// </summary>
public enum AvroMissingFieldHandling
{
    /// <summary>
    /// Follow the Avro specification: throw <see cref="System.Runtime.Serialization.SerializationException"/>.
    /// Fields that can hold <c>null</c> (union with <c>null</c>, reference types, <see cref="System.Nullable{T}"/>) are always resolved to <c>null</c>.
    /// </summary>
    Throw,

    /// <summary>
    /// Leave the member at its CLR default value (behaviour of AvroConvert 3.x).
    /// </summary>
    UseDefault
}
