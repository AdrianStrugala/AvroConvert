using System;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Exceptions
{
    internal class EndOfStreamReachedException : Exception
    {
        internal EndOfStreamReachedException()
            : base()
        {
        }

        internal EndOfStreamReachedException(string s)
            : base(s)
        {
        }

        internal EndOfStreamReachedException(string s, Exception inner)
            : base(s, inner)
        {
        }
    }
}
