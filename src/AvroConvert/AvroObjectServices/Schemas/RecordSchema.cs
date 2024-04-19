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

/** Modifications copyright(C) 2021 Adrian Strugala **/

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{
    /// <summary>
    ///     Class represents a record schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#schema_record">the specification</a>.
    /// </summary>
    internal sealed class RecordSchema : NamedSchema
    {
        private readonly List<RecordFieldSchema> fields;
        private readonly Dictionary<string, RecordFieldSchema> fieldsByName;

        internal RecordSchema(
            NamedEntityAttributes namedAttributes,
            Type runtimeType,
            Dictionary<string, string> attributes)
            : base(namedAttributes, runtimeType, attributes)
        {
            fields = new List<RecordFieldSchema>();
            fieldsByName = new Dictionary<string, RecordFieldSchema>(StringComparer.InvariantCultureIgnoreCase);
        }

        internal RecordSchema(string name, string @namespace) : this(new NamedEntityAttributes(new SchemaName(name, @namespace), new List<string>(), string.Empty), typeof(object))
        {
        }

        internal RecordSchema(NamedEntityAttributes namedAttributes, Type runtimeType)
            : this(namedAttributes, runtimeType, new Dictionary<string, string>())
        {
        }

        internal void AddField(RecordFieldSchema field)
        {
            if (field == null)
            {
                throw new ArgumentNullException("field");
            }

            fields.Add(field);
            fieldsByName.Add(field.Name, field);
        }

        internal bool TryGetField(string fieldName, out RecordFieldSchema result)
        {
            return fieldsByName.TryGetValue(fieldName, out result);
        }

        internal RecordFieldSchema GetField(string fieldName)
        {
            return fieldsByName[fieldName];
        }

        internal ReadOnlyCollection<RecordFieldSchema> Fields => fields.AsReadOnly();

        internal override void ToJsonSafe(JsonTextWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteProperty("name", Name);
            writer.WriteOptionalProperty("namespace", Namespace);
            writer.WriteOptionalProperty("doc", Doc);
            writer.WriteOptionalProperty("aliases", Aliases);
            writer.WriteProperty("type", "record");
            writer.WritePropertyName("fields");
            writer.WriteStartArray();
            fields.ForEach(_ => _.ToJson(writer));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        internal override AvroType Type => AvroType.Record;

        internal override bool CanRead(TypeSchema writerSchema)
        {
            return Type == writerSchema.Type
                   || Fields.Count == 0; //hack to allow any item to be serialized to Object 
        }
    }
}
