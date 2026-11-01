#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using System;
using System.Text;

namespace SolTechnology.Avro;

/// <summary>
/// A value of the container file metadata: bytes in the Avro specification. Text converts to it implicitly, as UTF-8.
/// </summary>
public readonly struct AvroMetadataValue
{
    private readonly byte[] _bytes;

    /// <summary>The value as it is written to the header.</summary>
    public byte[] Bytes => _bytes ?? Array.Empty<byte>();

    private AvroMetadataValue(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes, "value");
        _bytes = bytes;
    }

    /// <summary>A value holding <paramref name="value"/> as UTF-8.</summary>
    public static implicit operator AvroMetadataValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new AvroMetadataValue(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>A value holding <paramref name="value"/>.</summary>
    public static implicit operator AvroMetadataValue(byte[] value) => new(value);
}
