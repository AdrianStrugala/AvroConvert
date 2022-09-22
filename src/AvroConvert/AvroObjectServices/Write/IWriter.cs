using System.IO;

namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal interface IWriter
    {
        /// <summary>
        /// null is written as zero bytes
        /// </summary>
        void WriteNull();

        /// <summary>
        /// true is written as 1 and false 0.
        /// </summary>
        /// <param name="b">Boolean value to write</param>
        void WriteBoolean(bool b);

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        /// <param name="datum"></param>
        void WriteInt(int value);

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        /// <param name="datum"></param>
        void WriteLong(long value);

        /// <summary>
        /// A float is written as 4 bytes.
        /// The float is converted into a 32-bit integer using a method equivalent to
        /// Java's floatToIntBits and then encoded in little-endian format.
        /// </summary>
        /// <param name="value"></param>
        void WriteFloat(float value);

        /// <summary>
        ///A double is written as 8 bytes.
        ///The double is converted into a 64-bit integer using a method equivalent to
        ///Java's doubleToLongBits and then encoded in little-endian format.
        /// </summary>
        /// <param name="value"></param>
        void WriteDouble(double value);

        /// <summary>
        /// Bytes are encoded as a long followed by that many bytes of data.
        /// </summary>
        /// <param name="value"></param>
        /// 
        void WriteBytes(byte[] value);

        /// <summary>
        /// A string is encoded as a long followed by
        /// that many bytes of UTF-8 encoded character data.
        /// </summary>
        /// <param name="value"></param>
        void WriteString(string value);

        void WriteEnum(int value);
        void StartItem();
        void WriteItemCount(long value);
        void WriteArrayStart();
        void WriteArrayEnd();
        void WriteMapStart();
        void WriteMapEnd();
        void WriteUnionIndex(int value);
        void WriteFixed(byte[] data);
        void WriteFixed(byte[] data, int start, int len);
        void WriteBytesRaw(byte[] bytes);
        void WriteStream(MemoryStream stream);
    }
}