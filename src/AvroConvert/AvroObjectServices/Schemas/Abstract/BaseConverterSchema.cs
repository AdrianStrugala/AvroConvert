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

using System;
using System.Collections.Generic;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.Abstract
{
    /// <summary>
    /// Represents a base class for Avro converter schemas used with <see cref="Converters.IAvroConverter"/>.
    /// The custom serialization/deserialization behavior is invoked based on the provided runtime type.
    /// </summary>
    public abstract class BaseConverterSchema : TypeSchema
    {

        internal BaseConverterSchema(Type runtimeType, IDictionary<string, string> attributes) : base(runtimeType, attributes)
        {
        }

        public BaseConverterSchema(Type runtimeType, AvroType avroType, string name, IDictionary<string, string> attributes = null) : base(runtimeType, attributes)
        {
            Type = avroType;
            Name = name;
        }
    }
}