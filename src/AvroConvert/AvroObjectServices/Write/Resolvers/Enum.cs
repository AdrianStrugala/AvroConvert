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

using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Enum
    {
        internal Encoder.WriteItem Resolve(EnumSchema schema)
        {
            return (value, e) =>
            {
                if (!schema.Symbols.Contains(value.ToString()))
                {
                    throw new AvroTypeException(
                        $"[Enum] Provided value is not of the enum [{schema.Name}] members");
                }

                e.WriteEnum(schema.GetValueBySymbol(value.ToString()));
            };
        }
    }
}
