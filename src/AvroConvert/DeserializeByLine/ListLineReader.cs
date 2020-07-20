using System;

namespace SolTechnology.Avro.DeserializeByLine
{
    internal class ListLineReader<T> : ILineReader<T>, IDisposable
    {
        public bool HasNext()
        {
            throw new NotImplementedException();
        }

        public T ReadNext()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
