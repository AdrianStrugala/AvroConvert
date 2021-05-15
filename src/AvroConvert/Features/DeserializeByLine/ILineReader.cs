using System;

namespace SolTechnology.Avro.Features.DeserializeByLine
{
    public interface ILineReader<T> : IDisposable
    {
        bool HasNext();
        T ReadNext();
    }
}
