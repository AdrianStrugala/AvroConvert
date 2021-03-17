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

using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    internal class Decimal
    {
        internal Encoder.WriteItem Resolve(DecimalSchema schema)
        {
            return (value, encoder) =>
            {
                if (!(schema.BaseTypeSchema is BytesSchema))
                {
                    throw new AvroTypeMismatchException($"[Decimal] required to write against [decimal] of [Bytes] schema but found [{schema.BaseTypeSchema}]");
                }

                var bytesValue = (byte[])schema.ConvertToBaseValue(value, schema);
                encoder.WriteBytes(bytesValue);
            };
        }
    }
}
