#region license

/**Copyright (c) 2023 Adrian Strugała
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Reflection;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<(RecordSchema Writer, RecordSchema Reader, Type Type), RecordPlan> _recordPlans = new();
        private static readonly ConcurrentDictionary<string, Type> ClrTypeBySchemaName = new();

        protected virtual object ResolveRecord(
            RecordSchema writerSchema,
            RecordSchema readerSchema,
            IReader reader,
            Type type)
        {
            if (type != typeof(object))
            {
                return ReadForType(writerSchema, readerSchema, reader, type);
            }

            // Dynamic target: prefer a CLR type whose name matches the schema, otherwise an ExpandoObject.
            Type clrType = ClrTypeBySchemaName.GetOrAdd(readerSchema.FullName, _ => FindClrTypeForRecordSchema(readerSchema));
            if (clrType != null)
            {
                return ReadForType(writerSchema, readerSchema, reader, clrType);
            }

            var result = new ExpandoObject() as IDictionary<string, object>;

            foreach (RecordFieldSchema wf in writerSchema.Fields)
            {
                if (readerSchema.TryGetField(wf.Name, out var rf))
                {
                    string name = rf.Aliases.FirstOrDefault() ?? wf.Name;
                    var targetType = wf.TypeSchema.Type == AvroType.Array ? typeof(List<object>) : typeof(object);
                    result.Add(name, Resolve(wf.TypeSchema, rf.TypeSchema, reader, targetType) ?? wf.DefaultValue);
                }
                else
                {
                    _skipper.Skip(wf.TypeSchema, reader);
                }
            }

            foreach (RecordFieldSchema rf in readerSchema.Fields)
            {
                if (writerSchema.TryGetField(rf.Name, out _))
                {
                    continue;
                }

                string name = rf.Aliases.FirstOrDefault() ?? rf.Name;
                result.Add(name, ResolveMissingReaderField(readerSchema, rf, rf.DefaultValue));
            }

            return result;
        }

        private object ReadForType(RecordSchema writerSchema, RecordSchema readerSchema, IReader reader, Type type)
        {
            var key = (writerSchema, readerSchema, type);
            if (!_recordPlans.TryGetValue(key, out var plan))
            {
                plan = BuildRecordPlan(writerSchema, readerSchema, type);
                _recordPlans.Add(key, plan);
            }

            object result = RuntimeHelpers.GetUninitializedObject(type);

            foreach (var step in plan.Steps)
            {
                switch (step.Kind)
                {
                    case ReadStepKind.Skip:
                        _skipper.Skip(step.WriterField.TypeSchema, reader);
                        break;

                    case ReadStepKind.Read:
                        var value = Resolve(step.WriterField.TypeSchema, step.ReaderField.TypeSchema, reader, step.Member.Type)
                                    ?? FormatDefaultValue(step.WriterField.DefaultValue, step.Member);
                        step.Member.Set(result, value);
                        break;

                    case ReadStepKind.Default:
                        step.Member.Set(result, step.DefaultValue);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves writer fields against the reader schema and the CLR type once per (writer, reader, type):
        /// which fields are read into which member, which are skipped, and which reader-only fields get a default.
        /// </summary>
        private RecordPlan BuildRecordPlan(RecordSchema writerSchema, RecordSchema readerSchema, Type type)
        {
            var members = TypeMembers.For(type);
            var steps = new List<ReadStep>(writerSchema.Fields.Count);

            foreach (RecordFieldSchema wf in writerSchema.Fields)
            {
                if (!readerSchema.TryGetField(wf.Name, out var rf))
                {
                    steps.Add(ReadStep.Skip(wf));
                    continue;
                }

                var member = members.Find(rf.GetAliasOrDefault() ?? wf.Name);
                steps.Add(member is { CanWrite: true } ? ReadStep.Read(wf, rf, member) : ReadStep.Skip(wf));
            }

            foreach (var (member, value) in ResolveMissingReaderFields(writerSchema, readerSchema, members))
            {
                steps.Add(ReadStep.Default(member, value));
            }

            return new RecordPlan(steps.ToArray());
        }

        /// <summary>
        /// Avro schema resolution: reader fields absent from the writer take the reader's default;
        /// without a default, fields that can hold null resolve to null and anything else is governed by MissingFieldHandling.
        /// </summary>
        private IEnumerable<(MemberAccessor Member, object Value)> ResolveMissingReaderFields(
            RecordSchema writerSchema,
            RecordSchema readerSchema,
            TypeMembers members)
        {
            foreach (RecordFieldSchema rf in readerSchema.Fields)
            {
                if (writerSchema.TryGetField(rf.Name, out _))
                {
                    continue;
                }

                var member = members.Find(rf.GetAliasOrDefault() ?? rf.Name);
                if (member is not { CanWrite: true })
                {
                    continue;
                }

                object defaultValue = rf.HasDefaultValue ? FormatDefaultValue(rf.DefaultValue, member) : null;
                bool clrNullable = !member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) != null;
                var value = ResolveMissingReaderField(readerSchema, rf, defaultValue, clrNullable);
                if (value == null && !clrNullable)
                {
                    // UseDefault on a non-nullable value type: leave the CLR default rather than assigning null.
                    continue;
                }

                yield return (member, value);
            }
        }

        private object ResolveMissingReaderField(RecordSchema readerSchema, RecordFieldSchema rf, object formattedDefault, bool clrNullable = false)
        {
            if (rf.HasDefaultValue)
            {
                return formattedDefault;
            }

            bool nullable = clrNullable ||
                            rf.TypeSchema.Type == AvroType.Null ||
                            rf.TypeSchema is UnionSchema union && union.Schemas.Any(s => s.Type == AvroType.Null);
            if (nullable || _missingFieldHandling == AvroMissingFieldHandling.UseDefault)
            {
                return null;
            }

            throw new SerializationException(
                $"Field '{rf.Name}' of record '{readerSchema.FullName}' is not present in the writer schema and has no default value. " +
                $"Add a [DefaultValue] / \"default\" to the field, make it nullable, or set {nameof(AvroConvertOptions)}.{nameof(AvroConvertOptions.MissingFieldHandling)} = {nameof(AvroMissingFieldHandling.UseDefault)}.");
        }

        private object FormatDefaultValue(object defaultValue, MemberAccessor member)
        {
            if (defaultValue == null)
            {
                return null;
            }

            var t = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

            if (defaultValue.GetType() == t)
            {
                return defaultValue;
            }

            if (t.IsEnum)
            {
                return EnumParser.Parse(t, (string)defaultValue, _namingPolicy);
            }

            //TODO: Map and Record default values are represented as Dictionary<string,object>
            //https://avro.apache.org/docs/1.4.0/spec.html
            //It might be not supported at the moment

            return Convert.ChangeType(defaultValue, t);
        }

        private enum ReadStepKind { Read, Skip, Default }

        private sealed class RecordPlan
        {
            internal ReadStep[] Steps { get; }
            internal RecordPlan(ReadStep[] steps) => Steps = steps;
        }

        private sealed class ReadStep
        {
            internal ReadStepKind Kind { get; private init; }
            internal RecordFieldSchema WriterField { get; private init; }
            internal RecordFieldSchema ReaderField { get; private init; }
            internal MemberAccessor Member { get; private init; }
            internal object DefaultValue { get; private init; }

            internal static ReadStep Read(RecordFieldSchema wf, RecordFieldSchema rf, MemberAccessor member) =>
                new() { Kind = ReadStepKind.Read, WriterField = wf, ReaderField = rf, Member = member };

            internal static ReadStep Skip(RecordFieldSchema wf) =>
                new() { Kind = ReadStepKind.Skip, WriterField = wf };

            internal static ReadStep Default(MemberAccessor member, object value) =>
                new() { Kind = ReadStepKind.Default, Member = member, DefaultValue = value };
        }

        /// <summary>Finds a loaded CLR type whose full name (or simple name) matches the record schema.</summary>
        private static Type FindClrTypeForRecordSchema(RecordSchema schema)
        {
            Assembly[] preferred = { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() };
            return Find(preferred) ?? Find(AppDomain.CurrentDomain.GetAssemblies());

            Type Find(Assembly[] assemblies)
            {
                var types = assemblies.Where(a => a != null).SelectMany(SafeGetTypes).ToList();
                return types.FirstOrDefault(t => t.FullName == schema.FullName)
                       ?? types.FirstOrDefault(t => t.Name == schema.Name);
            }

            static IEnumerable<Type> SafeGetTypes(Assembly assembly)
            {
                try { return assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            }
        }
    }
}
