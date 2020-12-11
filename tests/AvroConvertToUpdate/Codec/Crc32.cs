using System;
using System.Collections.Generic;
using System.Linq;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Codec
{
    /// <summary>
    /// Performs 32-bit reversed cyclic redundancy checks.
    /// </summary>
    internal static class Crc32
    {
        #region Constants
        /// <summary>
        /// Generator polynomial (modulo 2) for the reversed CRC32 algorithm. 
        /// </summary>
        private const uint SGenerator = 0xEDB88320;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the Crc32 class.
        /// </summary>
        static Crc32()
        {
            // Constructs the checksum lookup table. Used to optimize the checksum.
            _mChecksumTable = Enumerable.Range(0, 256).Select(i =>
                                                              {
                                                                  var tableEntry = (uint)i;
                                                                  for (var j = 0; j < 8; ++j)
                                                                  {
                                                                      tableEntry = ((tableEntry & 1) != 0)
                                                                                       ? (SGenerator ^ (tableEntry >> 1))
                                                                                       : (tableEntry >> 1);
                                                                  }
                                                                  return tableEntry;
                                                              }).ToArray();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Calculates the checksum of the byte stream.
        /// </summary>
        /// <param name="byteStream">The byte stream to calculate the checksum for.</param>
        /// <returns>A 32-bit reversed checksum.</returns>
        internal static uint Get<T>(IEnumerable<T> byteStream)
        {
            // Initialize checksumRegister to 0xFFFFFFFF and calculate the checksum.
            return ~byteStream.Aggregate(0xFFFFFFFF, (checksumRegister, currentByte) =>
                                                         (_mChecksumTable[(checksumRegister & 0xFF) ^ Convert.ToByte(currentByte)] ^ (checksumRegister >> 8)));
        }
        #endregion

        #region Fields
        /// <summary>
        /// Contains a cache of calculated checksum chunks.
        /// </summary>
        private static readonly uint[] _mChecksumTable;

        #endregion
    }
}