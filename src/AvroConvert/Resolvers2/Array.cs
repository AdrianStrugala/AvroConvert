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
using SolTechnology.Avro.Extensions;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Resolvers2
{
    internal partial class Resolver<T>
    {
        private readonly Dictionary<Type, Func<IList>> cachedArrayInitializers = new Dictionary<Type, Func<IList>>();

        internal object ResolveArray<T>(Schema.Schema writerSchema, Schema.Schema readerSchema, IReader d, long itemsCount = 0)
        {
            var type = typeof(T);
            if (writerSchema.Tag == Schema.Schema.Type.Array)
            {
                writerSchema = ((ArraySchema)writerSchema).ItemSchema;
            }
            readerSchema = ((ArraySchema)readerSchema).ItemSchema;

            if (type.IsDictionary())
            {
                return ResolveDictionary((RecordSchema)writerSchema, (RecordSchema)readerSchema, d, type);
            }


            var containingType = type.GetEnumeratedType();
            var containingTypeArray = containingType.MakeArrayType();
            var resultType = typeof(List<>).MakeGenericType(containingType);

            Func<IList> resultFunc;
            if (cachedArrayInitializers.ContainsKey(resultType))
            {
                resultFunc = cachedArrayInitializers[resultType];
            }
            else
            {
                resultFunc = Expression.Lambda<Func<IList>>(Expression.New(resultType)).Compile();
                cachedArrayInitializers.Add(resultType, resultFunc);
            }
            IList result = resultFunc.Invoke();


            int i = 0;
            if (itemsCount == 0)
            {
                for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
                {
                    for (int j = 0; j < n; j++, i++)
                    {
                        dynamic y = Resolve(writerSchema, readerSchema, d, containingType);
                        result.Add(y);
                    }
                }
            }
            else
            {
                for (int k = 0; k < itemsCount; k++)
                {
                    result.Add(Resolve(writerSchema, readerSchema, d, containingType));
                }
            }

            if (type.IsArray)
            {
                dynamic resultArray = Activator.CreateInstance(containingTypeArray, new object[] { result.Count });
                result.CopyTo(resultArray, 0);
                return resultArray;
            }

            if (type.IsList())
            {
                return result;
            }


            var hashSetType = typeof(HashSet<>).MakeGenericType(containingType);
            if (type == hashSetType)
            {
                dynamic resultHashSet = Activator.CreateInstance(hashSetType);
                foreach (dynamic item in result)
                {
                    resultHashSet.Add(item);
                }

                return resultHashSet;
            }


            var reflectionResult = type.GetField("Empty")?.GetValue(null);
            var addMethod = type.GetMethod("Add");
            foreach (dynamic item in result)
            {
                reflectionResult = addMethod.Invoke(reflectionResult, new[] { item });
            }

            return reflectionResult;
        }

        protected object ResolveDictionary(RecordSchema writerSchema, RecordSchema readerSchema, IReader d, Type type)
        {
            var containingTypes = type.GetGenericArguments();
            dynamic resultDictionary = Activator.CreateInstance(type);

            for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
            {
                for (int j = 0; j < n; j++)
                {
                    dynamic key = Resolve(writerSchema.GetField("Key").Schema, readerSchema.GetField("Key").Schema, d, containingTypes[0]);
                    dynamic value = Resolve(writerSchema.GetField("Value").Schema, readerSchema.GetField("Value").Schema, d, containingTypes[1]);
                    resultDictionary.Add(key, value);
                }
            }
            return resultDictionary;
        }
    }
}
