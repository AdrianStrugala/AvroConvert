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