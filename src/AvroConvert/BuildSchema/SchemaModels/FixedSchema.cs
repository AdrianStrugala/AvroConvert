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

/** Modifications copyright(C) 2021 Adrian Struga³a **/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SolTechnology.Avro.Attributes;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{
    /// <summary>
    ///     Represents a fixed schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#Fixed">the specification</a>.
    /// </summary>
    internal sealed class FixedSchema : NamedSchema
    {
        internal FixedSchema(NamedEntityAttributes namedEntityAttributes, int size, Type runtimeType)
            : this(namedEntityAttributes, size, runtimeType, new Dictionary<string, string>())
        {
        }

        internal FixedSchema(
            NamedEntityAttributes namedEntityAttributes,
            int size,
            Type runtimeType,
            Dictionary<string, string> attributes) : base(namedEntityAttributes, runtimeType, attributes)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("size");
            }

            this.Size = size;
        }

        internal int Size { get; }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            if (seenSchemas.Contains(this))
            {
                writer.WriteValue(this.FullName);
                return;
            }

            seenSchemas.Add(this);
            writer.WriteStartObject();
            writer.WriteProperty("type", "fixed");
            writer.WriteProperty("name", this.FullName);
            writer.WriteOptionalProperty("aliases", this.Aliases);
            writer.WriteProperty("size", this.Size);
            writer.WriteEndObject();
        }

        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Fixed;
    }
}
