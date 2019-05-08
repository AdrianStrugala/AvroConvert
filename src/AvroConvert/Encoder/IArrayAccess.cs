namespace EhwarSoft.Avro.Encoder
{
    public interface IArrayAccess
    {
        /// <summary>
        /// Checks if the given object is an array. If it is a valid array, this function returns normally. Otherwise,
        /// it throws an exception. The default implementation checks if the value is an array.
        /// </summary>
        /// <param name="value"></param>
        object EnsureArrayObject(object value);

        /// <summary>
        /// Returns the length of an array. The default implementation requires the object
        /// to be an array of objects and returns its length. The defaul implementation
        /// gurantees that EnsureArrayObject() has been called on the value before this
        /// function is called.
        /// </summary>
        /// <param name="value">The object whose array length is required</param>
        /// <returns>The array length of the given object</returns>
        long GetArrayLength(object value);

        /// <summary>
        /// Returns the element at the given index from the given array object. The default implementation
        /// requires that the value is an object array and returns the element in that array. The defaul implementation
        /// gurantees that EnsureArrayObject() has been called on the value before this
        /// function is called.
        /// </summary>
        /// <param name="value">The array object</param>
        /// <param name="index">The index to look for</param>
        /// <returns>The array element at the index</returns>
        void WriteArrayValues(object array, AbstractEncoder.WriteItem valueWriter, IWriter encoder);

    }
}