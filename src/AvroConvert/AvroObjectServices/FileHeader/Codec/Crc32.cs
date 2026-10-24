using System;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    /// <summary>
    /// CRC-32 (IEEE 802.3, reflected polynomial 0xEDB88320) as required by the Avro snappy codec.
    /// </summary>
    internal static class Crc32
    {
        private const uint Polynomial = 0xEDB88320;
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    entry = (entry & 1) != 0 ? Polynomial ^ (entry >> 1) : entry >> 1;
                }
                table[i] = entry;
            }
            return table;
        }

        internal static uint Get(ReadOnlySpan<byte> data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return ~crc;
        }
    }
}
