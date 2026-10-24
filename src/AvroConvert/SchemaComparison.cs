#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion

using System.Collections.Generic;
using System.Linq;

namespace SolTechnology.Avro
{
    /// <summary>Result of <see cref="AvroSchema.Compare"/>.</summary>
    public sealed class SchemaComparison
    {
        internal SchemaComparison(bool areEquivalent, IReadOnlyList<SchemaDifference> differences)
        {
            AreEquivalent = areEquivalent;
            Differences = differences;
        }

        /// <summary>Same Parsing Canonical Form: both schemas read and write identical binary data.</summary>
        public bool AreEquivalent { get; }

        /// <summary>The reader schema can read data written with the writer schema (no breaking difference).</summary>
        public bool CanRead => Differences.All(d => !d.IsBreaking);

        /// <summary>Every difference relevant to schema resolution, breaking or not.</summary>
        public IReadOnlyList<SchemaDifference> Differences { get; }

        public override string ToString() =>
            AreEquivalent ? "Equivalent" : (CanRead ? "Readable" : "Not readable") + (Differences.Count > 0 ? ": " + string.Join("; ", Differences) : "");
    }

    /// <summary>Result of <see cref="AvroSchema.CheckCompatibility(AvroSchema, AvroSchema, AvroCompatibilityMode)"/>.</summary>
    public sealed class SchemaCompatibility
    {
        internal SchemaCompatibility(AvroCompatibilityMode mode, IReadOnlyList<SchemaDifference> incompatibilities)
        {
            Mode = mode;
            Incompatibilities = incompatibilities;
        }

        public AvroCompatibilityMode Mode { get; }

        public bool IsCompatible => Incompatibilities.Count == 0;

        /// <summary>Breaking differences found by the checks the mode requires.</summary>
        public IReadOnlyList<SchemaDifference> Incompatibilities { get; }

        public override string ToString() =>
            IsCompatible ? $"Compatible ({Mode})" : $"Incompatible ({Mode}): {string.Join("; ", Incompatibilities)}";
    }

    /// <summary>One difference between a writer and a reader schema.</summary>
    public sealed class SchemaDifference
    {
        internal SchemaDifference(string path, SchemaDifferenceKind kind, bool isBreaking, string description)
        {
            Path = path;
            Kind = kind;
            IsBreaking = isBreaking;
            Description = description;
        }

        /// <summary>Location in the schema, e.g. <c>Order.lines[].price</c> (<c>[]</c> array items, <c>{}</c> map values).</summary>
        public string Path { get; }

        public SchemaDifferenceKind Kind { get; }

        /// <summary>True when data written with the writer schema cannot be read with the reader schema because of it.</summary>
        public bool IsBreaking { get; }

        public string Description { get; }

        public override string ToString() => $"{Path}: {Description}{(IsBreaking ? " (breaking)" : "")}";
    }

    public enum SchemaDifferenceKind
    {
        /// <summary>Different types that cannot be resolved (e.g. string → int).</summary>
        TypeChanged,
        /// <summary>A type promotion allowed by the specification (int → long, float, double; long → float, double; float → double; string ↔ bytes).</summary>
        TypePromoted,
        /// <summary>Record, enum or fixed with a different name (and no matching alias).</summary>
        NameChanged,
        /// <summary>Field present in the reader only; breaking when it has no default.</summary>
        FieldAdded,
        /// <summary>Field present in the writer only; the reader skips it.</summary>
        FieldRemoved,
        /// <summary>Enum symbol present in the reader only.</summary>
        SymbolAdded,
        /// <summary>Enum symbol present in the writer only; breaking unless the reader enum has a default.</summary>
        SymbolRemoved,
        /// <summary>Union branch present in the reader only.</summary>
        UnionBranchAdded,
        /// <summary>Writer union branch the reader cannot read.</summary>
        UnionBranchRemoved,
        /// <summary>Fixed with a different size.</summary>
        SizeChanged,
        /// <summary>Different logical type on the same underlying type; breaking only for decimal precision/scale.</summary>
        LogicalTypeChanged
    }

    /// <summary>Compatibility modes, with the same meaning as in Confluent Schema Registry.</summary>
    public enum AvroCompatibilityMode
    {
        /// <summary>The new schema reads data written with the latest previous one.</summary>
        Backward,
        /// <summary>The latest previous schema reads data written with the new one.</summary>
        Forward,
        /// <summary>Backward and Forward.</summary>
        Full,
        /// <summary>The new schema reads data written with every previous one.</summary>
        BackwardTransitive,
        /// <summary>Every previous schema reads data written with the new one.</summary>
        ForwardTransitive,
        /// <summary>BackwardTransitive and ForwardTransitive.</summary>
        FullTransitive
    }
}
