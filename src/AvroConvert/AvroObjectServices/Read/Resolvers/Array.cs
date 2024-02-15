#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Dictionary<int, Func<IList>> _cachedArrayInitializers = new();

        internal object ResolveArray(TypeSchema writerSchema, TypeSchema readerSchema, IReader reader, Type type, long itemsCount = 0)
        {
            if (writerSchema.Type == AvroType.Array)
            {
                writerSchema = ((ArraySchema)writerSchema).ItemSchema;
            }
            readerSchema = ((ArraySchema)readerSchema).ItemSchema;

            if (type.IsDictionary())
            {
                return ResolveDictionary((RecordSchema)writerSchema, (RecordSchema)readerSchema, reader, type);
            }

            var containingType = type.GetEnumeratedType();
            var typeHash = type.GetHashCode();

            int capacity = itemsCount == 0 ? (int)reader.ReadArrayStart() : (int)itemsCount;
            Func<IList> resultFunc;
            if (_cachedArrayInitializers.TryGetValue(typeHash, out var initializer))
            {
                resultFunc = initializer;
            }
            else
            {
                var resultType = typeof(List<>).MakeGenericType(containingType);
                ConstructorInfo constructor = resultType.GetConstructor(new[] { typeof(int) });
                ConstantExpression capacityExpr = Expression.Constant(capacity);
                NewExpression newExp = Expression.New(constructor, capacityExpr);
                resultFunc = Expression.Lambda<Func<IList>>(newExp).Compile();
                _cachedArrayInitializers.Add(typeHash, resultFunc);
            }
            IList result = resultFunc.Invoke();

            int i = 0;
            if (itemsCount == 0)
            {
                for (int n = capacity; n != 0; n = (int)reader.ReadArrayNext())
                {
                    for (int j = 0; j < n; j++, i++)
                    {
                        object item = Resolve(writerSchema, readerSchema, reader, containingType);
                        result.Add(item);
                    }
                }
            }
            else
            {
                for (int k = 0; k < itemsCount; k++)
                {
                    result.Add(Resolve(writerSchema, readerSchema, reader, containingType));
                }
            }

            if (type.IsArray)
            {
                var containingTypeArray = containingType.MakeArrayType();

                Array resultArray = (Array)Activator.CreateInstance(containingTypeArray, result.Count);
                result.CopyTo(resultArray!, 0);
                return resultArray;
            }

            if (type.IsList() || type.IsEnumerable())
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
                    dynamic key = Resolve(writerSchema.GetField("Key").TypeSchema, readerSchema.GetField("Key").TypeSchema, d, containingTypes[0]);
                    dynamic value = Resolve(writerSchema.GetField("Value").TypeSchema, readerSchema.GetField("Value").TypeSchema, d, containingTypes[1]);
                    resultDictionary.Add(key, value);
                }
            }
            return resultDictionary;
        }
    }
}
