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

/** Modifications copyright(C) 2020 Adrian Strugala **/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SolTechnology.Avro.Attributes;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{
    /// <summary>
    ///     Class represents a record schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#schema_record">the specification</a>.
    /// </summary>
    internal sealed class RecordSchema : NamedSchema
    {
        private readonly List<RecordField> fields;
        private readonly Dictionary<string, RecordField> fiedsByName;

        internal RecordSchema(
            NamedEntityAttributes namedAttributes,
            Type runtimeType,
            Dictionary<string, string> attributes)
            : base(namedAttributes, runtimeType, attributes)
        {
            this.fields = new List<RecordField>();
            this.fiedsByName = new Dictionary<string, RecordField>();
        }

        internal RecordSchema(NamedEntityAttributes namedAttributes, Type runtimeType)
            : this(namedAttributes, runtimeType, new Dictionary<string, string>())
        {
        }

        internal void AddField(RecordField field)
        {
            if (field == null)
            {
                throw new ArgumentNullException("field");
            }

            fields.Add(field);
            fiedsByName.Add(field.Name, field);
        }

        internal bool TryGetField(string fieldName, out RecordField result)
        {
            return fiedsByName.TryGetValue(fieldName, out result);
        }
        
        internal RecordField GetField(string fieldName)
        {
            return fiedsByName[fieldName];
        }

        internal ReadOnlyCollection<RecordField> Fields => fields.AsReadOnly();

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            if (seenSchemas.Contains(this))
            {
                writer.WriteValue(this.FullName);
                return;
            }

            seenSchemas.Add(this);
            writer.WriteStartObject();
            writer.WriteProperty("type", "record");
            writer.WriteProperty("name", FullName);
            writer.WriteOptionalProperty("doc", Doc);
            writer.WriteOptionalProperty("aliases", Aliases);
            writer.WritePropertyName("fields");
            writer.WriteStartArray();
            this.fields.ForEach(_ => _.ToJson(writer, seenSchemas));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Record;
    }
}
