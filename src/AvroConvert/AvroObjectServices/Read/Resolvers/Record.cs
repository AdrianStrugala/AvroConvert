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
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using FastMember;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<int, Dictionary<string, ReadStep>> _readStepsDictionary = new();
        private readonly Dictionary<int, TypeAccessor> _accessorDictionary = new();

        protected virtual object ResolveRecord(
            RecordSchema writerSchema,
            RecordSchema readerSchema,
            IReader reader,
            Type type)
        {
            if (type != typeof(object))
            {
                object result = RuntimeHelpers.GetUninitializedObject(type);
                var typeHash = type.GetHashCode();

                TypeAccessor accessor;
                Dictionary<string, ReadStep> readSteps;

                if (!_accessorDictionary.ContainsKey(typeHash))
                {
                    accessor = TypeAccessor.Create(type, true);
                    var members = accessor.GetMembers();
                    readSteps = new Dictionary<string, ReadStep>();
                    foreach (RecordFieldSchema wf in writerSchema.Fields)
                    {
                        if (readerSchema.TryGetField(wf.Name, out var rf))
                        {
                            string name = rf.GetAliasOrDefault() ?? wf.Name;

                            var memberInfo = members.FirstOrDefault(n =>
                                n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                            if (memberInfo == null)
                            {
                                continue;
                            }

                            if (memberInfo.CanWrite)
                            {
                                accessor[result, memberInfo.Name] = GetValue(wf, rf, memberInfo, reader);
                                readSteps.Add(memberInfo.Name, new ReadStep(wf, rf, memberInfo));
                            }
                            else
                            {
                                _skipper.Skip(wf.TypeSchema, reader);
                                readSteps.Add(memberInfo.Name, new ReadStep(wf, rf, memberInfo, true));
                            }
                        }
                        else
                        {
                            _skipper.Skip(wf.TypeSchema, reader);
                            readSteps.Add(wf.Name, new ReadStep(wf, rf, null, true));
                        }
                    }

                    foreach (var (member, value) in ResolveMissingReaderFields(writerSchema, readerSchema, members))
                    {
                        accessor[result, member.Name] = value;
                        readSteps.Add(member.Name, ReadStep.Default(member, value));
                    }

                    _readStepsDictionary.Add(typeHash, readSteps);
                    _accessorDictionary.Add(typeHash, accessor);
                }
                else
                {
                    accessor = _accessorDictionary[typeHash];
                    readSteps = _readStepsDictionary[typeHash];

                    foreach (var readStep in readSteps)
                    {
                        var readStepValue = readStep.Value;
                        if (readStepValue.IsDefault)
                        {
                            accessor[result, readStep.Key] = readStepValue.DefaultValue;
                        }
                        else if (readStepValue.ShouldSkip)
                        {
                            _skipper.Skip(readStepValue.WriteFieldSchema.TypeSchema, reader);
                        }
                        else
                        {
                            accessor[result, readStep.Key] =
                                GetValue(
                                    readStepValue.WriteFieldSchema,
                                    readStepValue.ReadFieldSchema,
                                    readStepValue.MemberInfo,
                                    reader);
                        }
                    }
                }

                return result;
            }
            else
            {
                //for reading dynamics

                // Try to look up a matching CLR type.
                Assembly[] assemblies = { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() };
                Type clrType = GetClrTypeForRecordSchema(readerSchema, assemblies);
                if (clrType == null)
                {
                    clrType = GetClrTypeForRecordSchema(readerSchema, AppDomain.CurrentDomain.GetAssemblies());
                }
                
                if (clrType != null)
                {
                    return ReadForType(writerSchema, readerSchema, reader, clrType);
                }
                else
                {
                    // Fall back to Expando
                    var result = new ExpandoObject() as IDictionary<string, object>;

                    foreach (RecordFieldSchema wf in writerSchema.Fields)
                    {
                        if (readerSchema.TryGetField(wf.Name, out var rf))
                        {
                            string name = rf.Aliases.FirstOrDefault() ?? wf.Name;

                            dynamic value;
                            if (wf.TypeSchema.Type == AvroType.Array)
                            {
                                value = Resolve(wf.TypeSchema, rf.TypeSchema, reader, typeof(List<object>)) ??
                                        wf.DefaultValue;
                            }
                            else
                            {
                                value = Resolve(wf.TypeSchema, rf.TypeSchema, reader, typeof(object)) ??
                                        wf.DefaultValue;
                            }

                            result.Add(name, value);
                        }
                        else
                            _skipper.Skip(wf.TypeSchema, reader);
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
            }
        }

        private object ReadForType(RecordSchema writerSchema, RecordSchema readerSchema, IReader reader, Type clrType)
        {
            object result = RuntimeHelpers.GetUninitializedObject(clrType);
            var accessor = TypeAccessor.Create(clrType, true);
            var members = accessor.GetMembers();
            foreach (RecordFieldSchema wf in writerSchema.Fields)
            {
                if (readerSchema.TryGetField(wf.Name, out var rf))
                {
                    string name = rf.GetAliasOrDefault() ?? wf.Name;
                    var memberInfo = members
                        .FirstOrDefault(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                    if (memberInfo is { CanWrite: true })
                    {
                        accessor[result, memberInfo.Name] = GetValue(wf, rf, memberInfo, reader);
                    }
                    else
                    {
                        _skipper.Skip(wf.TypeSchema, reader);
                    }
                }
                else
                {
                    _skipper.Skip(wf.TypeSchema, reader);
                }
            }

            foreach (var (member, value) in ResolveMissingReaderFields(writerSchema, readerSchema, members))
            {
                accessor[result, member.Name] = value;
            }

            return result;
        }

        /// <summary>
        /// Avro schema resolution: reader fields absent from the writer take the reader's default;
        /// without a default, fields that can hold null resolve to null and anything else is governed by MissingFieldHandling.
        /// </summary>
        private IEnumerable<(Member Member, object Value)> ResolveMissingReaderFields(
            RecordSchema writerSchema,
            RecordSchema readerSchema,
            MemberSet members)
        {
            foreach (RecordFieldSchema rf in readerSchema.Fields)
            {
                if (writerSchema.TryGetField(rf.Name, out _))
                {
                    continue;
                }

                string name = rf.GetAliasOrDefault() ?? rf.Name;
                var member = members.FirstOrDefault(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
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

        private object GetValue(RecordFieldSchema wf,
            RecordFieldSchema rf,
            Member memberInfo,
            IReader dec)
        {
            object value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, memberInfo.Type);
            return value ?? FormatDefaultValue(wf.DefaultValue, memberInfo);
        }

        private object FormatDefaultValue(object defaultValue, Member memberInfo)
        {
            if (defaultValue == null)
            {
                return defaultValue;
            }

            var t = memberInfo.Type;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
            }

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

        private class ReadStep
        {
            internal RecordFieldSchema WriteFieldSchema { get; set; }
            internal RecordFieldSchema ReadFieldSchema { get; set; }
            internal Member MemberInfo { get; set; }
            internal bool ShouldSkip { get; set; }
            internal bool IsDefault { get; private set; }
            internal object DefaultValue { get; private set; }

            public ReadStep(RecordFieldSchema writeFieldSchema, RecordFieldSchema readFieldSchema, Member memberInfo,
                bool shouldSkip = false)
            {
                WriteFieldSchema = writeFieldSchema;
                ReadFieldSchema = readFieldSchema;
                MemberInfo = memberInfo;
                ShouldSkip = shouldSkip;
            }

            internal static ReadStep Default(Member memberInfo, object value) =>
                new(null, null, memberInfo) { IsDefault = true, DefaultValue = value };
        }

        /// <summary>
        /// Attempts to find a CLR type that matches the given record schema.
        /// The matching is based on the schema’s full name or simple name.
        /// </summary>
        private Type GetClrTypeForRecordSchema(RecordSchema schema, Assembly[] assemblies)
        {
            // Prefer using the full name (namespace + name) if available.
            string fullName = schema.FullName; 
            
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .ToList();
            
            var clrType = types.FirstOrDefault(t => t.FullName == fullName);
            if (clrType != null)
            {
                return clrType;
            }

            clrType = types.FirstOrDefault(t => t.Name == schema.Name);
            return clrType;
        }
    }
}