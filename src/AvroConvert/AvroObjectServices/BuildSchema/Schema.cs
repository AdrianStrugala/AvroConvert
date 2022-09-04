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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    internal abstract class Schema
    {
        protected Schema(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException("attributes");
            }

            Attributes = new Dictionary<string, string>(attributes);
        }

        internal Dictionary<string, string> Attributes { get; set; }

        internal void AddAttribute(string attribute, string value)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException("attribute");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
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


        internal static TypeSchema Create(string schemaInJson)
        {
            if (string.IsNullOrEmpty(schemaInJson))
            {
                throw new ArgumentNullException("schemaInJson");
            }

            return new JsonSchemaBuilder().BuildSchema(schemaInJson);
        }

        internal static TypeSchema Create(object obj)
        {
            var builder = new ReflectionSchemaBuilder();
            var schema = builder.BuildSchema(obj?.GetType());

            return schema;
        }

        internal static TypeSchema Create(Type type)
        {
            var builder = new ReflectionSchemaBuilder();
            var schema = builder.BuildSchema(type);

            return schema;
        }
    }
}
