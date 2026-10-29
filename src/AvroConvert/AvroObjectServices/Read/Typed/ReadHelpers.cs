using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>Loop helpers called from compiled read plans; keep the expression trees small and the item delegates typed.</summary>
    internal static class ReadHelpers
    {

        internal static void ReadCountInto<T>(Reader reader, long count, Func<Reader, T> readItem, List<T> list)
        {
            for (long i = 0; i < count; i++)
            {
                list.Add(readItem(reader));
            }
        }

        /// <summary>
        /// Reads <paramref name="count"/> container entries that are each an array of items and flattens them. When the
        /// entry schema is a union of null and the array, <paramref name="nullIndex"/> is the null branch (-1 otherwise).
        /// </summary>
        internal static void ReadArraysInto<T>(Reader reader, long count, Func<Reader, T> readItem, List<T> list, int nullIndex)
        {
            for (long i = 0; i < count; i++)
            {
                if (nullIndex >= 0 && reader.ReadUnionIndex() == nullIndex)
                {
                    continue;
                }

                for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
                {
                    for (long j = 0; j < n; j++)
                    {
                        list.Add(readItem(reader));
                    }
                }
            }
        }

        internal static Dictionary<string, T> ReadMap<T>(Reader reader, Func<Reader, T> readValue)
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

        /// <summary>A dictionary type with a parameterless constructor (e.g. a class deriving from Dictionary) filled with the read entries.</summary>
        internal static TTarget CopyMap<TTarget, TKey, TValue>(Dictionary<TKey, TValue> source) where TTarget : IDictionary<TKey, TValue>, new()
        {
            var result = new TTarget();
            foreach (var entry in source)
            {
                result.Add(entry.Key, entry.Value);
            }

            return result;
        }

        /// <summary>Map whose string keys are converted to <typeparamref name="TKey"/> (Uri, Guid, ...).</summary>
        internal static Dictionary<TKey, TValue> ReadMapKeyed<TKey, TValue>(Reader reader, Func<Reader, TKey> readKey, Func<Reader, TValue> readValue)
        {
            var result = new Dictionary<TKey, TValue>();
            for (long n = reader.ReadMapStart(); n != 0; n = reader.ReadMapNext())
            {
                for (long i = 0; i < n; i++)
                {
                    var key = readKey(reader);
                    result[key] = readValue(reader);
                }
            }

            return result;
        }

        /// <summary>Dictionary with arbitrary key type, encoded as an array of {Key, Value} records.</summary>
        internal static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Reader reader, Func<Reader, TKey> readKey, Func<Reader, TValue> readValue)
        {
            var result = new Dictionary<TKey, TValue>();
            for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
            {
                for (long i = 0; i < n; i++)
                {
                    var key = readKey(reader);
                    result.Add(key, readValue(reader));
                }
            }

            return result;
        }

        internal static TCollection Fold<TCollection, T>(List<T> items, TCollection empty, Func<TCollection, T, TCollection> add)
        {
            var result = empty;
            foreach (var item in items)
            {
                result = add(result, item);
            }

            return result;
        }

        internal static TCollection AddAll<TCollection, T>(TCollection collection, List<T> items, Action<TCollection, T> add)
        {
            foreach (var item in items)
            {
                add(collection, item);
            }

            return collection;
        }

        internal static Exception UnionIndexOutOfRange(int index, int count) =>
            new ArgumentOutOfRangeException("index", $"Cannot get union member of index [{index}]. Union size: [{count}]");
    }
}
