#region license
/**Copyright (c) 2020 Adrian Struga³a
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
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveUnion(UnionSchema writerSchema, TypeSchema readerSchema, IReader d, Type type)
        {
            int index = d.ReadUnionIndex();
            TypeSchema ws = writerSchema.Schemas[index];

            if (readerSchema is UnionSchema unionSchema)
                readerSchema = FindBranch(unionSchema, ws);
            else
            if (!readerSchema.CanRead(ws))
                throw new AvroException("Schema mismatch. Reader: " + _readerSchema + ", writer: " + _writerSchema);

            return Resolve(ws, readerSchema, d, type);
        }

        protected static TypeSchema FindBranch(UnionSchema us, TypeSchema writerSchema)
        {
            foreach (var readSchema in us.Schemas)
            {
                if (readSchema.CanRead(writerSchema))
                {
                    return readSchema;
                }
            }

            throw new AvroException("No matching schema for " + writerSchema + " in " + us);
        }
    }
}