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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    public abstract class Schema
    {
        private static readonly ConcurrentDictionary<Type, TypeSchema> _schemaCache = new();
        private static readonly ConcurrentDictionary<(Type Type, OptionsKey Options), TypeSchema> _schemaWithOptionsCache = new();
        private string _json;

        protected Schema(IDictionary<string, string> attributes)
        {
            Attributes = (Dictionary<string, string>)(attributes ?? new Dictionary<string, string>());
        }

        internal Dictionary<string, string> Attributes { get; set; }

        internal void AddAttribute(string attribute, string value)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            Attributes.Add(attribute, value);
        }

        public override string ToString()
        {
            // Schemas are immutable once built, so the JSON form is computed once and reused by every Serialize call.
            return _json ??= BuildJson();
        }

        private string BuildJson()
        {
            using (var result = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonTextWriter(result);
                this.ToJson(writer, new HashSet<NamedSchema>());
                return result.ToString();
            }
        }

        internal void ToJson(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            this.ToJsonSafe(writer, seenSchemas);
        }


        internal abstract void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas);


        internal static TypeSchema Parse(string schemaInJson)
        {
            if (string.IsNullOrEmpty(schemaInJson))
            {
                throw new ArgumentNullException(nameof(schemaInJson));
            }

            return new TypeSchemaBuilder().BuildSchema(schemaInJson);
        }

        internal static TypeSchema Create(object obj, AvroConvertOptions options = null)
        {
            var type = obj?.GetType();
            if (type is null)
            {
                return new ReflectionSchemaBuilder(options).BuildSchema(null);
            }

            if (options is null)
            {
                return Create(type);
            }

            return _schemaWithOptionsCache.GetOrAdd((type, OptionsKey.From(options)),
                _ => new ReflectionSchemaBuilder(options).BuildSchema(type));
        }

        internal static TypeSchema Create(Type type) =>
            _schemaCache.GetOrAdd(type, t => new ReflectionSchemaBuilder().BuildSchema(t));

        /// <summary>
        /// Schema-affecting part of <see cref="AvroConvertOptions"/>. Naming policy and converters are keyed by their
        /// types (they are expected to be stateless), so the cache cannot grow with every options instance.
        /// </summary>
        private readonly record struct OptionsKey(
            bool IncludeOnlyDataContractMembers,
            int MaxItemsInSchemaTree,
            AvroNumberHandling NumberHandling,
            Type NamingPolicy,
            string Converters)
        {
            internal static OptionsKey From(AvroConvertOptions options) => new(
                options.IncludeOnlyDataContractMembers,
                options.MaxItemsInSchemaTree,
                options.NumberHandling,
                options.NamingPolicy?.GetType(),
                options.AvroConverters.Count == 0
                    ? null
                    : string.Join("|", options.AvroConverters.Select(c => c.GetType().AssemblyQualifiedName)));
        }
    }
}
