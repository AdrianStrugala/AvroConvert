#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using SolTechnology.Avro.AvroObjectServices.Read;

namespace SolTechnology.Avro.Features.DeserializeByLine.LineReaders
{
    internal class ListLineReader<T> : ILineReader<T>
    {
        private readonly IReader reader;
        private readonly Resolver resolver;
        private long itemsCount;
        private bool finished;

        public ListLineReader(IReader reader, Resolver resolver)
        {
            this.reader = reader;
            this.resolver = resolver;

            itemsCount = reader.ReadArrayStart();
            finished = itemsCount == 0;
        }
        public bool HasNext()
        {
            if (itemsCount == 0)
            {
                // A zero count terminates the array; reading another count after it would consume the next entry.
                if (finished)
                {
                    return false;
                }

                itemsCount = reader.ReadArrayNext();
                finished = itemsCount == 0;
                return !finished;
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
