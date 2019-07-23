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
namespace AvroConvert.BuildSchema
{
    /// <summary>
    ///     Class containing avro schema tokens.
    /// </summary>
    internal static class Token
    {
        public const string Type = "type";
        public const string Name = "name";
        public const string Namespace = "namespace";
        public const string Doc = "doc";
        public const string Aliases = "aliases";
        public const string Fields = "fields";
        public const string Order = "order";
        public const string Default = "default";
        public const string Symbols = "symbols";
        public const string Items = "items";
        public const string Values = "values";
        public const string Size = "size";
    }
}
