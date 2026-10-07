#region license
/**Copyright (c) 2022 Adrian Strugala
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
using System.Dynamic;
using System.Linq;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Write.Resolvers;
using SolTechnology.Avro.Infrastructure.Reflection;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveRecord(RecordSchema recordSchema)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            int index = 0;
            foreach (RecordFieldSchema field in recordSchema.Fields)
            {
                var record = new WriteStep
                {
                    WriteField = ResolveWriter(field.TypeSchema),
                    FiledName = field.Aliases.FirstOrDefault() ?? field.Name,
                };
                writeSteps[index++] = record;
            }

            void RecordResolver(object v, IWriter e)
            {
                WriteRecordFields(v, writeSteps, e);
            }

            return RecordResolver;
        }

        private static void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder)
        {
            if (recordObj is null)
            {
                encoder.WriteNull();
                return;
            }

            if (recordObj is ExpandoObject expando)
            {
                HandleExpando(writers, encoder, expando);
                return;
            }

            var type = recordObj.GetType();

            foreach (var writer in writers)
            {
                if (!ReferenceEquals(writer.CachedType, type))
                {
                    writer.CachedType = type;
                    writer.CachedGetter = TypeMembers.For(type).Find(writer.FiledName)?.Get;
                }

                var value = writer.CachedGetter?.Invoke(recordObj);
                writer.WriteField(value, encoder);
            }
        }

        private static void HandleExpando(WriteStep[] writers, IWriter encoder, ExpandoObject expando)
        {
            var expandoDictionary = expando.ToDictionary(x => x.Key, y => y.Value, StringComparer.InvariantCultureIgnoreCase);

            foreach (var writer in writers)
            {
                expandoDictionary.TryGetValue(writer.FiledName, out var value);
                writer.WriteField(value, encoder);
            }
        }
    }
}
