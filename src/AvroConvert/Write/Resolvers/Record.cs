namespace AvroConvert.Write.Resolvers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
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
                    WriteField = Resolver.ResolveWriter(field.Schema),
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

        private Dictionary<string, object> SplitKeyValues(object item, RecordSchema schema)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if ((item == null) || (schema?.Fields == null))
            {
                return result;
            }

            PropertyInfo[] properties = item.GetType().GetProperties();
            FieldInfo[] fields = item.GetType().GetFields();


            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttributes(false).OfType<DataMemberAttribute>().SingleOrDefault();
                string propertyName = attribute?.Name ?? property.Name;

                var schemaField = schema.Fields.SingleOrDefault(f => f.Name == propertyName);

                if (schemaField == null)
                {
                    continue;
                }

                var value = FindPropertyValue(property, item);

                result = SmartAddValueToResult(value, result, schemaField, property.PropertyType);
            }

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttributes(false).OfType<DataMemberAttribute>().SingleOrDefault();
                string fieldName = attribute?.Name ?? field.Name;

                var schemaField = schema.Fields.Single(f => f.Name == fieldName);

                if (schemaField == null)
                {
                    continue;
                }

                var value = FindFieldValue(field, item);

                result = SmartAddValueToResult(value, result, schemaField, field.FieldType);
            }

            return result;
        }

        private Dictionary<string, object> SmartAddValueToResult(dynamic value, Dictionary<string, object> result, Field schemaField, Type fieldType)
        {
            if (value == null)
            {
                result.Add(schemaField.Name, null);
            }

            else if (typeof(IList).IsAssignableFrom(fieldType))
            {
                // We have a List<T> or array                  
                result.Add(schemaField.Name, GetSplitList((IList)value, (ArraySchema)schemaField.Schema));
            }

            else if (fieldType.GetTypeInfo().IsValueType || fieldType == typeof(string))
            {
                // We have a simple type
                result.Add(schemaField.Name, value);
            }
            else
            {
                //complex type
                result.Add(schemaField.Name, SplitKeyValues(value, (RecordSchema)schemaField.Schema));
            }

            return result;
        }

        private IList GetSplitList(IList list, ArraySchema schema)
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
                result.Add(item != null ? SplitKeyValues(item, (RecordSchema)schema.ItemSchema) : null);
            }

            return result;
        }

        private dynamic FindPropertyValue(PropertyInfo propertyInfo, object item)
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

        private dynamic FindFieldValue(FieldInfo fieldInfo, object item)
        {
            dynamic value = null;
            try
            {
                value = fieldInfo.GetValue(item);
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
