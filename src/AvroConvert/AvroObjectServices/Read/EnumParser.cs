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
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal static class EnumParser
    {
        private static ConcurrentDictionary<Type, EnumCache> memberValueCaches = new ConcurrentDictionary<Type, EnumCache>();

        public static object Parse(Type enumType, string value, IAvroNamingPolicy namingPolicy)
        {
            var cache = GetEnumMemberCache(enumType, namingPolicy);

            return Enum.Parse(enumType, cache.MembersToNames[value]);
        }

        public static string GetEnumName(Type enumType, object value, IAvroNamingPolicy namingPolicy)
        {
            var cache = GetEnumMemberCache(enumType, namingPolicy);

            var valueAsString = value.ToString();

            return cache.NamesToMembers.TryGetValue(valueAsString, out var actualValue)
                ? actualValue
                : valueAsString;
        }

        private static EnumCache GetEnumMemberCache(Type runtimeType, IAvroNamingPolicy namingPolicy)
        {
            return memberValueCaches.GetOrAdd(runtimeType, type =>
            {
                var names = Enum.GetNames(type);
                var members = names.Select(x => GetEnumMemberValue(type, x, namingPolicy)).ToArray();

                var cache = new EnumCache();

                for (var i = 0; i < names.Length; i++)
                {
                    cache.NamesToMembers[names[i]] = members[i];
                    cache.MembersToNames[members[i]] = names[i];
                }
                
                return cache;
            });
        }

        private static string GetEnumMemberValue(Type runtimeType, string member, IAvroNamingPolicy namingPolicy)
        {
            var field = runtimeType.GetField(member);

            if (field != null && namingPolicy != null)
            {
                var naming = namingPolicy.GetMemberName(field);

                if (!string.IsNullOrEmpty(naming))
                {
                    return naming;
                }
            }

            var attribute = field?.GetCustomAttribute<EnumMemberAttribute>();

            return attribute != null && !string.IsNullOrEmpty(attribute.Value)
                ? attribute.Value
                : member;
        }

        private class EnumCache
        {
            public Dictionary<string, string> MembersToNames = new Dictionary<string, string>();

            public Dictionary<string, string> NamesToMembers = new Dictionary<string, string>();
        }
    }
}
