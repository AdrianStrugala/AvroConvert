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
namespace AvroConvert.BuildSchema
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using global::AvroConvert.Attributes;
    using global::AvroConvert.Extensions;
    using TypeExtensions = Extensions.TypeExtensions;

    /// <summary>
    /// Allows using standard <see cref="T:System.Runtime.Serialization.DataContractAttribute"/> and 
    /// <see cref="T:System.Runtime.Serialization.DataMemberAttribute"/> attributes for defining what types/properties/fields
    /// should be serialized.
    /// </summary>
    public class AvroDataContractResolver : AvroContractResolver
    {
        private readonly bool _usePropertyNameAsAlias;
        private readonly bool allowNullable;
        private readonly bool useAlphabeticalOrder;


        public AvroDataContractResolver(bool usePropertyNameAsAlias, bool allowNullable = false, bool useAlphabeticalOrder = false)
        {
            _usePropertyNameAsAlias = usePropertyNameAsAlias;
            this.allowNullable = allowNullable;
            this.useAlphabeticalOrder = useAlphabeticalOrder;
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
        public override IEnumerable<Type> GetKnownTypes(Type type)
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
        /// <returns>
        /// Serialization information about the type.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The type argument is null.</exception>
        public override TypeSerializationInfo ResolveType(Type type)
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

            bool canContainNull = this.allowNullable || type.CanContainNull();

            if (type.IsInterface() ||
                type.IsNativelySupported() ||
                (type.IsEnum() && !type.GetTypeInfo().GetCustomAttributes(false).OfType<DataContractAttribute>().Any()))
            {
                return new TypeSerializationInfo
                {
                    Name = StripAvroNonCompatibleCharacters(type.Name),
                    Namespace = StripAvroNonCompatibleCharacters(type.Namespace),
                    Nullable = canContainNull
                };
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            var attributes = type.GetTypeInfo().GetCustomAttributes(false);
            var dataContract = attributes.OfType<DataContractAttribute>().SingleOrDefault();

            var name = StripAvroNonCompatibleCharacters(dataContract?.Name ?? type.Name);
            var nspace = StripAvroNonCompatibleCharacters(dataContract?.Namespace ?? type.Namespace);

            return new TypeSerializationInfo
            {
                Name = name,
                Namespace = nspace,
                Nullable = canContainNull
            };
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
        public override MemberSerializationInfo[] ResolveMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.IsKeyValuePair())
            {
                var keyValueProperties = type.GetAllProperties();

                if (_usePropertyNameAsAlias)
                {
                    return keyValueProperties.Select(p => new MemberSerializationInfo
                    {
                        Name = p.Name,
                        MemberInfo = p,
                        Nullable = false,
                        Aliases =
                        {
                            p.Name
                        }
                    }).ToArray();
                }
                else
                {
                    return keyValueProperties.Select(p => new MemberSerializationInfo
                    {
                        Name = p.Name,
                        MemberInfo = p,
                        Nullable = false
                    }).ToArray();
                }
            }

            var fields = type.GetAllFields();
            var dataMemberProperties = type.GetAllProperties();

            var serializedProperties = TypeExtensions.RemoveDuplicates(dataMemberProperties);
            TypeExtensions.CheckPropertyGetters(serializedProperties);

            var members = fields
                .Concat<MemberInfo>(serializedProperties)
                .Select(m => new
                {
                    Member = m,
                    Attribute = m.GetCustomAttributes(false).OfType<DataMemberAttribute>().SingleOrDefault(),
                    Nullable = m.GetCustomAttributes(false).OfType<NullableSchemaAttribute>().Any() || m.GetType().CanContainNull()
                });

            IEnumerable<MemberSerializationInfo> result;


            if (_usePropertyNameAsAlias)
            {
                result = members.Select(m => new MemberSerializationInfo
                {
                    Name = m.Attribute?.Name ?? m.Member.Name,
                    MemberInfo = m.Member,
                    Nullable = m.Nullable,
                    Aliases = { m.Member.Name }
                });
            }
            else
            {
                result = members.Select(m => new MemberSerializationInfo
                {
                    Name = m.Attribute?.Name ?? m.Member.Name,
                    MemberInfo = m.Member,
                    Nullable = m.Nullable
                });
            }

            if (this.useAlphabeticalOrder)
            {
                result = result.OrderBy(p => p.Name);
            }

            return result.ToArray();
        }
    }
}
