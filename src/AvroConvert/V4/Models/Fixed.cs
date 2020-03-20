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

using System;
using System.Linq;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.V4.Exceptions;

namespace SolTechnology.Avro.V4.Models
{
    internal class Fixed
    {
        protected readonly byte[] _value;
        private FixedSchema _schema;

        internal FixedSchema Schema
        {
            get => _schema;

            set
            {
                if (!(value is FixedSchema))
                    throw new AvroException("Schema " + value.Name + " in set is not FixedSchema");

                if ((value as FixedSchema).Size != _value.Length)
                    throw new AvroException("Schema " + value.Name + " Size " + (value as FixedSchema).Size + "is not equal to bytes length " + this._value.Length);

                _schema = value;
            }
        }

        internal Fixed(FixedSchema schema)
        {
            _value = new byte[schema.Size];
            Schema = schema;
        }

        internal Fixed(FixedSchema schema, byte[] value)
        {
            this._value = new byte[schema.Size];
            Schema = schema;
            Value = value;
        }

        protected Fixed(uint size)
        {
            this._value = new byte[size];
        }

        internal byte[] Value
        {
            get { return this._value; }
            set
            {
                if (value.Length == this._value.Length)
                {
                    Array.Copy(value, this._value, value.Length);
                    return;
                }
                throw new AvroException("Invalid length for fixed: " + value.Length + ", (" + Schema + ")");
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || !(obj is Fixed that)) return false;
            if (!that.Schema.Equals(Schema)) return false;
            return !_value.Where((t, i) => _value[i] != that._value[i]).Any();
        }

        public override int GetHashCode()
        {
            return Schema.GetHashCode() + _value.Sum(b => 23 * b);
        }
    }
}
