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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Record
    {
        // private readonly ConcurrentDictionary<int, Func<object, string, object>> gettersDictionary = new();
        //
        // internal Encoder.WriteItem Resolve(RecordSchema recordSchema)
        // {
        //     WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];
        //
        //     int index = 0;
        //     foreach (RecordFieldSchema field in recordSchema.Fields)
        //     {
        //         writeSteps[index++] = new WriteStep
        //         {
        //             WriteField = Resolver.ResolveWriter(field.TypeSchema),
        //             Field = field
        //         };
        //     }
        //
        //     void RecordResolver(object v, IWriter e)
        //     {
        //         WriteRecordFields(v, writeSteps, e, recordSchema.Id);
        //     }
        //     return RecordResolver;
        // }
        //
        // private void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder, int id)
        // {
        //     var type = recordObj.GetType();
        //
        //     Func<object, string, object> getters;
        //     if (gettersDictionary.ContainsKey(id))
        //     {
        //         getters = gettersDictionary[id];
        //     }
        //     else
        //     {
        //         getters = GenerateGetValue(type);
        //         gettersDictionary.AddOrUpdate(id, getters, (key, existingVal) => existingVal);
        //     }
        //
        //     foreach (var writer in writers)
        //     {
        //         string name = writer.Field.Aliases.FirstOrDefault() ?? writer.Field.Name;
        //         var value = getters.Invoke(recordObj, name);
        //         if (value == null)
        //         {
        //             value = type.GetField(name)?.GetValue(recordObj);
        //         }
        //
        //         writer.WriteField(value, encoder);
        //     }
        // }
        //
        // private static Func<object, string, object> GenerateGetValue(Type type)
        // {
        //     var instance = Expression.Parameter(typeof(object), "instance");
        //     var memberName = Expression.Parameter(typeof(string), "memberName");
        //     var nameHash = Expression.Variable(typeof(int), "nameHash");
        //     var calHash = Expression.Assign(nameHash,
        //         Expression.Call(memberName, typeof(object).GetMethod("GetHashCode")));
        //     var cases = new List<SwitchCase>();
        //     foreach (var propertyInfo in type.GetProperties())
        //     {
        //         var property = Expression.Property(Expression.Convert(instance, type), propertyInfo.Name);
        //         var propertyHash = Expression.Constant(propertyInfo.Name.GetHashCode(), typeof(int));
        //
        //         cases.Add(Expression.SwitchCase(Expression.Convert(property, typeof(object)), propertyHash));
        //     }
        //
        //     var switchEx = Expression.Switch(nameHash, Expression.Constant(null), cases.ToArray());
        //     var methodBody = Expression.Block(typeof(object), new[] { nameHash }, calHash, switchEx);
        //
        //     return Expression.Lambda<Func<object, string, object>>(methodBody, instance, memberName).Compile();
        //
        // }

        private static Func<object, object> GenerateGetterLambda(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                return null;
            }
            // Define our instance parameter, which will be the input of the Func
            var objParameterExpr = Expression.Parameter(typeof(object), "instance");
            // 1. Cast the instance to the correct type
            var instanceExpr = Expression.Convert(objParameterExpr, propertyInfo!.ReflectedType);
            // 2. Call the getter and retrieve the value of the property
            var propertyExpr = Expression.Property(instanceExpr, propertyInfo);
            // 3. Convert the property's value to object
            var propertyObjExpr = Expression.Convert(propertyExpr, typeof(object));
            // Create a lambda expression of the latest call & compile it
            return Expression.Lambda<Func<object, object>>(propertyObjExpr, objParameterExpr).Compile();
        }

        private readonly ConcurrentDictionary<int, WriteStep[]> writeStepDictionary = new();

        public void Resolve2(RecordSchema recordSchema, object recordObj, IWriter encoder)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            if (writeStepDictionary.ContainsKey(recordSchema.Id))
            {
                writeSteps = writeStepDictionary[recordSchema.Id];
            }
            else
            {
                var type = recordSchema.RuntimeType;
                int index = 0;
                foreach (RecordFieldSchema field in recordSchema.Fields)
                {
                    string name = field.Aliases.FirstOrDefault() ?? field.Name;

                    var writeStep = new WriteStep
                    {
                        WriteField = Resolver.ResolveWriter(field.TypeSchema),
                        FiledName = name,
                        Getter = GenerateGetterLambda(type.GetProperty(name))
                    };
                    writeSteps[index++] = writeStep;
                }
                writeStepDictionary.AddOrUpdate(recordSchema.Id, writeSteps, (key, existingVal) => existingVal);
            }

            WriteRecordFields2(recordObj, writeSteps, encoder);
        }

        private void WriteRecordFields2(object recordObj, WriteStep[] writers, IWriter encoder)
        {
            var type = recordObj.GetType();

            object value = null;
            foreach (var writer in writers)
            {
                value = writer.Getter?.Invoke(recordObj);
                if (value == null)
                {
                    value = type.GetField(writer.FiledName)?.GetValue(recordObj);
                }
                writer.WriteField(value, encoder);
            }
        }
    }
}
