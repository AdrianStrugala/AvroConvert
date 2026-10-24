namespace SolTechnology.Avro.Policies;

/// <summary>
/// Controls what happens when a <c>null</c> value is serialized against a schema that cannot hold <c>null</c>
/// (anything other than <c>"null"</c> or a union with <c>"null"</c>).
/// </summary>
public enum AvroNullValueHandling
{
    /// <summary>
    /// Follow the Avro specification: throw. <c>null</c> is a separate Avro type; a field that can be missing must be
    /// declared as a union with <c>"null"</c> (in C#: <c>string?</c>, <c>int?</c> or <c>[NullableSchema]</c>).
    /// </summary>
    Throw,

    /// <summary>
    /// Write the empty value of the schema instead: <c>""</c>, empty bytes, empty array or map, zero, <c>false</c>,
    /// and records field by field (behaviour of AvroConvert 3.x). The value reads back as that empty value, not as <c>null</c>.
    /// </summary>
    WriteEmpty
}
