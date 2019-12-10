using System.Collections;
using System.Linq;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    public class Array
    {
        public Encoder.WriteItem Resolve(ArraySchema schema)
        {
            var itemWriter = Resolver.ResolveWriter(schema.ItemSchema);
            return (d, e) => WriteArray(itemWriter, d, e);
        }

        private void WriteArray(Encoder.WriteItem itemWriter, object @object, IWriter encoder)
        {
            var array = EnsureArrayObject(@object);
            long l = GetArrayLength(array);
            encoder.WriteArrayStart();
            encoder.SetItemCount(l);
            WriteArrayValues(array, itemWriter, encoder);
            encoder.WriteArrayEnd();
        }

        private System.Array EnsureArrayObject(object value)
        {
            var enumerable = value as IEnumerable;
            var list = enumerable.Cast<object>().ToList();

            int length = list.Count;
            dynamic[] result = new dynamic[length];
            list.CopyTo(result, 0);

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