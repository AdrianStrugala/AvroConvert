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

using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class TimestampMicroseconds
    {
        internal void Resolve(TimestampMicrosecondsSchema schema, object logicalValue, IWriter writer)
        {
            if (schema.BaseTypeSchema is not LongSchema)
            {
                throw new AvroTypeMismatchException(
                    $"[TimestampMicroseconds] required to write against [long] of [Long] schema but found [{schema.BaseTypeSchema}]");
            }

            DateTime date;
            switch (logicalValue)
            {
                case DateTimeOffset x:
                    date = x.DateTime;
                    break;

                case DateOnly x:
                    date = x.ToDateTime(new TimeOnly());
                    break;

                default:
                    date = (DateTime)logicalValue;
                    break;
            }

            writer.WriteLong((long)(date - DateTimeExtensions.UnixEpochDateTime).TotalMilliseconds * 1000);
        }
    }
}
