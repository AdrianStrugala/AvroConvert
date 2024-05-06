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

/** Modifications copyright(C) 2022 Adrian Strugala **/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class RecordFieldSchema : Schema
    {
        internal string Name => NamedEntityAttributes.Name.Name;
        internal string Namespace => NamedEntityAttributes.Name.Namespace;
        internal ReadOnlyCollection<string> Aliases => NamedEntityAttributes.Aliases.AsReadOnly();
        internal string Doc => NamedEntityAttributes.Doc;
        internal TypeSchema TypeSchema { get; }
        internal bool HasDefaultValue { get; }
        internal object DefaultValue { get; }
        internal int Position { get; }
        internal NamedEntityAttributes NamedEntityAttributes { get; }

        internal string GetAliasOrDefault() => NamedEntityAttributes.Aliases.FirstOrDefault();

        internal RecordFieldSchema(
            NamedEntityAttributes namedEntityAttributes,
            TypeSchema typeSchema,
            bool hasDefaultValue,
            object defaultValue,
            int position)
            : this(namedEntityAttributes, typeSchema, hasDefaultValue, defaultValue, position, new Dictionary<string, string>())
        {
        }

        internal RecordFieldSchema(
            NamedEntityAttributes namedEntityAttributes,
            TypeSchema typeSchema,
            bool hasDefaultValue,
            object defaultValue,
            int position,
            Dictionary<string, string> attributes)
            : base(attributes)
        {
            NamedEntityAttributes = namedEntityAttributes;
            TypeSchema = typeSchema;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            Position = position;
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartObject();

            writer.WriteProperty("name", Name);
            writer.WriteOptionalProperty("namespace", Namespace);
            writer.WriteOptionalProperty("doc", Doc);
            writer.WriteOptionalProperty("aliases", Aliases);
            writer.WritePropertyName("type");
            TypeSchema.ToJson(writer, seenSchemas);
            writer.WriteOptionalProperty("default", DefaultValue, HasDefaultValue);

            writer.WriteEndObject();
        }
        
    }
}
