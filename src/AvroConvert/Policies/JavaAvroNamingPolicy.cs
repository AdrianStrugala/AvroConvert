using System;
using System.Reflection;
using System.Text;

namespace SolTechnology.Avro.Policies;

/// <summary>
/// Defines a naming policy that uses Java naming conventions.
/// </summary>
/// <remarks>
/// In the Avro specification, the most common naming convention to use is Java notation, which uses
/// lower Pascal-cased property and field names, upper Pascal-cased type names and upper snake-cased enum members.
/// </remarks>
public class JavaAvroNamingPolicy : IAvroNamingPolicy
{
    private readonly string _rootNamespace;

    /// <summary>
    /// Creates a new instance of the <see cref="JavaAvroNamingPolicy"/>.
    /// </summary>
    /// <param name="rootNamespace">The root namespace to use for all types.</param>
    public JavaAvroNamingPolicy(string rootNamespace)
    {
        this._rootNamespace = rootNamespace;
    }

    /// <inheritdoc cref="IAvroNamingPolicy"/>
    public virtual AvroTypeInfo GetTypeName(Type type)
    {
        return new AvroTypeInfo(type.Name, _rootNamespace);
    }

    /// <inheritdoc cref="IAvroNamingPolicy"/>
    public virtual string GetMemberName(MemberInfo member)
    {
        if (member.DeclaringType?.IsEnum == true)
        {
            return ToSnakeCase(member.Name).ToUpper();
        }

        return ToLowerPascalCase(member.Name);
    }

    private string ToSnakeCase(string name)
    {
        var result = new StringBuilder(name.Length);
        var upper = false;
        var previous = name[0];

        for (var i = 1; i < name.Length; i++)
        {
            var current = name[i];

            if (IsUpperOrDigit(current))
            {
                result.Append(char.ToLower(previous));

                if (upper == false && (i + 1 < name.Length && IsUpperOrDigit(name[i + 1]) || i + 1 == name.Length))
                {
                    result.Append('_');
                }

                upper = true;
            }
            else
            {
                if (upper)
                {
                    result.Append('_');
                }

                result.Append(char.ToLower(previous));
                upper = false;
            }

            previous = current;
        }

        result.Append(char.ToLower(previous));

        return result.ToString();
    }

    private string ToLowerPascalCase(string name)
    {
        return string.IsNullOrEmpty(name) 
            ? name
            : char.ToLower(name[0]) + name.Substring(1);
    }

    private bool IsUpperOrDigit(char c)
    {
        return char.IsUpper(c) || char.IsDigit(c);
    }
}