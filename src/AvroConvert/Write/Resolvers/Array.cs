namespace AvroConvert.Write.Resolvers
{
    using System.Collections;
    using Schema;

    public class Array
    {
        public Encoder.WriteItem Resolve(ArraySchema schema)
        {
            var itemWriter = Factory.ResolveWriter(schema.ItemSchema);
            return (d, e) => WriteArray(itemWriter, d, e);
        }

        private void WriteArray(Encoder.WriteItem itemWriter, object array, IWriter encoder)
        {
            array = EnsureArrayObject(array);
            long l = GetArrayLength(array);
            encoder.WriteArrayStart();
            encoder.SetItemCount(l);
            WriteArrayValues(array, itemWriter, encoder);
            encoder.WriteArrayEnd();
        }

        private object EnsureArrayObject(object value)
        {
            if (value == null)
            {
                return null;
            }

            IList list = value as IList;
            int length = list.Count;

            object[] result = new object[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = list[i];
            }

            return result;
        }

        private long GetArrayLength(object value)
        {
            return ((System.Array)value)?.Length ?? 0;
        }

        private void WriteArrayValues(object array, Encoder.WriteItem valueWriter, IWriter encoder)
        {
            if (array == null)
            {
                valueWriter(null, encoder);
            }
            else
            {
                var arrayInstance = (System.Array)array;
                for (int i = 0; i < arrayInstance.Length; i++)
                {
                    encoder.StartItem();
                    valueWriter(arrayInstance.GetValue(i), encoder);
                }
            }
        }
    }
}