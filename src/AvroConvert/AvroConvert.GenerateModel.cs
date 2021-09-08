#region license
/**Copyright (c) 2021 Adrian Strugala
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

using SolTechnology.Avro.Features.GenerateModel;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Generates C# .NET classes from schema in given AVRO object
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
