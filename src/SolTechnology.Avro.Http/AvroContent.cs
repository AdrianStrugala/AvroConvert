using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http;

/// <summary>HTTP content holding a value serialized to an Avro object container (<c>application/avro</c>).</summary>
public sealed class AvroContent : HttpContent
{
    private readonly byte[] _body;

    private AvroContent(object value, Type objectType, byte[] body)
    {
        Value = value;
        ObjectType = objectType;
        _body = body;
        Headers.ContentType = new MediaTypeHeaderValue(AvroMediaTypes.Avro);
    }

    /// <summary>The serialized value.</summary>
    public object Value { get; }

    /// <summary>The declared type of <see cref="Value"/>.</summary>
    public Type ObjectType { get; }

    /// <summary>Serializes <paramref name="value"/> to Avro.</summary>
    public static AvroContent Create<T>(T value, AvroConvertOptions options = null) =>
        Create(value, typeof(T), options);

    /// <summary>Serializes <paramref name="value"/> of type <paramref name="inputType"/> to Avro.</summary>
    public static AvroContent Create(object value, Type inputType, AvroConvertOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(inputType);
        var body = options == null ? AvroConvert.Serialize(value) : AvroConvert.Serialize(value, options);
        return new AvroContent(value, inputType, body);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
        stream.WriteAsync(_body).AsTask();

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
        stream.WriteAsync(_body, cancellationToken).AsTask();

    protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
        stream.Write(_body);

    protected override Task<Stream> CreateContentReadStreamAsync() =>
        Task.FromResult<Stream>(new MemoryStream(_body, writable: false));

    protected override bool TryComputeLength(out long length)
    {
        length = _body.Length;
        return true;
    }
}
