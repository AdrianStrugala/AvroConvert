#region license
/**Copyright (c) 2023 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
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
}