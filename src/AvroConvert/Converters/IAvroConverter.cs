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
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro.Converters
{
    /// <summary>
    /// Defines an interface for custom Avro converters.
    /// </summary>
    /// <remarks>
    /// Implement this interface to provide custom serialization and deserialization
    /// behavior for Avro data based on a specified runtimeType in the <see cref="TypeSchema"/>.
    /// </remarks>
    public interface IAvroConverter
    {
        /// <summary>
        /// Gets the <see cref="TypeSchema"/> associated with this Avro converter.
        /// The <see cref="TypeSchema"/> contains runtime type information on which matching is done.
        /// </summary>
        public TypeSchema TypeSchema { get; }

        /// <summary>
        /// Serializes the provided data into Avro format and writes it to the specified <see cref="IWriter"/>.
        /// </summary>
        /// <param name="data">The data to be serialized.</param>
        /// <param name="writer">The Avro writer used for serialization.</param>
        public void Serialize(object data, IWriter writer);

        /// <summary>
        /// Deserializes Avro data from the specified <see cref="IReader"/> and returns the deserialized object.
        /// </summary>
        /// <param name="reader">The Avro reader used for deserialization.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(IReader reader);
    }
}
