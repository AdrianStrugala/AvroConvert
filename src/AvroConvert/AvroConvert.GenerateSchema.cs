﻿#region license
/**Copyright (c) 2020 Adrian Strugała
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
using SolTechnology.Avro.AvroObjectServices.BuildSchema;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Generates schema for given .NET Type
        /// </summary>
        public static string GenerateSchema(Type type)
        {
            var schemaBuilder = new ReflectionSchemaBuilder();
            var schema = schemaBuilder.BuildSchema(type);

            return schema.ToString();
        }


        /// <summary>
        /// Generates schema for given .NET Type
        /// <paramref name="includeOnlyDataContractMembers"/> indicates if only classes with DataContractAttribute and properties marked with DataMemberAttribute should be returned
        /// </summary>
        public static string GenerateSchema(Type type, bool includeOnlyDataContractMembers)
        {
            return GenerateSchema(type, new AvroConvertOptions
            {
                IncludeOnlyDataContractMembers = includeOnlyDataContractMembers
            });
        }

        /// <summary>
        /// Generates schema for given .NET Type
        /// </summary>
        /// <param name="type">The given .NET type</param>
        /// <param name="options">Options to use for generating the schema</param>
        /// <returns></returns>
        public static string GenerateSchema(Type type, AvroConvertOptions options)
        {
            var builder = new ReflectionSchemaBuilder(options);
            var schema = builder.BuildSchema(type);

            return schema.ToString();
        }
    }
}
