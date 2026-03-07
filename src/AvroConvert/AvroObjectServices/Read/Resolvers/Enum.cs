#region license
/**Copyright (c) 2021 Adrian Strugala
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
using System.Linq;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        protected virtual object ResolveEnum(EnumSchema writerSchema, TypeSchema readerSchema, IReader d, Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            int position = d.ReadEnum();
            string value = writerSchema.Symbols[position];

            if (type == typeof(object))
            {
                var targetType = GetClrTypeForEnum(readerSchema, new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() });

                if (targetType == null)
                {
                    targetType = GetClrTypeForEnum(readerSchema, AppDomain.CurrentDomain.GetAssemblies());
                }

                if (targetType != null)
                {
                    type = targetType;
                }
            }

            return EnumParser.Parse(type, value, _namingPolicy);
        }

        private Type GetClrTypeForEnum(TypeSchema schema, Assembly[] assemblies)
        {
            return assemblies
                .Select(x => x.GetType(schema.Name))
                .FirstOrDefault(x => x != null);
        }
    }
}