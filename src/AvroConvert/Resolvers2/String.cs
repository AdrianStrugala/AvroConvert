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
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro.Resolvers2
{
      internal partial class Resolver<T>
    {
        private Dictionary<Type, Func<object>> @switch(string value) => new Dictionary<Type, Func<object>>
        {
            {typeof(string), () => value},
            {typeof(decimal), () => decimal.Parse(value)},
            {typeof(DateTimeOffset), () => DateTimeOffset.Parse(value)},
            {typeof(DateTimeOffset?), () => DateTimeOffset.Parse(value)},
            {typeof(Uri), () => new Uri(value)},
        };

        internal object ResolveString<T>(IReader reader)
        {
            var value = reader.ReadString();

            @switch(value).TryGetValue(typeof(T), out Func<object> resultFunc);

            if (resultFunc == null)
            {
                return value;
            }
            var result = resultFunc.Invoke();
            return result;
        }
    }
}
