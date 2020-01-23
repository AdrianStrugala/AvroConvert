#region license
/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/** Modifications copyright(C) 2020 Adrian Strugała **/
#endregion

using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Models
{
    internal class Enum
    {
        internal EnumSchema Schema { get; }
        private string _value;
        internal string Value
        {
            get => _value;
            set
            {
                if (!Schema.Contains(value)) throw new AvroException("Unknown value for enum: " + value + "(" + Schema + ")");
                _value = value;
            }
        }

        internal Enum(EnumSchema schema, string value)
        {
            this.Schema = schema;
            this.Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            return (obj != null && obj is Enum @enum) && Value.Equals(@enum.Value);
        }

        public override int GetHashCode()
        {
            return 17 * Value.GetHashCode();
        }

        public override string ToString()
        {
            return "Schema: " + Schema + ", value: " + Value;
        }
    }
}
