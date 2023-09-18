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
using System.Runtime.Serialization;
using FastMember;
using SolTechnology.Avro.AvroObjectServices.Schemas;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<int, Dictionary<string, Func<object>>> _readStepsDictionary = new Dictionary<int, Dictionary<string, Func<object>>>();
        private readonly Dictionary<int, TypeAccessor> _accessorDictionary = new Dictionary<int, TypeAccessor>();

        protected virtual object ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec,
            Type type)
        {
            if (type != typeof(object))
            {
                object result = FormatterServices.GetUninitializedObject(type);
                var typeHash = type.GetHashCode();

                TypeAccessor accessor;
                Dictionary<string, Func<object>> readSteps;

                if (!_accessorDictionary.ContainsKey(typeHash))
                {
                    accessor = TypeAccessor.Create(type, true);
                    readSteps = new Dictionary<string, Func<object>>();

                    foreach (RecordFieldSchema wf in writerSchema.Fields)
                    {
                        if (readerSchema.TryGetField(wf.Name, out var rf))
                        {
                            string name = rf.Aliases.FirstOrDefault() ?? wf.Name;

                            var members = accessor.GetMembers();
                            var memberInfo = members.FirstOrDefault(n =>
                                n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                            if (memberInfo == null)
                            {
                                continue;
                            }

                            Func<object> func = () =>
                            {
                                object value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, memberInfo.Type);
                                return value ?? FormatDefaultValue(wf.DefaultValue, memberInfo);
                            };

                            accessor[result, memberInfo.Name] = func.Invoke();

                            readSteps.Add(memberInfo.Name, func);

                        }
                        else
                            _skipper.Skip(wf.TypeSchema, dec);
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
                        accessor[result, readStep.Key] = readStep.Value.Invoke();
                    }
                }

                return result;
            }
            else
            {
                //for reading dynamics

                var result = new ExpandoObject() as IDictionary<string, object>;

                foreach (RecordFieldSchema wf in writerSchema.Fields)
                {
                    if (readerSchema.TryGetField(wf.Name, out var rf))
                    {
                        string name = rf.Aliases.FirstOrDefault() ?? wf.Name;

                        dynamic value;
                        if (wf.TypeSchema.Type == AvroType.Array)
                        {
                            value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, typeof(List<object>)) ?? wf.DefaultValue;
                        }
                        else
                        {
                            value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, typeof(object)) ?? wf.DefaultValue;
                        }

                        result.Add(name, value);
                    }
                    else
                        _skipper.Skip(wf.TypeSchema, dec);
                }
                return result;
            }
        }

        private static object FormatDefaultValue(object defaultValue, Member memberInfo)
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
                return Enum.Parse(t, (string)defaultValue);
            }

            //TODO: Map and Record default values are represented as Dictionary<string,object>
            //https://avro.apache.org/docs/1.4.0/spec.html
            //It might be not supported at the moment

            return Convert.ChangeType(defaultValue, t);
        }
    }
}
