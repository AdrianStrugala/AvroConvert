#region license
/**Copyright (c) 2021 Adrian Strugała
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
using SolTechnology.Avro.BuildSchema.SchemaModels;
using SolTechnology.Avro.Exceptions;

namespace SolTechnology.Avro.Write.Resolvers
{
    internal class TimestampMilliseconds
    {
        internal Encoder.WriteItem Resolve(TimestampMillisecondsSchema schema)
        {
            return (value, encoder) =>
            {
                if (!(schema.BaseTypeSchema is LongSchema))
                {
                    throw new AvroTypeMismatchException($"[TimestampMilliseconds] required to write against [long] of [Long] schema but found [{schema.BaseTypeSchema}]");
                }

                var bytesValue = (long)schema.ConvertToBaseValue(value, schema);
                encoder.WriteLong(bytesValue);
            };
        }
    }
}
