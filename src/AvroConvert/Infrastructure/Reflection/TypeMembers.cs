using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace SolTechnology.Avro.Infrastructure.Reflection
{
    /// <summary>A readable/writable instance member with compiled, boxed accessors.</summary>
    internal sealed class MemberAccessor
    {
        internal string Name { get; }
        internal Type Type { get; }
        internal bool CanRead => Get != null;
        internal bool CanWrite => Set != null;
        internal Func<object, object> Get { get; }
        internal Action<object, object> Set { get; }

        internal MemberAccessor(string name, Type type, Func<object, object> get, Action<object, object> set)
        {
            Name = name;
            Type = type;
            Get = get;
            Set = set;
        }
    }

    /// <summary>
    /// Replacement for FastMember: compiled getters/setters for the members that take part in Avro (de)serialization –
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
                        property.Name,
                        property.PropertyType,
                        property.GetMethod != null ? CompileGetter(type, property) : null,
                        property.SetMethod != null ? CompileSetter(type, property) : null));
                }

                foreach (var field in t.GetFields(all | BindingFlags.DeclaredOnly))
                {
                    // Skip compiler-generated backing fields (<Name>k__BackingField).
                    if (field.Name.IndexOf('<') >= 0 || !IsIncluded(field, field.IsPublic))
                    {
                        continue;
                    }

                    result.Add(new MemberAccessor(
                        field.Name,
                        field.FieldType,
                        CompileGetter(type, field),
                        field.IsInitOnly ? null : CompileSetter(type, field)));
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

        private static Action<object, object> CompileSetter(Type type, MemberInfo member)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            var memberType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;

            // Unbox (not Convert) so that assignments on boxed structs mutate the box itself.
            Expression target = type.IsValueType ? Expression.Unbox(instance, type) : Expression.Convert(instance, type);
            var body = Expression.Assign(Expression.MakeMemberAccess(target, member), Expression.Convert(value, memberType));
            return Expression.Lambda<Action<object, object>>(body, instance, value).Compile();
        }
    }
}
