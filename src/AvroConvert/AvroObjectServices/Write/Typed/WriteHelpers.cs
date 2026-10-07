using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>Loop helpers called from compiled write plans; keep expression trees small and item delegates typed.</summary>
    internal static class WriteHelpers
    {
        internal static void WriteArray<T>(IWriter writer, T[] items, Action<IWriter, T> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                writer.WriteItemCount(items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    writeItem(writer, items[i]);
                }
            }
            writer.WriteArrayEnd();
        }

        internal static void WriteList<T>(IWriter writer, IReadOnlyList<T> items, Action<IWriter, T> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                writer.WriteItemCount(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    writeItem(writer, items[i]);
                }
            }
            writer.WriteArrayEnd();
        }

        internal static void WriteEnumerable<T>(IWriter writer, IEnumerable<T> items, Action<IWriter, T> writeItem)
        {
            writer.WriteArrayStart();
            if (items != null)
            {
                if (items is IReadOnlyCollection<T> readOnly)
                {
                    writer.WriteItemCount(readOnly.Count);
                }
                else if (items is ICollection<T> collection)
                {
                    writer.WriteItemCount(collection.Count);
                }
                else
                {
                    // Unknown count: Avro needs the block size before its items.
                    items = new List<T>(items);
                    writer.WriteItemCount(((List<T>)items).Count);
                }

                foreach (var item in items)
                {
                    writeItem(writer, item);
                }
            }
            writer.WriteArrayEnd();
        }

        internal static void WriteMap<T>(IWriter writer, IEnumerable<KeyValuePair<string, T>> entries, int count, Action<IWriter, T> writeValue)
        {
            writer.WriteMapStart();
            writer.WriteItemCount(count);
            foreach (var entry in entries)
            {
                writer.WriteString(entry.Key);
                writeValue(writer, entry.Value);
            }
            writer.WriteMapEnd();
        }
    }
}
