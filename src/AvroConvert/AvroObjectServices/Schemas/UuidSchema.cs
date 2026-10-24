#region license
/**Copyright (c) 2021 Adrian Struga³a
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class UuidSchema : LogicalTypeSchema
    {
        public UuidSchema(): this(typeof(Guid))
        {
        }
        public UuidSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new StringSchema();
        }

        /// <summary>uuid annotating <c>fixed(16)</c>; the 16 bytes follow RFC 4122 (big-endian) byte order.</summary>
        public UuidSchema(Type runtimeType, FixedSchema fixedBase) : base(runtimeType)
        {
            if (fixedBase.Size != 16)
            {
                throw new ArgumentException("uuid can only annotate fixed of size 16", nameof(fixedBase));
            }

            BaseTypeSchema = fixedBase;
        }

        internal static FixedSchema CreateFixedBase() =>
            new(new NamedEntityAttributes(new SchemaName("uuid"), new List<string>(), ""), 16, typeof(Guid));

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => LogicalTypeEnum.Uuid;

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            var guid = baseValue switch
            {
                Guid g => g,
                byte[] bytes => new Guid(bytes, bigEndian: true),
                AvroTypes.AvroFixed avroFixed => new Guid(avroFixed.Value, bigEndian: true),
                _ => Guid.Parse((string)baseValue)
            };

            return readType == typeof(string) ? guid.ToString() : guid;
        }
    }
}
