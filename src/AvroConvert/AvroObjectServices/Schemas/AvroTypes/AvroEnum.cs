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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes
{
    /// <summary>
    /// Represents Avro enumeration.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Represents Avro enumeration.")]
    internal sealed class AvroEnum
    {
        private readonly EnumSchema schema;
        private int value;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvroEnum"/> class.
        /// </summary>
        /// <param name="schema">The schema.</param>
        internal AvroEnum(Schema schema)
        {
            this.schema = schema as EnumSchema;
            if (this.schema == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Enum schema expected."), "schema");
            }
        }

        /// <summary>
        /// Gets the schema.
        /// </summary>
        internal EnumSchema Schema
        {
            get { return schema; }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        internal string Value
        {
            get
            {
                return schema.GetSymbolByValue(value);
            }

            set
            {
                this.value = schema.GetValueBySymbol(value);
            }
        }

        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <value>
        /// The integer value.
        /// </value>
        internal int IntegerValue
        {
            get
            {
                return value;
            }

            set
            {
                this.value = value;
            }
        }
    }
}
