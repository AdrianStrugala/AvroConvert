#region license
/**Copyright (c) 2020 Adrian Strugała
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
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> cachedRecordProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private readonly Dictionary<Type, Dictionary<string, FieldInfo>> cachedRecordFields = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        protected virtual object ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec, Type type)
        {
            object result = FormatterServices.GetUninitializedObject(type);

            if (cachedRecordProperties.ContainsKey(type))
            {
                var cachedProperties = cachedRecordProperties[type];
                var cachedFields = cachedRecordFields[type];

                foreach (Field wf in writerSchema)
                {
                    if (readerSchema.Contains(wf.Name))
                    {
                        Field rf = readerSchema.GetField(wf.Name);
                        string name = rf.aliases?[0] ?? wf.Name;

                        if (cachedProperties.ContainsKey(name))
                        {
                            var propertyInfo = cachedProperties[name];

                            object value = Resolve(wf.Schema, rf.Schema, dec, propertyInfo.PropertyType) ??
                                           wf.DefaultValue?.ToObject(typeof(object));
                            propertyInfo.SetValue(result, value, null);
                        }
                        else
                        {
                            if (cachedProperties.ContainsKey(name))
                            {
                                var fieldInfo = cachedFields[name];
                                object value = Resolve(wf.Schema, rf.Schema, dec, fieldInfo.FieldType) ??
                                                   wf.DefaultValue?.ToObject(typeof(object));
                                fieldInfo.SetValue(result, value);
                            }
                        }
                    }
                    else
                        _skipper.Skip(wf.Schema, dec);
                }
            }

            else
            {
                var cachedProperties = new Dictionary<string, PropertyInfo>();
                var cachedFields = new Dictionary<string, FieldInfo>();

                foreach (Field wf in writerSchema)
                {
                    if (readerSchema.Contains(wf.Name))
                    {
                        Field rf = readerSchema.GetField(wf.Name);
                        string name = rf.aliases?[0] ?? wf.Name;

                        PropertyInfo propertyInfo = type.GetProperty(name,
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (propertyInfo != null)
                        {
                            object value = Resolve(wf.Schema, rf.Schema, dec, propertyInfo.PropertyType) ??
                                           wf.DefaultValue?.ToObject(typeof(object));
                            propertyInfo.SetValue(result, value, null);
                            cachedProperties.Add(name, propertyInfo);
                        }

                        FieldInfo fieldInfo = type.GetField(name,
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            object value = Resolve(wf.Schema, rf.Schema, dec, fieldInfo.FieldType) ??
                                           wf.DefaultValue?.ToObject(typeof(object));
                            fieldInfo.SetValue(result, value);
                            cachedFields.Add(name, fieldInfo);
                        }
                    }
                    else
                        _skipper.Skip(wf.Schema, dec);
                }
                cachedRecordProperties.Add(type, cachedProperties);
                cachedRecordFields.Add(type, cachedFields);
            }

            return result;
        }
    }
}
