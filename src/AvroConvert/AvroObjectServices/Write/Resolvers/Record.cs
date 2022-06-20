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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.JsonToAvro;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Extensions.JsonConvert;
using RecordSchema = SolTechnology.Avro.AvroObjectServices.Schema.RecordSchema;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Record
    {
        private readonly ConcurrentDictionary<int, Func<object, string, object>> gettersDictionary = new ConcurrentDictionary<int, Func<object, string, object>>();

        internal Encoder.WriteItem Resolve(RecordSchema recordSchema)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            int index = 0;
            foreach (RecordField field in recordSchema.Fields)
            {
                var record = new WriteStep
                {
                    WriteField = Resolver.ResolveWriter(field.TypeSchema),
                    Field = field
                };
                writeSteps[index++] = record;
            }

            void RecordResolver(object v, IWriter e)
            {
                WriteRecordFields(v, writeSteps, e);
            }


            return RecordResolver;
        }

        private void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder)
        {
            if (recordObj is ExpandoObject expando)
            {
                HandleExpando(expando, writers, encoder);
                return;
            }

            var type = recordObj.GetType();
            var typeHash = type.GetHashCode();

            Func<object, string, object> getters;
            if (gettersDictionary.ContainsKey(typeHash))
            {
                getters = gettersDictionary[typeHash];
            }
            else
            {
                getters = GenerateGetValue(type);
                gettersDictionary.AddOrUpdate(typeHash, getters, (key, existingVal) => existingVal);
            }

            foreach (var writer in writers)
            {
                string name = writer.Field.Aliases.FirstOrDefault() ?? writer.Field.Name;

                var value = getters.Invoke(recordObj, name);
                if (value == null)
                {
                    value = type.GetField(name)?.GetValue(recordObj);
                }

                writer.WriteField(value, encoder);
            }
        }

        private void HandleExpando(ExpandoObject expando, WriteStep[] writers, IWriter encoder)
        {
            foreach (var writer in writers)
            {
                string name = writer.Field.Aliases.FirstOrDefault() ?? writer.Field.Name;

                var value = expando.ToList().FirstOrDefault(x => x.Key == name).Value;

                switch (value)
                {
                    case JObject jObject:
                        value = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(jObject.ToString());
                        break;
                    case JArray jArray:
                        value = jArray.ToObject<object[]>();
                        value = ((object[])value).Select(ApplySwithcxD);
                        break;
                    case JValue jValue:
                        value = jValue.Value;
                        break;
                }

                writer.WriteField(value, encoder);
            }
        }

        private object ApplySwithcxD(object value)
        {
            switch (value)
            {
                case JObject jObject:
                    value = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(jObject.ToString());
                    break;
                case JArray jArray:
                    value = jArray.ToObject<object[]>();
                    if (jArray.FirstOrDefault() is JObject)
                    {

                    }
                    break;
                case JValue jValue:
                    value = jValue.Value;
                    break;
            }

            return value;
        }

        private static Func<object, string, object> GenerateGetValue(Type type)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var memberName = Expression.Parameter(typeof(string), "memberName");
            var nameHash = Expression.Variable(typeof(int), "nameHash");
            var calHash = Expression.Assign(nameHash,
                Expression.Call(memberName, typeof(object).GetMethod("GetHashCode")));
            var cases = new List<SwitchCase>();
            foreach (var propertyInfo in type.GetProperties())
            {
                var property = Expression.Property(Expression.Convert(instance, type), propertyInfo.Name);
                var propertyHash = Expression.Constant(propertyInfo.Name.GetHashCode(), typeof(int));

                cases.Add(Expression.SwitchCase(Expression.Convert(property, typeof(object)), propertyHash));
            }

            var switchEx = Expression.Switch(nameHash, Expression.Constant(null), cases.ToArray());
            var methodBody = Expression.Block(typeof(object), new[] { nameHash }, calHash, switchEx);

            return Expression.Lambda<Func<object, string, object>>(methodBody, instance, memberName).Compile();

        }
    }
}
