namespace AvroConvert.Write.Resolvers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Exceptions;
    using Schema;

    public class Record
    {
        public Encoder.WriteItem Resolve(RecordSchema recordSchema)
        {
            var writeSteps = new WriteStep[recordSchema.Fields.Count];
            Encoder.WriteItem recordResolver = (v, e) => WriteRecordFields(v, writeSteps, e, recordSchema);


            int index = 0;
            foreach (Field field in recordSchema)
            {
                var record = new WriteStep
                {
                    WriteField = Factory.ResolveWriter(field.Schema),
                    Field = field
                };
                writeSteps[index++] = record;
            }

            return recordResolver;
        }
        private void WriteRecordFields(object recordObj, WriteStep[] writers, IWriter encoder, RecordSchema schema)
        {
            var record = new Models.Record(schema);

            if (recordObj is Dictionary<string, object> obj)
            {
                record.Contents = obj;
            }

            else
            {
                record.Contents = SplitKeyValues(recordObj, schema);
            }

            foreach (var writer in writers)
            {
                writer.WriteField(record[writer.Field.Name], encoder);
            }
        }

        //TODO: REFACTOR
        private Dictionary<string, object> SplitKeyValues(object item, RecordSchema schema = null)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (item == null)
            {
                return result;
            }

            Type objType = item.GetType();
            PropertyInfo[] properties = objType.GetProperties();
            

            for (var i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var propName = schema?.Fields[i].Name ?? properties[i].Name;

                var value = FindValue(prop, item);

                if (value == null)
                {
                    result.Add(propName, null);
                }

                else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    // We have a List<T> or array                  
                    result.Add(propName, GetSplitList((IList)value));
                }

                else if (prop.PropertyType.GetTypeInfo().IsValueType ||
                         prop.PropertyType == typeof(string))
                {
                    // We have a simple type
                    result.Add(propName, value);
                }
                else
                {
                    //complex type
                    if (schema?.Fields[i].Schema is RecordSchema recordSchema)
                    {
                        result.Add(propName, SplitKeyValues(value, recordSchema));
                    }
                    else
                    {
                        result.Add(propName, SplitKeyValues(value));
                    }
                }
            }

            FieldInfo[] fields = objType.GetFields();

            foreach (var fieldInfo in fields)
            {
                if ((schema?.Fields.Select(f => f?.Name == fieldInfo.Name)).Any())
                {
                    result.Add(fieldInfo.Name, fieldInfo.GetValue(item));
                }
            }

            return result;
        }

        private IList GetSplitList(IList list)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var typeToCheck = list.GetType().GetProperties()[2].PropertyType;

            if (typeToCheck.GetTypeInfo().IsValueType ||
                typeToCheck == typeof(string))
            {
                return list;
            }

            List<object> result = new List<object>();

            foreach (var item in list)
            {
                result.Add(item != null ? SplitKeyValues(item) : null);
            }

            return result;
        }

        private dynamic FindValue(PropertyInfo propertyInfo, object item)
        {
            dynamic value = null;
            try
            {
                value = propertyInfo.GetValue(item);
            }
            catch (Exception)
            {
                //no value
            }
            return value;
        }

        private void EnsureRecordObject(RecordSchema recordSchema, object value)
        {
            if (value == null || !(value is Models.Record) || !((value as Models.Record).Schema.Equals(recordSchema)))
            {
                throw new AvroTypeMismatchException("[GenericRecord] required to write against [Record] schema but found " + value.GetType());
            }
        }
    }
}
