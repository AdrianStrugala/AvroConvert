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

using System.Collections;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    internal class Map
    {
        internal Encoder.WriteItem Resolve(MapSchema mapSchema)
        {
            var itemWriter = Resolver.ResolveWriter(mapSchema.ValueSchema);
            return (v, e) => WriteMap(itemWriter, v, e);
        }

        private void WriteMap(Encoder.WriteItem itemWriter, object value, IWriter encoder)
        {
            EnsureMapObject(value);
            encoder.WriteMapStart();
            encoder.SetItemCount(GetMapSize(value));
            WriteMapValues(value, itemWriter, encoder);
            encoder.WriteMapEnd();
        }

        private void EnsureMapObject(object value)
        {
            if (value == null || !(value is IDictionary)) if (value != null) throw new AvroException("[IDictionary] required to write against [Map] schema but found " + value.GetType());
        }

        private long GetMapSize(object value)
        {
            return ((IDictionary)value).Count;
        }

        private void WriteMapValues(object map, Encoder.WriteItem valueWriter, IWriter encoder)
        {
            foreach (DictionaryEntry entry in ((IDictionary)map))
            {
                encoder.StartItem();
                encoder.WriteString(entry.Key.ToString());
                valueWriter(entry.Value, encoder);
            }
        }
    }
}