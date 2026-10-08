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
using System.Collections.Generic;
using SolTechnology.Avro.Converters;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro;

/// <summary>
/// Represents options for configuring Avro conversion and serialization settings.
/// </summary>
/// <remarks>
/// Use this class to configure Avro conversion options, including a collection of custom Avro converters
/// and the preferred codec type for serialization.
/// </remarks>
public class AvroConvertOptions
{
    /// <summary>
    /// Gets or sets a collection of custom Avro converters used for custom serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// Avro converters implement the <see cref="IAvroConverter"/> interface to provide specialized
    /// serialization and deserialization behavior for specific runtime types.
    /// </remarks>
    public List<IAvroConverter> AvroConverters { get; set; } = new();

    /// <summary>
    /// Gets or sets the preferred codec type for Avro serialization.
    /// </summary>
    /// <remarks>
    /// The codec type determines the compression algorithm used during serialization.
    /// </remarks>
    public CodecType Codec { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of items in the schema tree.
    /// </summary>
    /// <value>
    ///     The maximum number of items in the schema tree.
    /// </value>
    public int MaxItemsInSchemaTree { get; set; } = 1024;

    /// <summary>
    /// If set to <c>true</c> members without DataMemberAttribute won't be taken into consideration in serialization/deserialization
    /// </summary>
    public bool IncludeOnlyDataContractMembers { get; set; }
    
    /// <summary>
    /// Gets or sets the naming policy that can determine how types and fields are named.
    /// </summary>
    public IAvroNamingPolicy NamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets the number handling behavior for Avro types.
    /// </summary>
    public AvroNumberHandling NumberHandling { get; set; }

    /// <summary>
    /// Gets or sets how reader-schema fields that are absent from the writer schema and have no <c>default</c> are handled.
    /// Defaults to <see cref="AvroMissingFieldHandling.Throw"/> as required by the Avro specification.
    /// </summary>
    public AvroMissingFieldHandling MissingFieldHandling { get; set; }

    /// <summary>
    /// Gets or sets how a top-level collection is written by <c>Serialize</c>.
    /// Defaults to <see cref="AvroCollectionMode.Entries"/> (one container entry per element, like other Avro implementations).
    /// </summary>
    public AvroCollectionMode CollectionMode { get; set; }
}