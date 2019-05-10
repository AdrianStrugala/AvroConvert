namespace EhwarSoft.AvroConvert.Write.Array
{
    using System;
    using System.Collections;

    public class ArrayAccess : IArrayAccess
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
            return ((Array)value)?.Length ?? 0;
        }

        public void WriteArrayValues(object array, AbstractEncoder.WriteItem valueWriter, IWriter encoder)
        {
            if (array == null)
            {
                valueWriter(null, encoder);
            }
            else
            {
                var arrayInstance = (Array)array;
                for (int i = 0; i < arrayInstance.Length; i++)
                {
                    encoder.StartItem();
                    valueWriter(arrayInstance.GetValue(i), encoder);
                }
            }
        }
    }
}