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
        internal object ResolveDouble(Type readType, IReader reader)
        {
            double value = reader.ReadDouble();

            if (readType != typeof(double))
            {
                if (readType == typeof(int) || readType == typeof(int?))
                {
                    return Convert.ToInt32(value);
                }

                if (readType == typeof(short) || readType == typeof(short?))
                {
                    return Convert.ToInt16(value);
                }

                if (readType == typeof(decimal) || readType == typeof(decimal?))
                {
                    return Convert.ToDecimal(value);
                }


                if (readType == typeof(long) || readType == typeof(long?))
                {
                    return Convert.ToInt64(value);
                }

                if (readType == typeof(float) || readType == typeof(float?))
                {
                    return Convert.ToSingle(value);
                }
            }

            return value;
        }
    }
}
