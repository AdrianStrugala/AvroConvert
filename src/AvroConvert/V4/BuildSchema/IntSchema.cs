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

using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.V4.BuildSchema
{
    /// <summary>
    ///     Class represents an int schema.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#schema_primitive">the specification</a>.
    /// </summary>
    internal class IntSchema : PrimitiveTypeSchema
    {
        internal IntSchema()
            : this(typeof(int))
        {
        }

        internal IntSchema(Type runtimeType)
            : this(runtimeType, new Dictionary<string, string>())
        {
        }

        internal IntSchema(Type runtimeType, Dictionary<string, string> attributes)
            : base(runtimeType, attributes)
        {
        }

        internal override global::SolTechnology.Avro.V4.Schema.Schema.Type Type => global::SolTechnology.Avro.V4.Schema.Schema.Type.Int;
    }
}
