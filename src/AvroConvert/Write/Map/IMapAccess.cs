namespace EhwarSoft.AvroConvert.Write.Map
{
    public interface IMapAccess
    {
        /// <summary>
        /// Checks if the given object is a map. If it is a valid map, this function returns normally. Otherwise,
        /// it throws an exception. The default implementation checks if the value is an IDictionary<string, object>.
        /// </summary>
        /// <param name="value"></param>
        void EnsureMapObject(object value);

        /// <summary>
        /// Returns the size of the map object. The default implementation gurantees that EnsureMapObject has been
        /// successfully called with the given value. The default implementation requires the value
        /// to be an IDictionary<string, object> and returns the number of elements in it.
        /// </summary>
        /// <param name="value">The map object whose size is desired</param>
        /// <returns>The size of the given map object</returns>
        long GetMapSize(object value);

        /// <summary>
        /// Returns the contents of the given map object. The default implementation guarantees that EnsureMapObject
        /// has been called with the given value. The defualt implementation of this method requires that
        /// the value is an IDictionary<string, object> and returns its contents.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="valueWriter"></param>
        /// <param name="encoder"></param>
        /// <param name="value">The map object whose size is desired</param>
        /// <returns>The contents of the given map object</returns>
        void WriteMapValues(object map, Encoder.WriteItem valueWriter, IWriter encoder);
    }
}