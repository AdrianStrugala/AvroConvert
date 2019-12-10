using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace SolTechnology.Avro.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        ///     Checks if type t has a public parameter-less constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if type t has a public parameter-less constructor, false otherwise.</returns>
        public static bool HasParameterlessConstructor(this Type type)
        {
            //return type.GetTypeInfo().GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        ///     Determines whether the type is definitely unsupported for schema generation.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the type is unsupported; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUnsupported(this Type type)
        {
            return type == typeof(IntPtr)
                || type == typeof(UIntPtr)
                || type == typeof(object)
                || type.ContainsGenericParameters()
                || (!type.IsArray
                && !type.IsValueType()
                && !type.IsAnonymous()
                && !type.HasParameterlessConstructor()
                && type != typeof(string)
                && type != typeof(Uri)
                && !type.IsAbstract()
                && !type.IsInterface()
                && !(type.IsGenericType() && SupportedInterfaces.Contains(type.GetGenericTypeDefinition())));
        }

        /// <summary>
        /// The natively supported types.
        /// </summary>
        private static readonly HashSet<Type> NativelySupported = new HashSet<Type>
        {
            typeof(char),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(uint),
            typeof(int),
            typeof(bool),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(Uri),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid)
        };

        public static bool IsNativelySupported(this Type type)
        {
            var notNullable = Nullable.GetUnderlyingType(type) ?? type;
            return NativelySupported.Contains(notNullable)
                || type.IsArray
                || type.IsKeyValuePair()
                || type.GetAllInterfaces()
                       .FirstOrDefault(t => t.IsGenericType() &&
                                            t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) != null;
        }

        private static readonly HashSet<Type> SupportedInterfaces = new HashSet<Type>
        {
            typeof(IList<>),
            typeof(IDictionary<,>)
        };

        public static bool IsAnonymous(this Type type)
        {
            return type.IsClass()
                && type.GetTypeInfo().GetCustomAttributes(false).Any(a => a is CompilerGeneratedAttribute)
                && !type.IsNested
                && type.Name.StartsWith("<>", StringComparison.Ordinal)
                && type.Name.Contains("__Anonymous");
        }

        public static PropertyInfo GetPropertyByName(
            this Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
        {
            return type.GetProperty(name, flags);
        }

        public static MethodInfo GetMethodByName(this Type type, string shortName, params Type[] arguments)
        {
            var result = type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(m => m.Name == shortName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments));

            if (result != null)
            {
                return result;
            }

            return
                type
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => (m.Name.EndsWith(shortName, StringComparison.Ordinal) ||
                                       m.Name.EndsWith("." + shortName, StringComparison.Ordinal))
                                 && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments));
        }

        /// <summary>
        /// Gets all fields of the type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>Collection of fields.</returns>
        public static IEnumerable<FieldInfo> GetAllFields(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<FieldInfo>();
            }

            const BindingFlags Flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;
            return t
                .GetFields(Flags)
                .Where(f => !f.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .Concat(GetAllFields(t.BaseType()));
        }

        /// <summary>
        /// Gets all properties of the type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>Collection of properties.</returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<PropertyInfo>();
            }

            const BindingFlags Flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;

            return t
                .GetProperties(Flags)
                .Where(p => !p.IsDefined(typeof(CompilerGeneratedAttribute), false)
                            && p.GetIndexParameters().Length == 0)
                .Concat(GetAllProperties(t.BaseType()));
        }

        public static IEnumerable<Type> GetAllInterfaces(this Type t)
        {
            foreach (var i in t.GetInterfaces())
            {
                yield return i;
            }
        }

        public static string GetStrippedFullName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (string.IsNullOrEmpty(type.Namespace))
            {
                return StripAvroNonCompatibleCharacters(type.Name);
            }

            return StripAvroNonCompatibleCharacters(type.Namespace + "." + type.Name);
        }

        public static string StripAvroNonCompatibleCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(value, @"[^A-Za-z0-9_\.]", string.Empty, RegexOptions.None);
        }

        public static bool IsFlagEnum(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return type.GetTypeInfo().GetCustomAttributes(false).ToList().Find(a => a is FlagsAttribute) != null;
        }

        public static bool CanContainNull(this Type type)
        {
            //            var underlyingType = Nullable.GetUnderlyingType(type);
            //            return !type.IsValueType() || underlyingType != null;

            return Nullable.GetUnderlyingType(type) != null || type == typeof(string);
        }

        public static bool IsKeyValuePair(this Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        public static bool CanBeKnownTypeOf(this Type type, Type baseType)
        {
            return !type.IsAbstract()
                   && (type.GetTypeInfo().IsSubclassOf(baseType)
                   || type == baseType
                   || (baseType.IsInterface() && baseType.IsAssignableFrom(type))
                   || (baseType.IsGenericType() && baseType.IsInterface() && baseType.GenericIsAssignable(baseType)
                           && type.GetGenericArguments()
                                  .Zip(baseType.GetGenericArguments(), (type1, type2) => new Tuple<Type, Type>(type1, type2))
                                  .ToList()
                                  .TrueForAll(tuple => CanBeKnownTypeOf(tuple.Item1, tuple.Item2))));
        }

        private static bool GenericIsAssignable(this Type type, Type instanceType)
        {
            if (!type.IsGenericType() || !instanceType.IsGenericType())
            {
                return false;
            }

            var args = type.GetGenericArguments();
            return args.Any() && type.IsAssignableFrom(instanceType.GetGenericTypeDefinition().MakeGenericType(args));
        }

        public static IEnumerable<Type> GetAllKnownTypes(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<Type>();
            }

            return t.GetTypeInfo().GetCustomAttributes(true)
                .OfType<KnownTypeAttribute>()
                .Select(a => a.Type);
        }


        public static void CheckPropertyGetters(IEnumerable<PropertyInfo> properties)
        {
            var missingGetter = properties.FirstOrDefault(p => p.GetGetMethod(true) == null);
            if (missingGetter != null)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Property '{0}' of class '{1}' does not have a getter.", missingGetter.Name, missingGetter.DeclaringType.FullName));
            }
        }

        public static DataMemberAttribute GetDataMemberAttribute(this PropertyInfo property)
        {
            return property
                .GetCustomAttributes(false)
                .OfType<DataMemberAttribute>()
                .SingleOrDefault();
        }

        public static IList<PropertyInfo> RemoveDuplicates(IEnumerable<PropertyInfo> properties)
        {
            var result = new List<PropertyInfo>();
            foreach (var p in properties)
            {
                if (result.Find(s => s.Name == p.Name) == null)
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        public static bool IsDictionary(this Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type);
        }

        public static bool IsList(this Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        public static bool IsGuid(this Type type)
        {
            return type == typeof(Guid);
        }

        public static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        public static bool ContainsGenericParameters(this Type type)
        {
            return type.GetTypeInfo().ContainsGenericParameters;
        }

        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
    }
}
