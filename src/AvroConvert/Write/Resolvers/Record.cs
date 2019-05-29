namespace AvroConvert.Write.Resolvers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Exceptions;
    using Models;
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
            Models.Record record = new Models.Record(schema);

            if (recordObj is Dictionary<string, object> obj)
            {
                record.contents = obj;
            }

            else
            {
                record.contents = SplitKeyValues(recordObj);
            }

            foreach (var writer in writers)
            {
                writer.WriteField(record[writer.Field.Name], encoder);
            }
        }

        private Dictionary<string, object> SplitKeyValues(object item)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (item == null)
            {
                return result;
            }

            Type objType = item.GetType();
            PropertyInfo[] properties = objType.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                var value = FindValue(prop, item);

                if (value == null)
                {
                    result.Add(prop.Name, null);
                }

                else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    // We have a List<T> or array                  
                    result.Add(prop.Name, GetSplitList((IList)value));
                }

                else if (prop.PropertyType == typeof(Guid))
                {
                    // We have a guid type
                    result.Add(prop.Name, value.ToString());
                }

                else if (prop.PropertyType.GetTypeInfo().IsValueType ||
                         prop.PropertyType == typeof(string))
                {
                    // We have a simple type
                    result.Add(prop.Name, value);
                }
                else
                {
                    //complex type
                    result.Add(prop.Name, SplitKeyValues(value));
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
