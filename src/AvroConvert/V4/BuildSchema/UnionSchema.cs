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

namespace SolTechnology.Avro.V4.BuildSchema
{
    /// <summary>
    ///     Class representing a union schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#Unions">the specification</a>.
    /// </summary>
    internal sealed class UnionSchema : TypeSchema
    {
        private readonly List<TypeSchema> schemas;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnionSchema" /> class.
        /// </summary>
        /// <param name="schemas">The schemas.</param>
        /// <param name="runtimeType">Type of the runtime.</param>
        /// <param name="attributes">The attributes.</param>
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

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnionSchema" /> class.
        /// </summary>
        /// <param name="schemas">The schemas.</param>
        /// <param name="runtimeType">Type of the runtime.</param>
        internal UnionSchema(
            List<TypeSchema> schemas,
            Type runtimeType)
            : this(schemas, runtimeType, new Dictionary<string, string>())
        {
        }

        /// <summary>
        ///     Gets the schemas.
        /// </summary>
        internal ReadOnlyCollection<TypeSchema> Schemas
        {
            get { return this.schemas.AsReadOnly(); }
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartArray();
            this.schemas.ForEach(_ => _.ToJson(writer, seenSchemas));
            writer.WriteEndArray();
        }

        /// <summary>
        /// Gets the type of the schema as string.
        /// </summary>
        internal override global::SolTechnology.Avro.V4.Schema.Schema.Type Type => global::SolTechnology.Avro.V4.Schema.Schema.Type.Union;
    }
}
