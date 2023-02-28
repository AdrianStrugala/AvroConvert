#region license
/**Copyright (c) 2021 Adrian Strugała
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        internal object ResolveInt(Type readType, IReader reader)
        {
            int value = reader.ReadInt();

            return readType == typeof(int) ? value : ConvertValue(readType, value);
        }

        private object ConvertValue(Type readType, object value)
        {
            switch (readType)
            {
                case not null when readType == typeof(int):
                case not null when readType == typeof(int?):
                    return Convert.ToInt32(value);

                case not null when readType == typeof(uint):
                case not null when readType == typeof(uint?):
                    return Convert.ToUInt32(value);

                case not null when readType == typeof(long):
                case not null when readType == typeof(long?):
                    return Convert.ToInt64(value);

                case not null when readType == typeof(ulong?):
                case not null when readType == typeof(ulong):
                    return Convert.ToUInt64(value);

                case not null when readType == typeof(short):
                case not null when readType == typeof(short?):
                    return Convert.ToInt16(value);

                case not null when readType == typeof(ushort):
                case not null when readType == typeof(ushort?):
                    return Convert.ToUInt16(value);

                case not null when readType == typeof(decimal):
                case not null when readType == typeof(decimal?):
                    return Convert.ToDecimal(value);

                case not null when readType == typeof(float):
                case not null when readType == typeof(float?):
                    return Convert.ToSingle(value);

                case not null when readType == typeof(double):
                case not null when readType == typeof(double?):
                    return Convert.ToDouble(value);

                case not null when readType == typeof(char?):
                case not null when readType == typeof(char):
                    return Convert.ToChar(value);

                case not null when readType == typeof(byte):
                case not null when readType == typeof(byte?):
                    return Convert.ToByte(value);

                case not null when readType == typeof(sbyte):
                case not null when readType == typeof(sbyte?):
                    return Convert.ToSByte(value);
            }

            return value;
        }
    }
}
