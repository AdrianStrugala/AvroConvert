#region license
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License.  You may obtain a copy
// of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

/** Modifications copyright(C) 2020 Adrian Strugała **/
#endregion

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
    internal static class TypeExtensions
    {
        internal static bool HasParameterlessConstructor(this Type type)
        {
            //return type.GetTypeInfo().GetConstructor(BindingFlags.Instance | BindingFlags.internal | BindingFlags.Noninternal, null, Type.EmptyTypes, null) != null;
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        internal static bool IsUnsupported(this Type type)
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
                && !(type.IsGenericType() && type.ImplementsSupportedInterface()));
        }

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

        internal static bool IsNativelySupported(this Type type)
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
            typeof(IDictionary<,>),
            typeof(IEnumerable<>)
        };

        private static bool ImplementsSupportedInterface(this Type type)
        {
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (SupportedInterfaces.Contains(interfaceType.GetGenericTypeDefinition()))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsAnonymous(this Type type)
        {
            return type.IsClass()
                && type.GetTypeInfo().GetCustomAttributes(false).Any(a => a is CompilerGeneratedAttribute)
                && !type.IsNested
                && type.Name.StartsWith("<>", StringComparison.Ordinal)
                && type.Name.Contains("__Anonymous");
        }

        internal static PropertyInfo GetPropertyByName(
            this Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
        {
            return type.GetProperty(name, flags);
        }

        internal static MethodInfo GetMethodByName(this Type type, string shortName, params Type[] arguments)
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

        internal static IEnumerable<FieldInfo> GetAllFields(this Type t)
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

        internal static IEnumerable<PropertyInfo> GetAllProperties(this Type t)
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

        public static List<MemberInfo> GetFieldsAndProperties(this Type type, BindingFlags bindingAttr)
        {
            if (type == null)
            {
                return new List<MemberInfo>();
            }

            List<MemberInfo> targetMembers = new List<MemberInfo>();

            targetMembers.AddRange(type.GetFields(bindingAttr));
            targetMembers.AddRange(type.GetProperties(bindingAttr));

            List<MemberInfo> distinctMembers = new List<MemberInfo>(targetMembers.Count);

            foreach (IGrouping<string, MemberInfo> groupedMember in targetMembers.GroupBy(m => m.Name))
            {
                distinctMembers.Add(groupedMember.First());
            }

            var result = distinctMembers
                .Where(FilterMembers)
                .Concat(GetFieldsAndProperties(type.BaseType(), bindingAttr))
                .ToList();

            return result;
        }


        private static bool FilterMembers(MemberInfo member)
        {
            if (member.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }
            if (member is PropertyInfo property)
            {
                if (IsIndexedProperty(property))
                {
                    return false;
                }

                return !IsByRefLikeType(property.PropertyType);
            }
            else if (member is FieldInfo field)
            {
                return !IsByRefLikeType(field.FieldType);
            }

            return true;
        }
        public static bool IsIndexedProperty(PropertyInfo property)
        {
            return (property.GetIndexParameters().Length > 0);
        }

        public static bool IsByRefLikeType(Type type)
        {
            if (!type.IsValueType())
            {
                return false;
            }

            // IsByRefLike flag on type is not available in netstandard2.0
            var attributes = type.GetCustomAttributes(false);
            for (int i = 0; i < attributes.Length; i++)
            {
                if (string.Equals(attributes[i].GetType().FullName, "System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<Type> GetAllInterfaces(this Type t)
        {
            foreach (var i in t.GetInterfaces())
            {
                yield return i;
            }
        }

        internal static string GetStrippedFullName(this Type type)
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

        internal static string StripAvroNonCompatibleCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(value, @"[^A-Za-z0-9_\.]", string.Empty, RegexOptions.None);
        }

        internal static bool IsFlagEnum(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return type.GetTypeInfo().GetCustomAttributes(false).ToList().Find(a => a is FlagsAttribute) != null;
        }

        internal static bool CanContainNull(this Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            //            return !type.IsValueType() || underlyingType != null;
            return underlyingType != null;
        }


        internal static bool IsKeyValuePair(this Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        internal static bool CanBeKnownTypeOf(this Type type, Type baseType)
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

        internal static IEnumerable<Type> GetAllKnownTypes(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<Type>();
            }

            return t.GetTypeInfo().GetCustomAttributes(true)
                .OfType<KnownTypeAttribute>()
                .Select(a => a.Type);
        }


        internal static void CheckPropertyGetters(IEnumerable<PropertyInfo> properties)
        {
            var missingGetter = properties.FirstOrDefault(p => p.GetGetMethod(true) == null);
            if (missingGetter != null)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Property '{0}' of class '{1}' does not have a getter.", missingGetter.Name, missingGetter.DeclaringType.FullName));
            }
        }

        internal static DataMemberAttribute GetDataMemberAttribute(this PropertyInfo property)
        {
            return property
                .GetCustomAttributes(false)
                .OfType<DataMemberAttribute>()
                .SingleOrDefault();
        }

        internal static IList<PropertyInfo> RemoveDuplicates(IEnumerable<PropertyInfo> properties)
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

        public static Type GetEnumeratedType(this Type type)
        {
            return type?.GetElementType() ?? type.GenericTypeArguments.FirstOrDefault();

        }

        internal static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        internal static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        internal static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        internal static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        internal static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        internal static bool IsDictionary(this Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type);
        }

        internal static bool IsList(this Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        internal static bool IsGuid(this Type type)
        {
            return type == typeof(Guid);
        }

        internal static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        internal static bool ContainsGenericParameters(this Type type)
        {
            return type.GetTypeInfo().ContainsGenericParameters;
        }

        internal static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
    }
}
