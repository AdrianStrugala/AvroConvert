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
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.Skip;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveEnum(EnumSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            int position = d.ReadEnum();
            string value = writerSchema.Symbols[position];
            return Enum.Parse(type, value);
        }
    }
}