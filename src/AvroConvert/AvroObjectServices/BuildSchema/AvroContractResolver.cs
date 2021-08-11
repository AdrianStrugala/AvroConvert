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

/** Modifications copyright(C) 2021 Adrian Strugala **/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    /// Allows optionally using standard <see cref="T:System.Runtime.Serialization.DataContractAttribute"/> and 
    /// <see cref="T:System.Runtime.Serialization.DataMemberAttribute"/> attributes for defining what types/properties/fields
    /// should be serialized.
    /// </summary>


    internal class AvroContractResolver
    {

        private readonly bool _allowNullable;
        private readonly bool _useAlphabeticalOrder;
        private readonly bool _includeOnlyDataContractMembers;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvroContractResolver"/> class.
        /// </summary>
        /// <param name="usePropertyNameAsAlias">If set to <c>true</c>, Aliases get set to property names.</param>
        /// <param name="allowNullable">If set to <c>true</c>, null values are allowed.</param>
        /// <param name="useAlphabeticalOrder">If set to <c>true</c> use alphabetical data member order during serialization/deserialization.</param>
        /// <param name="includeOnlyDataContractMembers">If set to <c>true</c> members without DataMemberAttribute won't be taken into consideration in serialization/deserialization.</param>
        internal AvroContractResolver(bool allowNullable = false, bool useAlphabeticalOrder = false, bool includeOnlyDataContractMembers = false)
        {
            _allowNullable = allowNullable;
            _useAlphabeticalOrder = useAlphabeticalOrder;
            _includeOnlyDataContractMembers = includeOnlyDataContractMembers;
        }

        /// <summary>
        /// Gets the known types out of an abstract type or interface that could be present in the tree of
        /// objects serialized with this contract resolver.
        /// </summary>
        /// <param name="type">The abstract type.</param>
        /// <returns>
        /// An enumerable of known types.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        internal IEnumerable<Type> GetKnownTypes(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return new HashSet<Type>(type.GetAllKnownTypes());
        }

        /// <summary>
        /// Gets the serialization information about the type.
        /// This information is used for creation of the corresponding schema node.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <param name="member">The member type containing the type, if specified</param>
        /// <returns>
        /// Serialization information about the type.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        internal TypeSerializationInfo ResolveType(Type type, MemberInfo member = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsUnsupported())
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported by the resolver.", type));
            }

            bool isNullable = this._allowNullable || type.CanContainNull() || (!type.IsValueType && member?.IsNullableReferenceType() == true);

            if (type.IsInterface() ||
                type.IsNativelySupported() ||
                (type.IsEnum() && !type.GetTypeInfo().GetCustomAttributes(false).OfType<DataContractAttribute>().Any()))
            {
                return new TypeSerializationInfo
                {
                    Name = TypeExtensions.StripAvroNonCompatibleCharacters(type.Name),
                    Namespace = TypeExtensions.StripAvroNonCompatibleCharacters(type.Namespace),
                    Nullable = isNullable
                };
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            var attributes = type.GetTypeInfo().GetCustomAttributes(false);
            var dataContract = attributes.OfType<DataContractAttribute>().SingleOrDefault();
            if (dataContract == null && _includeOnlyDataContractMembers)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported by the resolver.", type));
            }

            var result = new TypeSerializationInfo
            {
                Name = TypeExtensions.StripAvroNonCompatibleCharacters(dataContract?.Name ?? type.Name),
                Namespace = TypeExtensions.StripAvroNonCompatibleCharacters(dataContract?.Namespace ?? type.Namespace),
                Nullable = isNullable,
                Doc = attributes.OfType<DescriptionAttribute>().SingleOrDefault()?.Description
            };
            return result;
        }

        /// <summary>
        /// Gets the serialization information about the type members.
        /// This information is used for creation of the corresponding schema nodes.
        /// </summary>
        /// <param name="type">The type, members of which should be serialized.</param>
        /// <returns>
        /// Serialization information about the fields/properties.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        internal MemberSerializationInfo[] ResolveMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsKeyValuePair())
            {
                var keyValueProperties = type.GetAllProperties();

                return keyValueProperties.Select(p => new MemberSerializationInfo
                {
                    Name = p.Name,
                    MemberInfo = p,
                    Nullable = false
                }).ToArray();
            }

            var allMembers = type.GetFieldsAndProperties(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            var membersToSerialize = type.GetFieldsAndProperties(BindingFlags.Public | BindingFlags.Instance);

            // add members that are explicitly marked with DataMember attribute
            foreach (var memberInfo in allMembers)
            {
                if (membersToSerialize.Contains(memberInfo))
                {
                    continue;
                }

                if (memberInfo.GetCustomAttributes(false).OfType<DataMemberAttribute>().Any())
                {
                    membersToSerialize.Add(memberInfo);
                }
            }

            var members = membersToSerialize
                .Where(x => !x.GetCustomAttributes(false).OfType<IgnoreDataMemberAttribute>().Any())
                .Select(m =>
                {
                    var customAttributes = m.GetCustomAttributes(false);

                    return new
                    {
                        Member = m,
                        Attribute = customAttributes.OfType<DataMemberAttribute>().SingleOrDefault(),
                        Nullable = customAttributes.OfType<NullableSchemaAttribute>().Any(), // m.GetType().CanContainNull() ||
                        DefaultValue = customAttributes.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value,
                        HasDefaultValue = customAttributes.OfType<DefaultValueAttribute>().Any(),
                        Doc = customAttributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description
                    };
                });


            if (_includeOnlyDataContractMembers)
            {
                members = members.Where(m => m.Attribute != null);
            }

            var result = members.Select(m => new MemberSerializationInfo
            {
                Name = m.Attribute?.Name ?? m.Member.Name,
                MemberInfo = m.Member,
                Nullable = m.Nullable,
                Aliases = m.Attribute?.Name != null ? new List<string> { m.Member.Name } : new List<string>(),
                HasDefaultValue = m.HasDefaultValue,
                DefaultValue = m.DefaultValue,
                Doc = m.Doc
            });


            if (this._useAlphabeticalOrder)
            {
                result = result.OrderBy(p => p.Name);
            }

            return result.ToArray();
        }
    }
}
