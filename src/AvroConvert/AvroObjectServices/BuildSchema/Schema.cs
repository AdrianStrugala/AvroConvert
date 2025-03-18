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
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    public abstract class Schema
    {
        private static ConcurrentDictionary<Type, TypeSchema> _schemaCache = new();
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
            
            if (options is null && type is not null)
            {
                return _schemaCache.GetOrAdd(type,
                    t =>
                    {
                        var builder = new ReflectionSchemaBuilder(null);
                        var schema = builder.BuildSchema(t);

                        return schema;
                    });
            }
            else
            {
                var builder = new ReflectionSchemaBuilder(options);
                var schema = builder.BuildSchema(type);

                return schema;
            }
        }

        internal static TypeSchema Create(Type type) =>
            _schemaCache.GetOrAdd(type,
                t =>
                {
                    var builder = new ReflectionSchemaBuilder();
                    var schema = builder.BuildSchema(t);

                    return schema;
                });
    }
}
