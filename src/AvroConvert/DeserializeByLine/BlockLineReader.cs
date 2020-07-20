using SolTechnology.Avro.Read;

namespace SolTechnology.Avro.DeserializeByLine
{
    public class BlockLineReader<T> : ILineReader<T>
    {
        private readonly IReader reader;
        private readonly Resolver resolver;
        private long blockCount;

        internal BlockLineReader(IReader reader, Resolver resolver, long blockCount)
        {
            this.reader = reader;
            this.resolver = resolver;
            this.blockCount = blockCount;
        }

        public bool HasNext()
        {
            return blockCount != 0;
        }

        public T ReadNext()
        {
            var result = resolver.Resolve<T>(reader, 0);
            blockCount--;

            return result;
        }

        public void Dispose()
        {
        }
    }
}