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
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveArray(ArraySchema schema)
        {
            var itemWriter = ResolveWriter(schema.ItemSchema);
            return (d, e) => WriteArray(itemWriter, d, e);
        }

        private void WriteArray(Encoder.WriteItem itemWriter, object @object, IWriter encoder)
        {
            encoder.WriteArrayStart();

            switch (@object)
            {
                case null:
                    break;

                case IList list:
                    encoder.WriteItemCount(list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        itemWriter(list[i], encoder);
                    }
                    break;

                case ICollection collection:
                    encoder.WriteItemCount(collection.Count);
                    foreach (var item in collection)
                    {
                        itemWriter(item, encoder);
                    }
                    break;

                case IEnumerable enumerable:
                    // Unknown count: materialise once (Avro needs the block size before its items).
                    var buffered = enumerable.Cast<object>().ToList();
                    encoder.WriteItemCount(buffered.Count);
                    foreach (var item in buffered)
                    {
                        itemWriter(item, encoder);
                    }
                    break;

                default:
                    throw new AvroTypeMismatchException($"Expected a collection for an array schema, but found [{@object.GetType()}]");
            }

            encoder.WriteArrayEnd();
        }
    }
}