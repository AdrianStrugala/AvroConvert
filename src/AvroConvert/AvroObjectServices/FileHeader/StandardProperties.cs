﻿#region license
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

using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader
{
    /// <summary>
    ///     Class containing standard properties for different avro types.
    /// </summary>
    internal static class StandardProperties
    {
        internal static readonly HashSet<string> Primitive = new HashSet<string> { AvroKeywords.Type };

        internal static readonly HashSet<string> Record = new HashSet<string>
        {
            AvroKeywords.Type,
            AvroKeywords.Name,
            AvroKeywords.Namespace,
            AvroKeywords.Doc,
            AvroKeywords.Aliases,
            AvroKeywords.Fields
        };

        internal static readonly HashSet<string> Enumeration = new HashSet<string>
        {
            AvroKeywords.Type,
            AvroKeywords.Name,
            AvroKeywords.Namespace,
            AvroKeywords.Doc,
            AvroKeywords.Aliases,
            AvroKeywords.Symbols
        };
    }
}
