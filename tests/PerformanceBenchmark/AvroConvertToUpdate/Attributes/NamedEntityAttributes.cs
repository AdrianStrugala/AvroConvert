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
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.BuildSchema;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Attributes
{
    /// <summary>
    ///     Standard attributes supported by named schemas.
    /// </summary>
    internal sealed class NamedEntityAttributes
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NamedEntityAttributes" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="aliases">The aliases.</param>
        /// <param name="doc">The doc.</param>
        internal NamedEntityAttributes(SchemaName name, IEnumerable<string> aliases, string doc)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (aliases == null)
            {
                throw new ArgumentNullException("aliases");
            }

            this.Name = name;
            this.Aliases = new List<string>(aliases);
            this.Doc = string.IsNullOrEmpty(doc) ? string.Empty : doc;
        }

        /// <summary>
        ///     Gets the name.
        /// </summary>
        internal SchemaName Name { get; private set; }

        /// <summary>
        ///     Gets the aliases.
        /// </summary>
        internal List<string> Aliases { get; private set; }

        /// <summary>
        ///     Gets the doc.
        /// </summary>
        internal string Doc { get; private set; }
    }
}
