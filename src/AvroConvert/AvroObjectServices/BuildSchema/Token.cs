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

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    ///     Class containing avro schema tokens.
    /// </summary>
    internal static class Token
    {
        internal const string Type = "type";
        internal const string Name = "name";
        internal const string Namespace = "namespace";
        internal const string Doc = "doc";
        internal const string Aliases = "aliases";
        internal const string Fields = "fields";
        internal const string Order = "order";
        internal const string Default = "default";
        internal const string Symbols = "symbols";
        internal const string Items = "items";
        internal const string Values = "values";
        internal const string Size = "size";
        internal const string LogicalType = "logicalType";
    }
}
