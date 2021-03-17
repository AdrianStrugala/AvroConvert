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
using System.Collections.Generic;

namespace SolTechnology.Avro.Read
{
    internal partial class Resolver
    {
        private Dictionary<Type, Func<object>> @switch(string value) => new Dictionary<Type, Func<object>>
        {
            {typeof(Uri), () => new Uri(value)},
        };

        internal object ResolveString(Type type, IReader reader)
        {
            var value = reader.ReadString();

            if (type == typeof(string))
            {
                return value;
            }

            if (@switch(value).TryGetValue(type, out Func<object> resultFunc))
            {
                return resultFunc.Invoke();
            }

            return value;
        }
    }
}
