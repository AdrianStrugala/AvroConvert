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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.JsonToAvro;
using SolTechnology.Avro.Infrastructure;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Reflection;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    /// The schema of a type says nothing about a value of type <c>object</c>: it holds an empty "System.Object" record there.
    /// This class fills such places in from the values being written: one type gives that type, several types a union, a
    /// null adds "null". A field of a class takes the values of all its instances. Types without <c>object</c> are not touched.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal sealed class RuntimeSchema
    {
        // The schema for a declared schema and the types found in each place, so that repeating data does not build it again.
        private static readonly ConcurrentDictionary<(TypeSchema Declared, string Shape), TypeSchema> Shapes = new();
        private const int ShapesLimit = 1024;

        // Converters are found by the runtime type of a schema, which does not survive parsing, so schemas built for them have a cache of their own.
        private static readonly ConcurrentDictionary<(string Json, OptionsKey Options), TypeSchema> SchemasWithConverters = new();
        private const int SchemasWithConvertersLimit = 256;

        private const int Unknown = 0;
        private const int Open = 1;
        private const int Complete = 2;

        private readonly AvroConvertOptions _options;
        private readonly Dictionary<string, Slot> _slots = new();
        private readonly HashSet<object> _onPath = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<(RecordSchema, Type), RecordPlan> _plans = new();
        private readonly Dictionary<Type, TypeSchema> _valueSchemas = new();
        private readonly Dictionary<string, RecordSchema> _records = new();
        private RecordSchema _lastRecord;
        private Type _lastType;
        private RecordPlan _lastPlan;

        private RuntimeSchema(AvroConvertOptions options)
        {
            _options = options;
        }

        /// <summary>A place in the schema that holds <c>object</c> values, with what was found there.</summary>
        private sealed class Slot
        {
            internal bool HasNull;
            internal readonly List<Type> Types = new();
            internal readonly List<Type> CompleteTypes = new();
            internal readonly Dictionary<string, TypeSchema> Branches = new();
            internal readonly Dictionary<string, string> BranchSlots = new();
            internal readonly List<ExpandoObject> Expandos = new();
        }

        /// <summary>How to walk the instances of one class as one record: only the fields that hold <c>object</c>.</summary>
        private sealed class RecordPlan
        {
            internal (TypeSchema Schema, string Slot, MemberAccessor Member, string Name)[] Fields;
        }

        /// <summary>The empty record that stands for <c>object</c> in a schema generated from a type.</summary>
        internal static bool IsObjectPlaceholder(TypeSchema schema) =>
            schema is RecordSchema { FieldCount: 0 } record && record.FullName == typeof(object).FullName;

        /// <summary>True when <paramref name="schema"/> needs values to be complete.</summary>
        internal static bool IsOpen(TypeSchema schema)
        {
            if (schema.OpenState == Unknown)
            {
                schema.OpenState = FindPlaceholder(schema, new HashSet<TypeSchema>()) ? Open : Complete;
            }

            return schema.OpenState == Open;
        }

        private static bool FindPlaceholder(TypeSchema schema, HashSet<TypeSchema> visited) => schema switch
        {
            RecordSchema record when IsObjectPlaceholder(record) => true,
            RecordSchema record => visited.Add(record) && record.Fields.Any(field => FindPlaceholder(field.TypeSchema, visited)),
            ArraySchema array => FindPlaceholder(array.ItemSchema, visited),
            MapSchema map => FindPlaceholder(map.ValueSchema, visited),
            NullableSchema nullable => FindPlaceholder(nullable.ValueSchema, visited),
            UnionSchema union => union.Schemas.Any(branch => FindPlaceholder(branch, visited)),
            _ => false
        };

        /// <summary><paramref name="schema"/>, generated from a type, completed with the types of <paramref name="values"/>; the same object when it is complete already.</summary>
        internal static TypeSchema Resolve(TypeSchema schema, IEnumerable values, AvroConvertOptions options)
        {
            if (!IsOpen(schema))
            {
                return schema;
            }

            var runtime = new RuntimeSchema(options);
            runtime.CollectAll(schema, values);

            var shape = runtime.Shape();
            if (shape != null && Shapes.TryGetValue((schema, shape), out var known))
            {
                return known;
            }

            var resolved = Intern(runtime.Build(schema, string.Empty), options);
            if (shape != null)
            {
                if (Shapes.Count >= ShapesLimit)
                {
                    Shapes.Clear();
                }

                Shapes.TryAdd((schema, shape), resolved);
            }

            return resolved;
        }

        /// <summary>The schema of ExpandoObjects merged into one record. Built for every call, so it is interned to reuse one write plan.</summary>
        internal static TypeSchema ResolveExpandos(IEnumerable<ExpandoObject> expandos, AvroConvertOptions options, string name = null)
        {
            var schema = new JsonSchemaBuilder().BuildSchema(expandos, name);
            if (!IsOpen(schema))
            {
                return Intern(schema, options);
            }

            var runtime = new RuntimeSchema(options);
            runtime.CollectAll(schema, expandos);
            return Intern(runtime.Build(schema, string.Empty), options);
        }

        /// <summary>One schema object for one schema text: compiled write plans are cached per schema object.</summary>
        internal static TypeSchema Intern(TypeSchema schema, AvroConvertOptions options)
        {
            if (!(options?.AvroConverters.Count > 0))
            {
                return Schema.Parse(schema.ToString());
            }

            if (SchemasWithConverters.Count >= SchemasWithConvertersLimit)
            {
                SchemasWithConverters.Clear();
            }

            return SchemasWithConverters.GetOrAdd((schema.ToString(), OptionsKey.From(options)), schema);
        }

        /// <summary>The types found in each place, in order; null when the schema depends on more than types (fields of an ExpandoObject).</summary>
        private string Shape()
        {
            var shape = new StringBuilder();
            foreach (var slot in _slots)
            {
                if (slot.Value.Expandos.Count > 0)
                {
                    return null;
                }

                shape.Append(slot.Key).Append(slot.Value.HasNull ? '?' : ':');
                foreach (var type in slot.Value.Types)
                {
                    shape.Append(type.TypeHandle.Value.ToString("x", CultureInfo.InvariantCulture)).Append(',');
                }

                shape.Append(';');
            }

            return shape.ToString();
        }

        // ------------------------------------------------------------------ collecting the values

        private void CollectAll(TypeSchema schema, IEnumerable values)
        {
            foreach (var value in values)
            {
                Collect(schema, value, string.Empty);
            }
        }

        /// <summary>Walks a value along an open schema. Complete parts of the schema are never entered: nothing can be found there.</summary>
        private void Collect(TypeSchema schema, object value, string slot)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            switch (schema)
            {
                case RecordSchema when IsObjectPlaceholder(schema):
                    CollectObject(value, slot);
                    break;
                case NullableSchema nullable:
                    Collect(nullable.ValueSchema, value, slot);
                    break;
                case UnionSchema union:
                    foreach (var branch in OpenBranches(union, value))
                    {
                        Collect(branch, value, slot);
                    }
                    break;
                case RecordSchema record when value != null:
                    CollectRecord(record, value);
                    break;
                case ArraySchema array when value is IEnumerable items:
                    var itemSlot = slot + "[]";
                    foreach (var item in items)
                    {
                        Collect(array.ItemSchema, item, itemSlot);
                    }
                    break;
                case MapSchema map when value != null:
                    var valueSlot = slot + "{}";
                    foreach (var item in MapValues(value))
                    {
                        Collect(map.ValueSchema, item, valueSlot);
                    }
                    break;
            }
        }

        /// <summary>The branches a value can go to; of several records, the one of its own type.</summary>
        private IEnumerable<TypeSchema> OpenBranches(UnionSchema union, object value)
        {
            var open = union.Schemas.Where(IsOpen).ToList();
            if (open.Count < 2)
            {
                return open;
            }

            var own = value == null || value is ExpandoObject or JObject ? null : Schema.Create(value.GetType(), _options) as RecordSchema;
            return open.Where(branch => branch is not RecordSchema record || IsObjectPlaceholder(record) || record.FullName == own?.FullName);
        }

        private void CollectRecord(RecordSchema record, object value)
        {
            // On the path already: a cycle, which has nothing new to say here. Writing it is what fails.
            if (!_onPath.Add(value))
            {
                return;
            }

            foreach (var field in Plan(record, value.GetType()).Fields)
            {
                if (field.Member != null)
                {
                    Collect(field.Schema, field.Member.Get(value), field.Slot);
                }
                else if (((IDictionary<string, object>)value).TryGetValue(field.Name, out var item))
                {
                    Collect(field.Schema, item, field.Slot);
                }
            }

            _onPath.Remove(value);
        }

        private RecordPlan Plan(RecordSchema record, Type type)
        {
            if (ReferenceEquals(record, _lastRecord) && ReferenceEquals(type, _lastType))
            {
                return _lastPlan;
            }

            if (!_plans.TryGetValue((record, type), out var plan))
            {
                var isBag = typeof(IDictionary<string, object>).IsAssignableFrom(type);
                var members = isBag ? null : TypeMembers.For(type);
                plan = new RecordPlan
                {
                    Fields = record.Fields
                        .Where(field => IsOpen(field.TypeSchema))
                        .Select(field => (field, member: members?.Find(field.Aliases.FirstOrDefault() ?? field.Name)))
                        .Where(found => isBag || found.member is { CanRead: true })
                        .Select(found => (found.field.TypeSchema, FieldSlot(record, found.field), found.member, found.field.Name))
                        .ToArray()
                };
                _plans[(record, type)] = plan;
            }

            _lastRecord = record;
            _lastType = type;
            _lastPlan = plan;
            return plan;
        }

        private void CollectObject(object value, string key)
        {
            if (value is JValue token)
            {
                value = token.Value;
            }

            if (!_slots.TryGetValue(key, out var slot))
            {
                _slots[key] = slot = new Slot();
            }

            if (value == null)
            {
                slot.HasNull = true;
                return;
            }

            if (value is ExpandoObject expando)
            {
                slot.Expandos.Add(expando);
                return;
            }

            // The common case of a long list: a complete type seen before says nothing new.
            var type = value.GetType();
            if (slot.CompleteTypes.Contains(type))
            {
                return;
            }

            if (!slot.Types.Contains(type))
            {
                slot.Types.Add(type);
            }

            var schema = SchemaOf(type);
            if (IsObjectPlaceholder(schema))
            {
                return;
            }

            var branch = schema.UnionKey;
            if (!slot.Branches.TryGetValue(branch, out var existing))
            {
                slot.Branches[branch] = schema;
            }
            else if (!ReferenceEquals(existing, schema) && existing.ToString() != schema.ToString())
            {
                // A union holds one schema of each Avro type. Guid goes as text next to a string, the rest has no common form.
                if (!(existing is StringSchema && schema is UuidSchema))
                {
                    slot.Branches[branch] = schema is StringSchema && existing is UuidSchema
                        ? schema
                        : throw new ArgumentException(
                            $"A value of type object needs a union of {existing} and {schema}, which Avro does not allow: they have the same underlying type. " +
                            "Give the schema of the data: Serialize(value, schema).");
                }
            }

            if (!IsOpen(schema))
            {
                slot.CompleteTypes.Add(type);
                return;
            }

            if (!slot.BranchSlots.TryGetValue(branch, out var branchSlot))
            {
                slot.BranchSlots[branch] = branchSlot = $"{key}<{branch}>";
            }

            Collect(schema, value, branchSlot);
        }

        /// <summary>A JSON object is a map, its keys are data. An ExpandoObject is a record, as everywhere else: see <see cref="BuildObject"/>.</summary>
        private TypeSchema SchemaOf(Type type)
        {
            if (!_valueSchemas.TryGetValue(type, out var schema))
            {
                var described = type == typeof(JObject) ? typeof(Dictionary<string, object>) : type == typeof(JArray) ? typeof(List<object>) : type;
                _valueSchemas[type] = schema = Schema.Create(described, _options);
            }

            return schema;
        }

        private static IEnumerable MapValues(object value) => value switch
        {
            JObject json => json.Properties().Select(property => property.Value),
            IDictionary dictionary => dictionary.Values,
            IDictionary<string, object> dictionary => dictionary.Values,
            IEnumerable entries => entries.Cast<object>().Select(entry => entry.GetType().GetProperty("Value")!.GetValue(entry)),
            _ => Array.Empty<object>()
        };

        private static string FieldSlot(RecordSchema record, RecordFieldSchema field) => $"{record.FullName}.{field.Name}";

        // ------------------------------------------------------------------ building the schema

        private TypeSchema Build(TypeSchema schema, string slot)
        {
            if (!IsOpen(schema))
            {
                return schema;
            }

            switch (schema)
            {
                case RecordSchema when IsObjectPlaceholder(schema):
                    return BuildObject(schema, slot);
                case RecordSchema record:
                    return BuildRecord(record);
                case ArraySchema array:
                    return new ArraySchema(Build(array.ItemSchema, slot + "[]"), array.RuntimeType, array.Attributes);
                case MapSchema map:
                    return new MapSchema(map.KeySchema, Build(map.ValueSchema, slot + "{}"), map.RuntimeType, map.Attributes);
                case NullableSchema nullable:
                    return Build(nullable.ValueSchema, slot);
                case UnionSchema union:
                    return Union(union.Schemas.Select(branch => Build(branch, slot)));
                default:
                    return schema;
            }
        }

        private TypeSchema BuildObject(TypeSchema placeholder, string key)
        {
            if (!_slots.TryGetValue(key, out var slot) || !slot.HasNull && slot.Branches.Count == 0 && slot.Expandos.Count == 0)
            {
                return placeholder;
            }

            var branches = slot.Branches.Select(branch => Build(branch.Value, $"{key}<{branch.Key}>")).ToList();
            if (slot.Expandos.Count > 0)
            {
                branches.Add(BuildExpandos(slot.Expandos, key));
            }

            return Union(slot.HasNull ? branches.Prepend(new NullSchema()) : branches);
        }

        /// <summary>ExpandoObjects of one place are one record, named after the place: other places hold other fields.</summary>
        private TypeSchema BuildExpandos(List<ExpandoObject> expandos, string key)
        {
            var name = key.Length == 0
                ? null
                : "UnknownObject_" + Regex.Replace(key.Replace("[]", "_items").Replace("{}", "_values"), "[^0-9a-zA-Z_]+", "_");

            return ResolveExpandos(expandos, _options, name);
        }

        private TypeSchema BuildRecord(RecordSchema record)
        {
            if (_records.TryGetValue(record.FullName, out var built))
            {
                return built;
            }

            var names = new NamedEntityAttributes(new SchemaName(record.Name, record.Namespace), record.Aliases.ToList(), record.Doc);
            var result = new RecordSchema(names, record.RuntimeType, record.Attributes);
            _records[record.FullName] = result;

            foreach (var field in record.Fields)
            {
                result.AddField(new RecordFieldSchema(field.NamedEntityAttributes, Build(field.TypeSchema, FieldSlot(record, field)),
                    field.HasDefaultValue, field.DefaultValue, field.Position, field.Attributes)
                {
                    Order = field.Order,
                    DefaultJson = field.DefaultJson
                });
            }

            return result;
        }

        /// <summary>One schema as it is, several as a flat union with one branch per Avro type.</summary>
        private static TypeSchema Union(IEnumerable<TypeSchema> schemas)
        {
            var branches = schemas
                .SelectMany(schema => schema is UnionSchema union ? union.Schemas : (IEnumerable<TypeSchema>)new[] { schema })
                .GroupBy(schema => schema.UnionKey)
                .Select(group => group.First())
                .ToList();

            return branches.Count == 1 ? branches[0] : new UnionSchema(branches, typeof(object));
        }
    }
}
