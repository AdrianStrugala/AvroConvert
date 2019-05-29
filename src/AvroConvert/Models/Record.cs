namespace AvroConvert.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Schema;

    public class Record
    {
        public RecordSchema Schema { get; }

        public IDictionary<string, object> contents = new Dictionary<string, object>();
        public Record(RecordSchema schema)
        {
            this.Schema = schema;
        }

        public object this[string fieldName]
        {
            get
            {
                object value;
                return contents.TryGetValue(fieldName, out value) ? value : null;
            }
        }

        public void Add(string fieldName, object fieldValue)
        {
            if (Schema.Contains(fieldName))
            {
                // TODO: Use a matcher to verify that object has the right type for the field.
                //contents.Add(fieldName, fieldValue);
                contents[fieldName] = fieldValue;
                return;
            }
            throw new AvroException("No such field: " + fieldName);
        }

        public bool TryGetValue(string fieldName, out object result)
        {
            return contents.TryGetValue(fieldName, out result);
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj != null && obj is Record)
            {
                Record other = obj as Record;
                return Schema.Equals(other.Schema) && areEqual(contents, other.contents);
            }
            return false;
        }

        private static bool areEqual(IDictionary<string, object> d1, IDictionary<string, object> d2)
        {
            if (d1.Count == d2.Count)
            {
                foreach (KeyValuePair<string, object> kv in d1)
                {
                    object o;
                    if (!d2.TryGetValue(kv.Key, out o)) return false;
                    if (!areEqual(o, kv.Value)) return false;
                }
                return true;
            }
            return false;
        }

        private static bool areEqual(object o1, object o2)
        {
            if (o1 == null) return o2 == null;
            if (o2 == null) return false;
            if (o1 is Array)
            {
                if (!(o2 is Array)) return false;
                return areEqual(o1 as Array, o1 as Array);
            }
            else if (o1 is IDictionary<string, object>)
            {
                if (!(o2 is IDictionary<string, object>)) return false;
                return areEqual(o1 as IDictionary<string, object>, o1 as IDictionary<string, object>);
            }
            return o1.Equals(o2);
        }

        private static bool areEqual(Array a1, Array a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!areEqual(a1.GetValue(i), a2.GetValue(i))) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return 31 * contents.GetHashCode()/* + 29 * Schema.GetHashCode()*/;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Schema: ");
            sb.Append(Schema);
            sb.Append(", contents: ");
            sb.Append("{ ");
            foreach (KeyValuePair<string, object> kv in contents)
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
