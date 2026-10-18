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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.CompareSchema;
using SolTechnology.Avro.Features.GetSchema;
using SolTechnology.Avro.Features.JsonToAvro;

namespace SolTechnology.Avro
{
    /// <summary>
    /// An Avro schema: parsed once, immutable, with its Parsing Canonical Form, fingerprints and the schema evolution checks
    /// (comparison and compatibility). Two instances are equal when they have the same canonical form, i.e. when they read
    /// and write identical binary data.
    /// </summary>
    public sealed class AvroSchema : IEquatable<AvroSchema>
    {
        private readonly Lazy<string> _canonicalForm;
        private readonly Lazy<long> _fingerprint;

        private AvroSchema(TypeSchema schema)
        {
            TypeSchema = schema;
            // Computed from the schema text as written: names declared without a namespace take the enclosing one there,
            // which a schema built from .NET types does not carry in its model.
            _canonicalForm = new Lazy<string>(() => ParsingCanonicalForm.From(Schema.Parse(Json)));
            _fingerprint = new Lazy<long>(() => ParsingCanonicalForm.Crc64(CanonicalForm));
        }

        internal TypeSchema TypeSchema { get; }

        /// <summary>The schema as JSON (as written by AvroConvert).</summary>
        public string Json => TypeSchema.ToString();

        /// <summary>
        /// Parsing Canonical Form: full names, only the attributes relevant to reading data (no doc, aliases, defaults or
        /// logical types), fixed attribute order, no whitespace.
        /// </summary>
        public string CanonicalForm => _canonicalForm.Value;

        /// <summary>CRC-64-AVRO (Rabin) fingerprint of the canonical form, the fingerprint recommended by the Avro specification.</summary>
        public long Fingerprint => _fingerprint.Value;

        /// <summary>Parses a schema from its JSON definition.</summary>
        public static AvroSchema Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Schema JSON is empty", nameof(json));
            }

            return new AvroSchema(Schema.Parse(json));
        }

        /// <summary>Generates the schema of a .NET type.</summary>
        public static AvroSchema Create<T>(AvroConvertOptions options = null) => Create(typeof(T), options);

        /// <summary>Generates the schema of a .NET type.</summary>
        public static AvroSchema Create(Type type, AvroConvertOptions options = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new AvroSchema(Schema.Create(type, options));
        }

        /// <summary>Reads the schema from the header of an Avro object container file.</summary>
        public static AvroSchema FromAvro(byte[] avro)
        {
            using var stream = new MemoryStream(avro, writable: false);
            return FromAvro(stream);
        }

        /// <summary>Reads the schema from the header of an Avro object container file.</summary>
        public static AvroSchema FromAvro(Stream avro)
        {
            return Parse(new HeaderDecoder().GetSchema(avro));
        }

        /// <summary>
        /// Infers a schema from sample JSON data (not a schema definition – use <see cref="Parse"/> for that).
        /// </summary>
        public static AvroSchema InferFromJson(string jsonData)
        {
            AvroConvert.EnsureLicenseNotice();
            return new AvroSchema(new JsonSchemaBuilder().BuildSchema(JToken.Parse(jsonData)));
        }

        /// <summary>Fingerprint of the canonical form with the given algorithm (CRC-64-AVRO as 8 little-endian bytes, MD5, SHA-256).</summary>
        public byte[] GetFingerprint(AvroFingerprintAlgorithm algorithm) => algorithm switch
        {
            AvroFingerprintAlgorithm.Crc64Avro => BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(Fingerprint)
                : BitConverter.GetBytes(Fingerprint).Reverse().ToArray(),
            AvroFingerprintAlgorithm.Md5 => ParsingCanonicalForm.Md5(CanonicalForm),
            AvroFingerprintAlgorithm.Sha256 => ParsingCanonicalForm.Sha256(CanonicalForm),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };

        /// <summary>
        /// Compares a writer schema with a reader schema using the Avro schema resolution rules: whether data written with
        /// <paramref name="writer"/> can be read with <paramref name="reader"/>, and every difference found on the way.
        /// </summary>
        public static SchemaComparison Compare(AvroSchema writer, AvroSchema reader)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var differences = new SchemaComparer().Compare(writer.TypeSchema, reader.TypeSchema);
            return new SchemaComparison(writer.Equals(reader), differences);
        }

        /// <summary>
        /// Checks whether <paramref name="next"/> is compatible with <paramref name="previous"/> in the given mode
        /// (<see cref="AvroCompatibilityMode.Backward"/>: next reads data written with previous;
        /// <see cref="AvroCompatibilityMode.Forward"/>: previous reads data written with next; Full: both).
        /// </summary>
        public static SchemaCompatibility CheckCompatibility(AvroSchema previous, AvroSchema next, AvroCompatibilityMode mode)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            return CheckCompatibility(new[] { previous }, next, mode);
        }

        /// <summary>
        /// Checks whether <paramref name="next"/> is compatible with the schema history (oldest first). Non-transitive modes
        /// check against the latest version only, <c>*Transitive</c> modes against every version.
        /// </summary>
        public static SchemaCompatibility CheckCompatibility(IEnumerable<AvroSchema> history, AvroSchema next, AvroCompatibilityMode mode)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var versions = history.ToList();
            if (versions.Count == 0)
            {
                return new SchemaCompatibility(mode, Array.Empty<SchemaDifference>());
            }

            var transitive = mode is AvroCompatibilityMode.BackwardTransitive or AvroCompatibilityMode.ForwardTransitive or AvroCompatibilityMode.FullTransitive;
            var checkBackward = mode is AvroCompatibilityMode.Backward or AvroCompatibilityMode.BackwardTransitive or AvroCompatibilityMode.Full or AvroCompatibilityMode.FullTransitive;
            var checkForward = mode is AvroCompatibilityMode.Forward or AvroCompatibilityMode.ForwardTransitive or AvroCompatibilityMode.Full or AvroCompatibilityMode.FullTransitive;

            var incompatibilities = new List<SchemaDifference>();
            foreach (var previous in transitive ? versions : versions.Skip(versions.Count - 1))
            {
                if (checkBackward)
                {
                    incompatibilities.AddRange(Compare(writer: previous, reader: next).Differences.Where(d => d.IsBreaking));
                }

                if (checkForward)
                {
                    incompatibilities.AddRange(Compare(writer: next, reader: previous).Differences.Where(d => d.IsBreaking));
                }
            }

            return new SchemaCompatibility(mode, incompatibilities);
        }

        /// <summary>Same canonical form, i.e. identical binary data.</summary>
        public bool Equals(AvroSchema other) =>
            other is not null && (ReferenceEquals(this, other) || CanonicalForm == other.CanonicalForm);

        public override bool Equals(object obj) => Equals(obj as AvroSchema);

        public override int GetHashCode() => Fingerprint.GetHashCode();

        /// <summary>The schema JSON.</summary>
        public override string ToString() => Json;
    }

    /// <summary>Algorithms for <see cref="AvroSchema.GetFingerprint"/> defined by the Avro specification.</summary>
    public enum AvroFingerprintAlgorithm
    {
        /// <summary>64-bit Rabin fingerprint (8 bytes, little-endian); same value as <see cref="AvroSchema.Fingerprint"/>.</summary>
        Crc64Avro,
        /// <summary>MD5 (16 bytes).</summary>
        Md5,
        /// <summary>SHA-256 (32 bytes).</summary>
        Sha256
    }
}
