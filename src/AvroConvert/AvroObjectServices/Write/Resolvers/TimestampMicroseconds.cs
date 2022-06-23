#region license
/**Copyright (c) 2021 Adrian Strugala
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

using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class TimestampMicroseconds
    {
        internal Encoder.WriteItem Resolve(TimestampMicrosecondsSchema schema)
        {
            return (value, encoder) =>
            {
                if (!(schema.BaseTypeSchema is LongSchema))
                {
                    throw new AvroTypeMismatchException($"[TimestampMicroseconds] required to write against [long] of [Long] schema but found [{schema.BaseTypeSchema}]");
                }

                var bytesValue = (long)schema.ConvertToBaseValue(value, schema);
                encoder.WriteLong(bytesValue);
            };
        }
    }
}
