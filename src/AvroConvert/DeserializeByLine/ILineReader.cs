using System;

namespace SolTechnology.Avro.DeserializeByLine
{
    public interface ILineReader<T> : IDisposable
    {
        bool HasNext();
        T ReadNext();
    }
}
