/*  
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Reflection;
using V2.Schema;

namespace V2.Reflect
{
    internal class DotnetProperty
    {
        private PropertyInfo _property;

        public IAvroFieldConverter Converter { get; set; }

        private bool IsPropertyCompatible(Schema.Schema.Type schemaTag)
        {
            Type propType;

            if (Converter == null)
            {
                propType = _property.PropertyType;
            }
            else
            {
                propType = Converter.GetAvroType();
            }

            switch (schemaTag)
            {
                case Schema.Schema.Type.Null:
                    return (Nullable.GetUnderlyingType(propType) != null) || (!propType.IsValueType);
                case Schema.Schema.Type.Boolean:
                    return propType == typeof(bool);
                case Schema.Schema.Type.Int:
                    return propType == typeof(int);
                case Schema.Schema.Type.Long:
                    return propType == typeof(long);
                case Schema.Schema.Type.Float:
                    return propType == typeof(float);
                case Schema.Schema.Type.Double:
                    return propType == typeof(double);
                case Schema.Schema.Type.Bytes:
                    return propType == typeof(byte[]);
                case Schema.Schema.Type.String:
                    return typeof(string).IsAssignableFrom(propType);
                case Schema.Schema.Type.Record:
                    //TODO: this probably should work for struct too
                    return propType.IsClass;
                case Schema.Schema.Type.Enumeration:
                    return propType.IsEnum;
                case Schema.Schema.Type.Array:
                    return typeof(IEnumerable).IsAssignableFrom(propType);
                case Schema.Schema.Type.Map:
                    return typeof(IDictionary).IsAssignableFrom(propType);
                case Schema.Schema.Type.Union:
                    return true;
                case Schema.Schema.Type.Fixed:
                    return propType == typeof(byte[]);
                case Schema.Schema.Type.Error:
                    return propType.IsClass;
            }

            return false;
        }

        public DotnetProperty(PropertyInfo property, Schema.Schema.Type schemaTag,  IAvroFieldConverter converter, ClassCache cache)
        {
            _property = property;
            Converter = converter;

            if (!IsPropertyCompatible(schemaTag))
            {
                if (Converter == null)
                {
                    var c = cache.GetDefaultConverter(schemaTag, _property.PropertyType);
                    if (c != null)
                    {
                        Converter = c;
                        return;
                    }
                }

                throw new AvroException($"Property {property.Name} in object {property.DeclaringType} isn't compatible with Avro schema type {schemaTag}");
            }
        }

        public DotnetProperty(PropertyInfo property, Schema.Schema.Type schemaTag, ClassCache cache)
            : this(property, schemaTag, null, cache)
        {
        }

        public virtual Type GetPropertyType()
        {
            if (Converter != null)
            {
                return Converter.GetAvroType();
            }

            return _property.PropertyType;
        }

        public virtual object GetValue(object o, Schema.Schema s)
        {
            if (Converter != null)
            {
                return Converter.ToAvroType(_property.GetValue(o), s);
            }

            return _property.GetValue(o);
        }

        public virtual void SetValue(object o, object v, Schema.Schema s)
        {
            if (Converter != null)
            {
                _property.SetValue(o, Converter.FromAvroType(v, s));
            }
            else
            {
                _property.SetValue(o, v);
            }
        }
    }
}
