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
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Exceptions;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Schema;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write.Resolvers
{
    internal class Fixed
    {
        internal Encoder.WriteItem Resolve(FixedSchema es)
        {
            return (value, encoder) =>
            {
                if (value is Guid guid)
                {
                    value = new Models.Fixed(es, guid.ToByteArray());
                }

                else if (!(value is Models.Fixed) || !((Models.Fixed)value).Schema.Equals(es))
                {
                    throw new AvroTypeMismatchException("[GenericFixed] required to write against [Fixed] schema but found " + value.GetType());
                }

                Models.Fixed ba = (Models.Fixed)value;
                encoder.WriteFixed(ba.Value);
            };
        }
    }
}
