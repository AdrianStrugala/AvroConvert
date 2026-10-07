namespace SolTechnology.Avro.Policies;

/// <summary>
/// How a top-level collection passed to <c>AvroConvert.Serialize</c> is laid out in the Avro container file.
/// </summary>
public enum AvroCollectionMode
{
    /// <summary>
    /// Each element becomes one entry of the container file and the file schema is the element schema.
    /// This is how Avro tooling (Java, Python, Spark, Kafka) writes collections, so such files can be read row by row.
    /// </summary>
    Entries,

    /// <summary>
    /// The whole collection is written as a single <c>array</c> entry (AvroConvert 3.x behaviour).
    /// </summary>
    SingleArray
}
