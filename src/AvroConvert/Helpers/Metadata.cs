namespace EhwarSoft.AvroConvert.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Constants;
    using Exceptions;

    public class Metadata
    {
        private readonly Dictionary<string, byte[]> _value;

        public Metadata()
        {
            _value = new Dictionary<string, byte[]>();
        }

        public void Add(string key, long value)
        {
            try
            {
                Add(key, System.Text.Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(e.Message, e);
            }
        }

        public void Add(string key, string value)
        {
            try
            {
                Add(key, System.Text.Encoding.UTF8.GetBytes(value));
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(e.Message, e);
            }
        }

        public void Add(string key, byte[] value)
        {
            if (IsReservedKey(key))
            {
                throw new AvroRuntimeException("Cannot set reserved meta key: " + key);
            }
            _value.Add(key, value);
        }

        public void ForceAdd(string key, string value)
        {
            _value.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public int GetSize()
        {
            return _value.Count;
        }

        public Dictionary<string, byte[]> GetValue()
        {
            return _value;
        }

        public bool IsReservedKey(string key)
        {
            return key.StartsWith(DataFileConstants.MetaDataReserved);
        }
    }
}
