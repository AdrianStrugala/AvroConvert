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


using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.AvroObjectServices.Schemas;
namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Json
    {
        internal Encoder.WriteItem Resolve(RecordSchema recordSchema)
        {
            WriteStep[] writeSteps = new WriteStep[recordSchema.Fields.Count];

            int index = 0;
            foreach (RecordFieldSchema field in recordSchema.Fields)
            {
                var record = new WriteStep
                {
                    WriteField = Resolver.ResolveWriter(field.TypeSchema),
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
            HandleJObject((JObject)recordObj, writers, encoder);
        }

        private void HandleJObject(JObject jObject, WriteStep[] writers, IWriter encoder)
        {
            foreach (var writer in writers)
            {
                string name = writer.Field.Aliases.FirstOrDefault() ?? writer.Field.Name;

                object value = jObject.Properties().FirstOrDefault(x => x.Name == name).Value;

                switch (value)
                {
                    case JObject _:
                        break;
                    case JArray jArray:
                        value = jArray.ToObject<object[]>();
                        value = ((object[])value).Select(SwitchJsonType);
                        break;
                    case JValue jValue:
                        value = jValue.Value;
                        break;
                }

                writer.WriteField(value, encoder);
            }
        }

        private object SwitchJsonType(object value)
        {
            switch (value)
            {
                case JObject _:
                    break;
                case JArray jArray:
                    value = jArray.ToObject<object[]>();
                    break;
                case JValue jValue:
                    value = jValue.Value;
                    break;
            }

            return value;
        }
    }
}
