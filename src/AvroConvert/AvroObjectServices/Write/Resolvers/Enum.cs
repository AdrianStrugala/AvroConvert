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

using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveEnum(EnumSchema schema)
        {
            return (value, e) =>
            {
                if (!schema.TryGetSymbolValue(value.ToString(), out var symbolValue))
                {
                    throw new AvroTypeException(
                        $"[Enum] Provided value is not of the enum [{schema.Name}] members");
                }

                e.WriteEnum(symbolValue);
            };
        }
    }
}
