namespace SolTechnology.Avro.Policies;

public enum AvroNumberHandling
{
    /// <summary>
    /// Decimals that are too large for the defined scale are rejected.
    /// </summary>
    Strict,

    /// <summary>
    /// Decimals that are too large for the defined scale are truncated.
    /// </summary>
    Truncate,

    /// <summary>
    /// Decimals that are too large for the defined scale are rounded.
    /// </summary>
    Rounding
}