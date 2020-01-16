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

using System.Collections.Generic;

namespace SolTechnology.Avro.Helpers
{
    public class Metadata
    {
        private readonly Dictionary<string, byte[]> _value;

        public Metadata()
        {
            _value = new Dictionary<string, byte[]>();
        }

        public void Add(string key, string value)
        {
            _value.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public int GetSize()
        {
            return _value.Count;
        }

        public Dictionary<string, byte[]> GetValue()
        {
            return _value;
        }
    }
}
