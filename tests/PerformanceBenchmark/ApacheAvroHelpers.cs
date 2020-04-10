using Avro;
using Avro.Generic;

namespace SolTechnology.PerformanceBenchmark
{
    public static class ApacheAvroHelpers
    {
        public static GenericRecord Create(object item, Schema schema)
        {
            var genericRecord = new GenericRecord((RecordSchema)schema);

            var props = item.GetType().GetProperties();

            foreach (var propertyInfo in props)
            {
                genericRecord.Add(propertyInfo.Name, propertyInfo.GetValue(item));
            }

            return genericRecord;
        }

        public static T Decreate<T>(GenericRecord genericRecord) where T : new()
        {
            T result = new T();
            var props = typeof(T).GetProperties();

            foreach (var propertyInfo in props)
            {
                genericRecord.TryGetValue(propertyInfo.Name, out var value);
                propertyInfo.SetValue(result, value);
            }

            return result;
        }
    }
}