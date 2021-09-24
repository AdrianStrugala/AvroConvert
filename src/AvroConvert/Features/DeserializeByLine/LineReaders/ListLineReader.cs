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

using SolTechnology.Avro.AvroObjectServices.Read;

namespace SolTechnology.Avro.Features.DeserializeByLine.LineReaders
{
    internal class ListLineReader<T> : ILineReader<T>
    {
        private readonly IReader reader;
        private readonly Resolver resolver;
        private int itemsCount;

        public ListLineReader(IReader reader, Resolver resolver)
        {
            this.reader = reader;
            this.resolver = resolver;

            itemsCount = (int)reader.ReadArrayStart();
        }
        public bool HasNext()
        {
            if (itemsCount == 0)
            {
                itemsCount = (int)reader.ReadArrayNext();
                return itemsCount != 0;
            }
            else
            {
                return true;
            }
        }

        public T ReadNext()
        {
            var result = resolver.Resolve<T>(reader);
            itemsCount--;
            return result;
        }

        public void Dispose()
        {
        }
    }
}
