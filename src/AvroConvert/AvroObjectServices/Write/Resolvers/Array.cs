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
using System.Collections.Generic;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Array
    {
        internal Encoder.WriteItem Resolve(ArraySchema schema)
        {
            var itemWriter = Resolver.ResolveWriter(schema.ItemSchema);
            return (d, e) => WriteArray(itemWriter, d, e);
        }

        private void WriteArray(Encoder.WriteItem itemWriter, object @object, IWriter encoder)
        {
            List<object> list = EnsureArrayObject(@object);

            long l = list?.Count ?? 0;
            encoder.WriteArrayStart();
            encoder.SetItemCount(l);
            WriteArrayValues(list, itemWriter, encoder, l);
            encoder.WriteArrayEnd();
        }

        private List<object> EnsureArrayObject(object value)
        {
            var enumerable = value as IEnumerable;
            List<object> list = enumerable.Cast<object>().ToList();

            return list;
        }

        private void WriteArrayValues(List<object> list, Encoder.WriteItem itemWriter, IWriter encoder, long count)
        {
            if (list == null)
            {
                itemWriter(null, encoder);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    itemWriter(list[i], encoder);
                }
            }
        }
    }
}