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

/** Modifications copyright(C) 2020 Adrian Struga�a **/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{
    /// <summary>
    ///     Schema representing an enumeration.
    ///     For more details please see <a href="http://avro.apache.org/docs/current/spec.html#Enums"> the specification</a>.
    /// </summary>
    internal sealed class EnumSchema : NamedSchema
    {
        private readonly List<string> symbols;
        private readonly List<long> avroToCSharpValueMapping;
        private readonly Dictionary<string, int> symbolToValue;
        private readonly Dictionary<int, string> valueToSymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSchema"/> class.
        /// </summary>
        /// <param name="namedEntityAttributes">The named entity attributes.</param>
        /// <param name="runtimeType">Type of the runtime.</param>
        internal EnumSchema(NamedEntityAttributes namedEntityAttributes, Type runtimeType)
            : this(namedEntityAttributes, runtimeType, new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumSchema" /> class.
        /// </summary>
        /// <param name="namedEntityAttributes">The named schema attributes.</param>
        /// <param name="runtimeType">Type of the runtime.</param>
        /// <param name="attributes">The attributes.</param>
        internal EnumSchema(
            NamedEntityAttributes namedEntityAttributes,
            Type runtimeType,
            Dictionary<string, string> attributes)
            : base(namedEntityAttributes, runtimeType, attributes)
        {
            if (runtimeType == null)
            {
                throw new ArgumentNullException("runtimeType");
            }

            this.symbols = new List<string>();
            this.symbolToValue = new Dictionary<string, int>();
            this.valueToSymbol = new Dictionary<int, string>();
            this.avroToCSharpValueMapping = new List<long>();
        }
        
        internal ReadOnlyCollection<string> Symbols => new (this.symbols);

        internal bool TryGetSymbolValue(string symbol, out int value) =>
            this.symbolToValue.TryGetValue(symbol, out value);

        internal int GetSymbolPosition(string symbol) =>
            this.symbols.IndexOf(symbol);
        
        internal string GetSymbolByValue(int value)
        {
            return this.valueToSymbol[value];
        }

        internal long[] AvroToCSharpValueMapping => this.avroToCSharpValueMapping.ToArray();

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            if (seenSchemas.Contains(this))
            {
                writer.WriteValue(this.FullName);
                return;
            }

            seenSchemas.Add(this);
            writer.WriteStartObject();
            writer.WriteProperty("type", "enum");
            writer.WriteProperty("name", Name);
            writer.WriteOptionalProperty("namespace", Namespace);
            writer.WriteOptionalProperty("doc", this.Doc);
            writer.WritePropertyName("symbols");
            writer.WriteStartArray();
            this.symbols.ForEach(writer.WriteValue);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        internal void AddSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Symbol should not be null."));
            }

            this.symbols.Add(symbol);
            this.symbolToValue.Add(symbol, this.symbolToValue.Count);
            this.valueToSymbol.Add(this.valueToSymbol.Count, symbol);

            if (this.avroToCSharpValueMapping.Any())
            {
                this.avroToCSharpValueMapping.Add(this.avroToCSharpValueMapping.Last() + 1);
            }
            else
            {
                this.avroToCSharpValueMapping.Add(0);
            }
        }

        internal override AvroType Type { get; set; } = AvroType.Enum;
    }
}
