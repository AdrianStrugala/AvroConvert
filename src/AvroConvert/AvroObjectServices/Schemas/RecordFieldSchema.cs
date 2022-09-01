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
        private readonly NamedEntityAttributes namedEntityAttributes;
        private readonly TypeSchema typeSchema;
        private readonly bool hasDefaultValue;
        private readonly object defaultValue;
        private readonly int position;

        internal RecordFieldSchema(
            NamedEntityAttributes namedEntityAttributes,
            TypeSchema typeSchema,
            bool hasDefaultValue,
            object defaultValue,
            MemberInfo info,
            int position)
            : this(namedEntityAttributes, typeSchema, hasDefaultValue, defaultValue, info, position, new Dictionary<string, string>())
        {
        }

 
        internal RecordFieldSchema(
            NamedEntityAttributes namedEntityAttributes,
            TypeSchema typeSchema,
            bool hasDefaultValue,
            object defaultValue,
            MemberInfo info,
            int position,
            Dictionary<string, string> attributes)
            : base(attributes)
        {
            this.namedEntityAttributes = namedEntityAttributes;
            this.typeSchema = typeSchema;
            this.hasDefaultValue = hasDefaultValue;
            this.defaultValue = defaultValue;
            this.MemberInfo = info;
            this.position = position;
        }
        
        internal string Name => namedEntityAttributes.Name.Name;

        internal string Namespace => namedEntityAttributes.Name.Namespace;

        internal ReadOnlyCollection<string> Aliases => namedEntityAttributes.Aliases.AsReadOnly();

        internal string Doc => namedEntityAttributes.Doc;

        internal TypeSchema TypeSchema => typeSchema;

        internal bool HasDefaultValue => hasDefaultValue;

        internal object DefaultValue => defaultValue;

        internal int Position => position;

        internal MemberInfo MemberInfo { get; set; }

        internal NamedEntityAttributes NamedEntityAttributes => namedEntityAttributes;

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartObject();
            writer.WriteProperty("name", Name);
            writer.WriteOptionalProperty("namespace", Namespace);
            writer.WriteOptionalProperty("doc", Doc);
            writer.WriteOptionalProperty("aliases", Aliases);
            writer.WritePropertyName("type");
            TypeSchema.ToJson(writer, seenSchemas);
            writer.WriteOptionalProperty("default", defaultValue, hasDefaultValue);

            writer.WriteEndObject();
        }
        
    }
}
