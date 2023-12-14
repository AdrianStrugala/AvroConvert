#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal static class EnumParser
    {
        private static ConcurrentDictionary<Type, Dictionary<string, string>> memberValuesToMember = new ConcurrentDictionary<Type, Dictionary<string, string>>();

        public static object Parse(Type enumType, string value)
        {
            var valuesCache = memberValuesToMember.GetOrAdd(enumType, type =>
            {
                return Enum.GetNames(type)
                    .Select(x => new
                    {
                        Member = x,
                        Value = GetEnumMemberValue(type, x)
                    })
                    .ToDictionary(x => x.Value, x => x.Member);
            });

            return Enum.Parse(enumType, valuesCache[value]);
        }

        public static string GetEnumMemberValue(Type runtimeType, string member)
        {
            var attribute = runtimeType.GetField(member)?.GetCustomAttribute<EnumMemberAttribute>();

            return attribute != null && !string.IsNullOrEmpty(attribute.Value)
                ? attribute.Value
                : member;
        }
    }
}
