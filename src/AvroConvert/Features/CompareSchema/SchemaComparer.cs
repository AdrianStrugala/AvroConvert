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
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.Features.CompareSchema
{
    /// <summary>
    /// Walks a writer and a reader schema along the "Schema Resolution" rules of the Avro specification and collects the
    /// differences; a difference is breaking when data written with the writer schema cannot be read with the reader schema.
    /// </summary>
    internal sealed class SchemaComparer
    {
        private readonly List<SchemaDifference> _differences = new();
        private readonly HashSet<(string Writer, string Reader)> _comparedRecords = new();

        internal IReadOnlyList<SchemaDifference> Compare(TypeSchema writer, TypeSchema reader)
        {
            var root = Unwrap(reader) is NamedSchema named ? named.Name : "$";
            Compare(writer, reader, root);
            return _differences;
        }

        private void Compare(TypeSchema writer, TypeSchema reader, string path)
        {
            writer = Unwrap(writer);
            reader = Unwrap(reader);

            if (reader is UnionSchema readerUnion)
            {
                CompareToUnion(writer, readerUnion, path);
                return;
            }

            if (writer is UnionSchema writerUnion)
            {
                foreach (var branch in writerUnion.Schemas)
                {
                    if (Matches(branch, reader))
                    {
                        Compare(branch, reader, path);
                    }
                    else
                    {
                        Add(path, SchemaDifferenceKind.UnionBranchRemoved, true, $"writer union branch {Describe(branch)} cannot be read as {Describe(reader)}");
                    }
                }
                return;
            }

            if (writer is LogicalTypeSchema || reader is LogicalTypeSchema)
            {
                CompareLogical(writer, reader, path);
                return;
            }

            if (writer.Type != reader.Type)
            {
                if (writer is PrimitiveTypeSchema && reader is PrimitiveTypeSchema && SchemaPromotion.IsSpecPromotion(writer.Type, reader.Type))
                {
                    Add(path, SchemaDifferenceKind.TypePromoted, false, $"{Describe(writer)} → {Describe(reader)}");
                }
                else
                {
                    Add(path, SchemaDifferenceKind.TypeChanged, true, $"{Describe(writer)} → {Describe(reader)}");
                }
                return;
            }

            switch (writer)
            {
                case RecordSchema writerRecord:
                    CompareRecords(writerRecord, (RecordSchema)reader, path);
                    break;
                case EnumSchema writerEnum:
                    CompareEnums(writerEnum, (EnumSchema)reader, path);
                    break;
                case FixedSchema writerFixed:
                    CompareFixed(writerFixed, (FixedSchema)reader, path);
                    break;
                case ArraySchema writerArray:
                    Compare(writerArray.ItemSchema, ((ArraySchema)reader).ItemSchema, path + "[]");
                    break;
                case MapSchema writerMap:
                    Compare(writerMap.ValueSchema, ((MapSchema)reader).ValueSchema, path + "{}");
                    break;
            }
        }

        private void CompareToUnion(TypeSchema writer, UnionSchema reader, string path)
        {
            if (writer is not UnionSchema writerUnion)
            {
                var branch = FindBranch(reader, writer);
                if (branch == null)
                {
                    Add(path, SchemaDifferenceKind.TypeChanged, true, $"{Describe(writer)} → {Describe(reader)}");
                }
                else
                {
                    Compare(writer, branch, path);
                }
                return;
            }

            var used = new HashSet<TypeSchema>();
            foreach (var writerBranch in writerUnion.Schemas)
            {
                var readerBranch = FindBranch(reader, writerBranch);
                if (readerBranch == null)
                {
                    Add(path, SchemaDifferenceKind.UnionBranchRemoved, true, $"writer union branch {Describe(writerBranch)} has no match in {Describe(reader)}");
                    continue;
                }

                used.Add(readerBranch);
                Compare(writerBranch, readerBranch, path);
            }

            foreach (var readerBranch in reader.Schemas.Where(b => !used.Contains(b)))
            {
                Add(path, SchemaDifferenceKind.UnionBranchAdded, false, $"reader union adds {Describe(readerBranch)}");
            }
        }

        private void CompareLogical(TypeSchema writer, TypeSchema reader, string path)
        {
            var writerLogical = writer as LogicalTypeSchema;
            var readerLogical = reader as LogicalTypeSchema;

            if (writerLogical?.LogicalTypeName != readerLogical?.LogicalTypeName)
            {
                Add(path, SchemaDifferenceKind.LogicalTypeChanged, false,
                    $"logical type {writerLogical?.LogicalTypeName ?? "none"} → {readerLogical?.LogicalTypeName ?? "none"}; the values are read but interpreted differently");
            }
            else if (writerLogical is DecimalSchema writerDecimal && readerLogical is DecimalSchema readerDecimal
                     && (writerDecimal.Precision != readerDecimal.Precision || writerDecimal.Scale != readerDecimal.Scale))
            {
                // The specification resolves decimals only when precision and scale match.
                Add(path, SchemaDifferenceKind.LogicalTypeChanged, true,
                    $"decimal({writerDecimal.Precision},{writerDecimal.Scale}) → decimal({readerDecimal.Precision},{readerDecimal.Scale})");
            }

            Compare(writerLogical?.BaseTypeSchema ?? writer, readerLogical?.BaseTypeSchema ?? reader, path);
        }

        private void CompareRecords(RecordSchema writer, RecordSchema reader, string path)
        {
            if (!CompareNames(writer, reader, path) || !_comparedRecords.Add((writer.FullName, reader.FullName)))
            {
                return;
            }

            var matched = new HashSet<RecordFieldSchema>();
            foreach (var readerField in reader.Fields)
            {
                var fieldPath = path + "." + readerField.Name;
                var writerField = writer.Fields.FirstOrDefault(f => f.Name == readerField.Name)
                                  ?? writer.Fields.FirstOrDefault(f => readerField.Aliases.Contains(f.Name));

                if (writerField != null)
                {
                    matched.Add(writerField);
                    Compare(writerField.TypeSchema, readerField.TypeSchema, fieldPath);
                }
                else if (readerField.HasDefaultValue)
                {
                    Add(fieldPath, SchemaDifferenceKind.FieldAdded, false, "field added with a default");
                }
                else
                {
                    Add(fieldPath, SchemaDifferenceKind.FieldAdded, true, "field added without a default");
                }
            }

            foreach (var writerField in writer.Fields.Where(f => !matched.Contains(f)))
            {
                Add(path + "." + writerField.Name, SchemaDifferenceKind.FieldRemoved, false, "field removed; the reader skips it");
            }
        }

        private void CompareEnums(EnumSchema writer, EnumSchema reader, string path)
        {
            if (!CompareNames(writer, reader, path))
            {
                return;
            }

            foreach (var symbol in writer.Symbols.Where(s => !reader.Symbols.Contains(s)))
            {
                if (reader.Default != null)
                {
                    Add(path, SchemaDifferenceKind.SymbolRemoved, false, $"symbol {symbol} removed; read as the default {reader.Default}");
                }
                else
                {
                    Add(path, SchemaDifferenceKind.SymbolRemoved, true, $"symbol {symbol} removed and the reader enum has no default");
                }
            }

            foreach (var symbol in reader.Symbols.Where(s => !writer.Symbols.Contains(s)))
            {
                Add(path, SchemaDifferenceKind.SymbolAdded, false, $"symbol {symbol} added");
            }
        }

        private void CompareFixed(FixedSchema writer, FixedSchema reader, string path)
        {
            if (CompareNames(writer, reader, path) && writer.Size != reader.Size)
            {
                Add(path, SchemaDifferenceKind.SizeChanged, true, $"fixed size {writer.Size} → {reader.Size}");
            }
        }

        private bool CompareNames(NamedSchema writer, NamedSchema reader, string path)
        {
            if (NamesMatch(writer, reader))
            {
                return true;
            }

            Add(path, SchemaDifferenceKind.NameChanged, true, $"{writer.FullName} → {reader.FullName} (no matching alias)");
            return false;
        }

        /// <summary>Named types resolve by unqualified name, or by an alias of the reader.</summary>
        private static bool NamesMatch(NamedSchema writer, NamedSchema reader) =>
            writer.Name == reader.Name || reader.Aliases.Contains(writer.Name) || reader.Aliases.Contains(writer.FullName);

        /// <summary>First reader branch matching exactly, otherwise the first one the writer type promotes to.</summary>
        private static TypeSchema FindBranch(UnionSchema reader, TypeSchema writer) =>
            reader.Schemas.FirstOrDefault(b => SameKind(writer, b))
            ?? reader.Schemas.FirstOrDefault(b => Promotes(writer, b));

        private static bool Matches(TypeSchema writer, TypeSchema reader) => SameKind(writer, reader) || Promotes(writer, reader);

        private static bool SameKind(TypeSchema writer, TypeSchema reader)
        {
            var w = Base(writer);
            var r = Base(reader);
            if (w.Type != r.Type)
            {
                return false;
            }

            return w is not NamedSchema writerNamed || NamesMatch(writerNamed, (NamedSchema)r);
        }

        private static bool Promotes(TypeSchema writer, TypeSchema reader) =>
            Base(writer) is PrimitiveTypeSchema w && Base(reader) is PrimitiveTypeSchema r && SchemaPromotion.IsSpecPromotion(w.Type, r.Type);

        private static TypeSchema Unwrap(TypeSchema schema) => schema is NullableSchema nullable ? Unwrap(nullable.ValueSchema) : schema;

        private static TypeSchema Base(TypeSchema schema) => Unwrap(schema) is LogicalTypeSchema logical ? Base(logical.BaseTypeSchema) : Unwrap(schema);

        private static string Describe(TypeSchema schema) => Unwrap(schema) switch
        {
            LogicalTypeSchema logical => $"{Describe(logical.BaseTypeSchema)} ({logical.LogicalTypeName})",
            NamedSchema named => named.FullName,
            ArraySchema array => $"array<{Describe(array.ItemSchema)}>",
            MapSchema map => $"map<{Describe(map.ValueSchema)}>",
            UnionSchema union => "[" + string.Join(", ", union.Schemas.Select(Describe)) + "]",
            var other => other.Type.ToString().ToLowerInvariant()
        };

        private void Add(string path, SchemaDifferenceKind kind, bool breaking, string description) =>
            _differences.Add(new SchemaDifference(path, kind, breaking, description));
    }
}
