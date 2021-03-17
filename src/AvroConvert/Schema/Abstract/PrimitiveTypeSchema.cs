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

namespace SolTechnology.Avro.Schema.Abstract
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
            AvroType t = writerSchema.Type;
            switch (Type)
            {
                case Avro.Schema.AvroType.Double:
                    return t == Avro.Schema.AvroType.Int || t == Avro.Schema.AvroType.Long || t == Avro.Schema.AvroType.Float;
                case Avro.Schema.AvroType.Float:
                    return t == Avro.Schema.AvroType.Int || t == Avro.Schema.AvroType.Long;
                case Avro.Schema.AvroType.Long:
                    return t == Avro.Schema.AvroType.Int;
                case Avro.Schema.AvroType.String:
                    return t == Avro.Schema.AvroType.String || t == Avro.Schema.AvroType.Null;
                default:
                    return false;
            }
        }
    }
}
