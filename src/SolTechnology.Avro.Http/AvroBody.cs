using System.IO;

namespace SolTechnology.Avro.Http;

internal static class AvroBody
{
    /// <summary>
    /// The bytes of a request body buffered in <paramref name="body"/>. When Content-Length was known the buffer is
    /// exactly the body and is returned as is, without the copy <see cref="MemoryStream.ToArray"/> makes.
    /// </summary>
    internal static byte[] ToArray(MemoryStream body) =>
        body.TryGetBuffer(out var buffer) && buffer.Offset == 0 && buffer.Count == buffer.Array!.Length
            ? buffer.Array
            : body.ToArray();
}
