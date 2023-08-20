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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.Abstract
{
    /// <summary>
    ///     Class representing an named schema: record, enumeration or fixed.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#Names">the specification</a>.
    /// </summary>
    internal abstract class NamedSchema : TypeSchema
    {
        private readonly NamedEntityAttributes attributes;

        internal NamedSchema(
            NamedEntityAttributes nameAttributes,
            Type runtimeType,
            Dictionary<string, string> attributes)
            : base(runtimeType, attributes)
        {
            if (nameAttributes == null)
            {
                throw new ArgumentNullException("nameAttributes");
            }

            this.attributes = nameAttributes;
        }

        internal string FullName => this.attributes.Name.FullName;

        public override string Name => this.attributes.Name.Name;

        internal string Namespace => this.attributes.Name.Namespace;

        internal ReadOnlyCollection<string> Aliases => this.attributes.Aliases.AsReadOnly();

        internal string Doc => this.attributes.Doc;
    }
}
