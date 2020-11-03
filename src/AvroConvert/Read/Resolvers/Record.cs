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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> cachedRecordProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();




        protected virtual object ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec, Type type)
        {
            object result = FormatterServices.GetUninitializedObject(type);
            // object result = Expression.Lambda<Func<object>>(Expression.New(type)).Compile()();



            // // var type = typeof(string);
            // var body = Expression.New(type);
            // // var body = Expression.Constant(true);
            // var parameter = Expression.Parameter(type);
            // var delegateType = typeof(Func<>).MakeGenericType(type);
            // dynamic lambda = Expression.Lambda(delegateType, body, parameter, parameter, parameter).Compile();
            // var result = lambda();

            // var body = Expression.New(type);
            // var delegateType = typeof(Func<>).MakeGenericType(type);
            // var lambda = Expression.Lambda(delegateType, body).Compile();
            // var result = lambda.DynamicInvoke();



            if (cachedRecordProperties.ContainsKey(type))
            {
                var cachedProperties = cachedRecordProperties[type];

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
                            FieldInfo fieldInfo = type.GetField(name,
                                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (fieldInfo != null)
                            {
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
                        }
                    }
                    else
                        _skipper.Skip(wf.Schema, dec);
                }
                cachedRecordProperties.Add(type, cachedProperties);
            }

            return result;
        }
    }
}
