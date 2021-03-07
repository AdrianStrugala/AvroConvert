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

/** Modifications copyright(C) 2020 Adrian Struga³a **/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{
    /// <summary>
    ///     Class representing a union schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#Unions">the specification</a>.
    /// </summary>
    internal sealed class UnionSchema : TypeSchema
    {
        private readonly List<TypeSchema> schemas;

        internal UnionSchema(
            List<TypeSchema> schemas,
            Type runtimeType,
            Dictionary<string, string> attributes)
            : base(runtimeType, attributes)
        {
            if (schemas == null)
            {
                throw new ArgumentNullException("schemas");
            }
            this.schemas = schemas;
        }

        internal UnionSchema(
            List<TypeSchema> schemas,
            Type runtimeType)
            : this(schemas, runtimeType, new Dictionary<string, string>())
        {
        }

        internal ReadOnlyCollection<TypeSchema> Schemas => schemas.AsReadOnly();

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartArray();
            this.schemas.ForEach(_ => _.ToJson(writer, seenSchemas));
            writer.WriteEndArray();
        }

        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Union;
    }
}
