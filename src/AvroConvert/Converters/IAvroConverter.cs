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
