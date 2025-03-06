namespace SolTechnology.Avro.Policies;

/// <summary>
/// Defines the name and namespace of a type.
/// </summary>
public class AvroTypeInfo
{
    /// <summary>
    /// Create a new instance of the <see cref="AvroTypeInfo"/>.
    /// </summary>
    /// <param name="name">The type name</param>
    /// <param name="ns">The type namespace</param>
    public AvroTypeInfo(string name, string ns)
    {
        Name = name;
        Namespace = ns;
    }

    /// <summary>
    /// The name of the type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The namespace of the type.
    /// </summary>
    public string Namespace { get; }
}