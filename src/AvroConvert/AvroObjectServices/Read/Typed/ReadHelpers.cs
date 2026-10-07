using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>Loop helpers called from compiled read plans; keep the expression trees small and the item delegates typed.</summary>
    internal static class ReadHelpers
    {
        internal static List<T> ReadList<T>(IReader reader, Func<IReader, T> readItem)
        {
            long n = reader.ReadArrayStart();
            var list = new List<T>(n > 0 && n < 1024 ? (int)n : 4);
            for (; n != 0; n = reader.ReadArrayNext())
            {
                for (long i = 0; i < n; i++)
                {
                    list.Add(readItem(reader));
                }
            }

            return list;
        }

        internal static List<T> ReadCount<T>(IReader reader, long count, Func<IReader, T> readItem)
        {
            var list = new List<T>(count < 1024 ? (int)count : 1024);
            ReadCountInto(reader, count, readItem, list);
            return list;
        }

        internal static void ReadCountInto<T>(IReader reader, long count, Func<IReader, T> readItem, List<T> list)
        {
            for (long i = 0; i < count; i++)
            {
                list.Add(readItem(reader));
            }
        }

        internal static Dictionary<string, T> ReadMap<T>(IReader reader, Func<IReader, T> readValue)
        {
            var result = new Dictionary<string, T>();
            for (long n = reader.ReadMapStart(); n != 0; n = reader.ReadMapNext())
            {
                for (long i = 0; i < n; i++)
                {
                    var key = reader.ReadString();
                    result[key] = readValue(reader);
                }
            }

            return result;
        }

        internal static Exception UnionIndexOutOfRange(int index, int count) =>
            new ArgumentOutOfRangeException("index", $"Cannot get union member of index [{index}]. Union size: [{count}]");
    }
}
