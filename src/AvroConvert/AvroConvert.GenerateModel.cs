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
using SolTechnology.Avro.Features.GenerateModel;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Generates C# .NET classes from given AVRO object containing schema
        /// </summary>
        public static string GenerateModel(byte[] avroBytes)
        {
            var generateClassHandler = new GenerateModel();
            var result = generateClassHandler.FromAvroObject(avroBytes);

            return result;
        }

        /// <summary>
        /// Generates C# .NET classes from given AVRO schema
        /// </summary>
        public static string GenerateModel(string schema)
        {
            var generateClassHandler = new GenerateModel();
            var result = generateClassHandler.FromAvroSchema(schema);

            return result;
        }
    }
}
