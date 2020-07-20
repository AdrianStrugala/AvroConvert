using System;
using System.Collections.Generic;
using System.Text;

namespace SolTechnology.Avro.DeserializeByLine
{
    internal interface ILineReader<T> : IDisposable
    {
        bool HasNext();
        T ReadNext();
    }
}
