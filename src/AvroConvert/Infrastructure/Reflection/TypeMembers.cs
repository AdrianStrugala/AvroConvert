using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace SolTechnology.Avro.Infrastructure.Reflection
{
    /// <summary>An instance member taking part in Avro (de)serialization; writes are emitted directly by the compilers.</summary>
    internal sealed class MemberAccessor
    {
        internal string Name { get; }
        internal Type Type { get; }
        internal MemberInfo Info { get; }
        internal bool CanRead => Get != null;
        internal bool CanWrite { get; }
        internal Func<object, object> Get { get; }

        internal MemberAccessor(MemberInfo info, Type type, Func<object, object> get, bool canWrite)
        {
            Info = info;
            Name = info.Name;
            Type = type;
            Get = get;
            CanWrite = canWrite;
        }
    }

    /// <summary>
    /// Compiled getters and metadata for the members that take part in Avro (de)serialization –
    /// public properties and fields, plus non-public ones marked with <see cref="DataMemberAttribute"/>.
    /// </summary>
    internal sealed class TypeMembers
    {
        private static readonly ConcurrentDictionary<Type, TypeMembers> Cache = new();

        private readonly Dictionary<string, MemberAccessor> _exact = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MemberAccessor> _ignoreCase = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MemberAccessor> _members = new();

        internal IReadOnlyList<MemberAccessor> Members => _members;

        internal static TypeMembers For(Type type) => Cache.GetOrAdd(type, Build);

        internal MemberAccessor Find(string name)
        {
            if (_exact.TryGetValue(name, out var member) || _ignoreCase.TryGetValue(name, out member))
            {
                return member;
            }

            return null;
        }

        private static TypeMembers Build(Type type)
        {
            var result = new TypeMembers();
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Walk the hierarchy explicitly so private members declared on base classes are included.
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var property in t.GetProperties(all | BindingFlags.DeclaredOnly))
                {
                    if (property.GetIndexParameters().Length > 0 || !IsIncluded(property, property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true))
                    {
                        continue;
                    }

                    result.Add(new MemberAccessor(
                        property,
                        property.PropertyType,
                        property.GetMethod != null ? CompileGetter(type, property) : null,
                        property.SetMethod != null));
                }

                foreach (var field in t.GetFields(all | BindingFlags.DeclaredOnly))
                {
                    // Skip compiler-generated backing fields (<Name>k__BackingField).
                    if (field.Name.IndexOf('<') >= 0 || !IsIncluded(field, field.IsPublic))
                    {
                        continue;
                    }

                    result.Add(new MemberAccessor(
                        field,
                        field.FieldType,
                        CompileGetter(type, field),
                        !field.IsInitOnly));
                }
            }

            return result;
        }

        private static bool IsIncluded(MemberInfo member, bool isPublic) =>
            isPublic || member.IsDefined(typeof(DataMemberAttribute), true);

        private void Add(MemberAccessor member)
        {
            // Derived members win over hidden base members of the same name.
            if (_exact.TryAdd(member.Name, member))
            {
                _members.Add(member);
                _ignoreCase.TryAdd(member.Name, member);
            }
        }

        private static Func<object, object> CompileGetter(Type type, MemberInfo member)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var body = Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(instance, type), member), typeof(object));
            return Expression.Lambda<Func<object, object>>(body, instance).Compile();
        }

    }
}
