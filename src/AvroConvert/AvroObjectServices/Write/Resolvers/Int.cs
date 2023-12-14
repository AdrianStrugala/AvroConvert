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
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal void ResolveInt(object value, IWriter encoder)
        {
            value ??= default(int);

            if (value is not int i)
            {
                try //ResolveMap short and uint
                {
                    encoder.WriteInt(Convert.ToInt32(value));
                    return;
                }
                catch
                {
                    throw new AvroTypeMismatchException("[Int] required to write against [Int] schema but found " + value.GetType());
                }
            }

            encoder.WriteInt(i);
        }
    }
}
