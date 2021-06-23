using System;
using System.Linq;
using System.Reflection;

namespace SolTechnology.Avro.Infrastructure.Extensions
{
    internal static class MemberInfoExtensions
    {
        private static readonly byte[] EmptyFlags = {0};

        internal static bool IsNullableReferenceType(this MemberInfo member)
        {
            var nullableFlags = GetNullableFlags(member);

            return nullableFlags.Length > 0 && nullableFlags[0] == 2;
        }

        private static byte[] GetNullableFlags(MemberInfo member)
        {
            var attributes = member.GetCustomAttributes(true)
                .OfType<Attribute>()
                .ToArray();

            var nullableAttribute = attributes
                .FirstOrDefault(a => a.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");

            if (nullableAttribute != null)
            {
                return (byte[]) nullableAttribute.GetType()
                    .GetRuntimeField("NullableFlags")?
                    .GetValue(nullableAttribute) ?? EmptyFlags;
            }

            var typeAttributes = member.DeclaringType?.GetCustomAttributes(false);

            var nullableContextAttribute = typeAttributes?
                .FirstOrDefault(a => a.GetType().FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

            if (nullableContextAttribute != null)
            {
                var value = nullableContextAttribute.GetType()
                    .GetRuntimeField("Flag")
                    .GetValue(nullableContextAttribute) ?? 0;

                return new[] {(byte) value};
            }

            return EmptyFlags;
        }
    }
}