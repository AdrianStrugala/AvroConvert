#region license
/**Copyright (c) 2022 Adrian Strugala
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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Write.Resolvers;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        private static readonly ConcurrentDictionary<Type, Lazy<MemberGetters>> gettersDictionary = new();

        private static readonly Func<Type, Lazy<MemberGetters>> getterFactory =
            type => new Lazy<MemberGetters>(() => MemberGetters.Build(type), LazyThreadSafetyMode.ExecutionAndPublication);

        internal Encoder.WriteItem ResolveRecord(RecordSchema recordSchema)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            int index = 0;
            foreach (RecordFieldSchema field in recordSchema.Fields)
            {
                var record = new WriteStep
                {
                    WriteField = ResolveWriter(field.TypeSchema),
                    FiledName = field.Aliases.FirstOrDefault() ?? field.Name,
                };
                writeSteps[index++] = record;
            }

            void RecordResolver(object v, IWriter e)
            {
                WriteRecordFields(v, writeSteps, e);
            }

            return RecordResolver;
        }

        private static void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder)
        {
            if (recordObj is null)
            {
                encoder.WriteNull();
                return;
            }

            if (recordObj is ExpandoObject expando)
            {
                HandleExpando(writers, encoder, expando);
                return;
            }

            var type = recordObj.GetType();

            foreach (var writer in writers)
            {
                if (!ReferenceEquals(writer.CachedType, type))
                {
                    writer.CachedType = type;
                    writer.CachedGetter = gettersDictionary.GetOrAdd(type, getterFactory).Value.Get(writer.FiledName);
                }

                var value = writer.CachedGetter?.Invoke(recordObj);
                writer.WriteField(value, encoder);
            }
        }

        private static void HandleExpando(WriteStep[] writers, IWriter encoder, ExpandoObject expando)
        {
            var expandoDictionary = expando.ToDictionary(x => x.Key, y => y.Value, StringComparer.InvariantCultureIgnoreCase);

            foreach (var writer in writers)
            {
                expandoDictionary.TryGetValue(writer.FiledName, out var value);
                writer.WriteField(value, encoder);
            }
        }

        /// <summary>Compiled getters for every readable member of a type, looked up exact-case first, then case-insensitively.</summary>
        private sealed class MemberGetters
        {
            private readonly Dictionary<string, Func<object, object>> _exact = new(StringComparer.Ordinal);
            private readonly Dictionary<string, Func<object, object>> _ignoreCase = new(StringComparer.OrdinalIgnoreCase);

            internal Func<object, object> Get(string name)
            {
                if (_exact.TryGetValue(name, out var getter) || _ignoreCase.TryGetValue(name, out getter))
                {
                    return getter;
                }

                return null;
            }

            internal static MemberGetters Build(Type type)
            {
                var result = new MemberGetters();
                var instance = Expression.Parameter(typeof(object), "instance");
                var typed = Expression.Convert(instance, type);

                const BindingFlags publicMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
                const BindingFlags privateMembers = BindingFlags.Instance | BindingFlags.NonPublic;

                foreach (var property in type.GetProperties(publicMembers))
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        result.Add(property.Name, Compile(instance, Expression.Property(typed, property)));
                    }
                }

                foreach (var field in type.GetFields(publicMembers))
                {
                    result.Add(field.Name, Compile(instance, Expression.Field(typed, field)));
                }

                // Non-public members take part in the schema only when marked with [DataMember].
                foreach (var property in type.GetProperties(privateMembers))
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0 && property.IsDefined(typeof(DataMemberAttribute), true))
                    {
                        result.Add(property.Name, Compile(instance, Expression.Property(typed, property)));
                    }
                }

                foreach (var field in type.GetFields(privateMembers))
                {
                    if (field.IsDefined(typeof(DataMemberAttribute), true))
                    {
                        result.Add(field.Name, Compile(instance, Expression.Field(typed, field)));
                    }
                }

                return result;
            }

            private void Add(string name, Func<object, object> getter)
            {
                _exact.TryAdd(name, getter);
                _ignoreCase.TryAdd(name, getter);
            }

            private static Func<object, object> Compile(ParameterExpression instance, MemberExpression member) =>
                Expression.Lambda<Func<object, object>>(Expression.Convert(member, typeof(object)), instance).Compile();
        }
    }
}