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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schema;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<int, Dictionary<string, MemberInfo>> cachedRecordMembers = new Dictionary<int, Dictionary<string, MemberInfo>>();

        protected virtual object ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec, Type type)
        {
            object result = FormatterServices.GetUninitializedObject(type);
            var typeHash = type.GetHashCode();

            var typeMembers = new Dictionary<string, MemberInfo>(StringComparer.InvariantCultureIgnoreCase);

            if (!cachedRecordMembers.ContainsKey(typeHash))
            {
                foreach (var propertyInfo in type.GetProperties())
                {
                    typeMembers.Add(propertyInfo.Name, propertyInfo);
                }
                foreach (var fieldInfo in type.GetFields())
                {
                    typeMembers.Add(fieldInfo.Name, fieldInfo);
                }
                cachedRecordMembers.Add(typeHash, typeMembers);
            }
            else
            {
                typeMembers = cachedRecordMembers[typeHash];
            }

            foreach (RecordField wf in writerSchema.Fields)
            {
                if (readerSchema.TryGetField(wf.Name, out var rf))
                {
                    string name = rf.Aliases.FirstOrDefault() ?? wf.Name;

                    if (!typeMembers.TryGetValue(name, out var memberInfo))
                    {
                        continue;
                    }

                    object value;

                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, fieldInfo.FieldType) ??
                                           wf.DefaultValue;
                            fieldInfo.SetValue(result, value);
                            break;

                        case PropertyInfo propertyInfo:
                            value = Resolve(wf.TypeSchema, rf.TypeSchema, dec, propertyInfo.PropertyType) ??
                                    wf.DefaultValue;
                            propertyInfo.SetValue(result, value, null);
                            break;
                    }
                }
                else
                    _skipper.Skip(wf.TypeSchema, dec);
            }
            return result;
        }
    }
}
