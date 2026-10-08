#region license
/**Copyright (c) 2020 Adrian Strugala
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
using SolTechnology.Avro.AvroObjectServices.Read.Typed;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    /// <summary>
    /// Entry point for decoding values of a (writer schema, reader schema) pair into T. All work is done by compiled
    /// read plans (<see cref="ReadCompiler"/>); this type only carries the schemas/options and wraps failures.
    /// </summary>
    internal sealed class Resolver
    {
        private readonly TypeSchema _readerSchema;
        private readonly TypeSchema _writerSchema;
        private readonly AvroConvertOptions _options;

        internal Resolver(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options = null)
        {
            _readerSchema = readerSchema;
            _writerSchema = writerSchema;
            _options = options;
        }

        internal ReadPlan<T> GetPlan<T>() => ReadPlanCache.Get<T>(_writerSchema, _readerSchema, _options);

        internal T Resolve<T>(IReader reader, long itemsCount = 0)
        {
            try
            {
                var plan = GetPlan<T>();

                if (itemsCount > 1)
                {
                    if (plan.Many == null)
                    {
                        throw new AvroTypeMismatchException($"The data contains {itemsCount} entries but [{typeof(T)}] is not a collection type");
                    }

                    return plan.Many(reader, itemsCount);
                }

                return plan.One(reader);
            }
            catch (Exception e)
            {
                throw WrapFailure<T>(e);
            }
        }

        internal AvroTypeMismatchException WrapFailure<T>(Exception e) =>
            new($"Unable to deserialize [{_writerSchema.Name}] of schema [{_writerSchema.Type}] to the target type [{typeof(T)}]. Inner exception:", e);
    }
}
