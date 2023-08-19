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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.Abstract
{
    /// <summary>
    ///     Base class for all type schemas.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html">the specification</a>.
    /// </summary>
    public abstract class TypeSchema : BuildSchema.Schema
    {
        protected TypeSchema(Type runtimeType, IDictionary<string, string> attributes) : base(attributes)
        {
            if (runtimeType == null)
            {
                throw new ArgumentNullException("runtimeType");
            }

            RuntimeType = runtimeType;
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteValue(CultureInfo.InvariantCulture.TextInfo.ToLower(this.Type.ToString()));
        }

        internal Type RuntimeType { get; set; }

        internal virtual AvroType Type { get; }

        internal virtual bool CanRead(TypeSchema writerSchema) { return Type == writerSchema.Type; }

        internal virtual string Name => Type.ToString().ToLower(CultureInfo.InvariantCulture);
    }
}