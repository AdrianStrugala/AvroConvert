using System;
using System.Reflection;

namespace SolTechnology.Avro.Policies;

/// <summary>
/// Defines a way to naming types, fields and enum symbols based on their underlying type.
/// </summary>
public interface IAvroNamingPolicy
{
    /// <summary>
    /// Gets the name and namespace of the given type.
    /// </summary>
    AvroTypeInfo GetTypeName(Type type);

    /// <summary>
    /// Gets the property, field or enum name of the given member.
    /// </summary>
    string GetMemberName(MemberInfo member);
}