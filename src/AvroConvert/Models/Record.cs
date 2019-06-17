namespace AvroConvert.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Schema;

    public class Record
    {
        public RecordSchema Schema { get; }

        public IDictionary<string, object> Contents = new Dictionary<string, object>();
        public Record(RecordSchema schema)
        {
            Schema = schema;
        }

        public object this[string fieldName] => Contents.TryGetValue(fieldName, out var value) ? value : null;


        public bool TryGetValue(string fieldName, out object result)
        {
            return Contents.TryGetValue(fieldName, out result);
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj != null && obj is Record other)
            {
                return Schema.Equals(other.Schema) && AreEqual(Contents, other.Contents);
            }
            return false;
        }

        private static bool AreEqual(IDictionary<string, object> d1, IDictionary<string, object> d2)
        {
            if (d1.Count == d2.Count)
            {
                foreach (KeyValuePair<string, object> keyValuePair in d1)
                {
                    if (!d2.TryGetValue(keyValuePair.Key, out var o)) return false;
                    if (!AreEqual(o, keyValuePair.Value)) return false;
                }

                return true;
            }
            return false;
        }

        private static bool AreEqual(object object1, object object2)
        {
            if (object1 == null) return object2 == null;
            if (object2 == null) return false;
            switch (object1)
            {
                case Array array1 when !(object2 is Array):
                    return false;
                case Array array1:
                    return AreEqual(array1, array1);
                case IDictionary<string, object> _ when !(object2 is IDictionary<string, object>):
                    return false;
                case IDictionary<string, object> dictionary1:
                    return AreEqual(dictionary1, dictionary1);
                default:
                    return object1.Equals(object2);
            }
        }

        private static bool AreEqual(Array array1, Array array2)
        {
            if (array1.Length != array2.Length) return false;
            return !array1.Cast<object>().Where((t, i) => !AreEqual(array1.GetValue(i), array2.GetValue(i))).Any();
        }

        public override int GetHashCode()
        {
            return 31 * Contents.GetHashCode()/* + 29 * Schema.GetHashCode()*/;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Schema: ");
            sb.Append(Schema);
            sb.Append(", contents: ");
            sb.Append("{ ");
            foreach (KeyValuePair<string, object> kv in Contents)
            {
                sb.Append(kv.Key);
                sb.Append(": ");
                sb.Append(kv.Value);
                sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
