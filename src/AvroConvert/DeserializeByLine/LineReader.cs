using System;
using System.IO;
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro.DeserializeByLine
{
    public class LineReader<T> : IDisposable
    {
        private readonly IReader reader;
        private readonly Resolver resolver;
        private T _next;

        internal LineReader(IReader reader, Resolver resolver)
        {
            this.reader = reader;
            this.resolver = resolver;
        }


        public bool HasNext()
        {
            try
            {
                var result = resolver.Resolve<T>(reader, 0);

                if (result == null)
                {
                    return false;
                }
                else
                {
                    _next = result;
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public T ReadNext()
        {
            return _next;
        }

        public void Dispose()
        {

        }
    }
}