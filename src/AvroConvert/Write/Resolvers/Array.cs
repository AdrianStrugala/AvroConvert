namespace EhwarSoft.AvroConvert.Write.Resolvers
{
    using System.Collections;

    public class Array
    {
        public object EnsureArrayObject(object value)
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

        public long GetArrayLength(object value)
        {
            return ((System.Array)value)?.Length ?? 0;
        }

        public void WriteArrayValues(object array, Encoder.WriteItem valueWriter, IWriter encoder)
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