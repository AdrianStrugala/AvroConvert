#region license
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

using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Schema;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write.Resolvers
{
    internal class Record
    {
        internal Encoder.WriteItem Resolve(RecordSchema recordSchema)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            int index = 0;
            foreach (Field field in recordSchema)
            {
                var record = new WriteStep
                {
                    WriteField = Resolver.ResolveWriter(field.Schema),
                    Field = field
                };
                writeSteps[index++] = record;
            }

            void RecordResolver(object v, IWriter e)
            {
                WriteRecordFields(v, writeSteps, e);
            }


            return RecordResolver;
        }
        private void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder)
        {
            foreach (var writer in writers)
            {
                string name = writer.Field.aliases?[0] ?? writer.Field.Name;

                object value = recordObj.GetType().GetProperty(name)?.GetValue(recordObj, null);
                if (value == null)
                {
                    value = recordObj.GetType().GetField(name)?.GetValue(recordObj);
                }

                writer.WriteField(value, encoder);
            }
        }
    }
}
