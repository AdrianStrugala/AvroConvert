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
using System.Globalization;
using Newtonsoft.Json;

namespace SolTechnology.Avro.BuildSchema.SchemaModels.Abstract
{
    internal abstract class PrimitiveTypeSchema : TypeSchema
    {
        protected PrimitiveTypeSchema(Type runtimeType, Dictionary<string, string> attributes)
            : base(runtimeType, attributes)
        {
        }
        
        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteValue(CultureInfo.InvariantCulture.TextInfo.ToLower(this.Type.ToString()));
        }

        internal override bool CanRead(TypeSchema writerSchema)
        {
            if (writerSchema is UnionSchema || Type == writerSchema.Type) return true;
            Avro.Schema.Schema.Type t = writerSchema.Type;
            switch (Type)
            {
                case Avro.Schema.Schema.Type.Double:
                    return t == Avro.Schema.Schema.Type.Int || t == Avro.Schema.Schema.Type.Long || t == Avro.Schema.Schema.Type.Float;
                case Avro.Schema.Schema.Type.Float:
                    return t == Avro.Schema.Schema.Type.Int || t == Avro.Schema.Schema.Type.Long;
                case Avro.Schema.Schema.Type.Long:
                    return t == Avro.Schema.Schema.Type.Int;
                case Avro.Schema.Schema.Type.String:
                    return t == Avro.Schema.Schema.Type.String || t == Avro.Schema.Schema.Type.Null;
                default:
                    return false;
            }
        }
    }
}
