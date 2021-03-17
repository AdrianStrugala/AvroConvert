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
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
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
            var array = EnsureArrayObject(@object);
            long l = GetArrayLength(array);
            encoder.WriteArrayStart();
            encoder.SetItemCount(l);
            WriteArrayValues(array, itemWriter, encoder);
            encoder.WriteArrayEnd();
        }

        private System.Array EnsureArrayObject(object value)
        {
            var enumerable = value as IEnumerable;
            var list = enumerable.Cast<object>().ToList();

            int length = list.Count;
            dynamic[] result = new dynamic[length];
            list.CopyTo(result, 0);

            return result;
        }

        private long GetArrayLength(object value)
        {
            return ((System.Array)value)?.Length ?? 0;
        }

        private void WriteArrayValues(object array, Encoder.WriteItem valueWriter, IWriter encoder)
        {
            if (array == null)
            {
                valueWriter(null, encoder);
            }
            else
            {
                var arrayInstance = (System.Array)array;
                for (int i = 0; i < arrayInstance.Length; i++)
                {
                    encoder.StartItem();
                    valueWriter(arrayInstance.GetValue(i), encoder);
                }
            }
        }
    }
}