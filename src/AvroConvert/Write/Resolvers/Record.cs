using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    public class Record
    {
        public Encoder.WriteItem Resolve(RecordSchema recordSchema)
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
