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
using V2.Generic;
using V2.Schema;
using Encoder = V2.IO.Encoder;

namespace V2.Specific
{
    /// <summary>
    /// PreresolvingDatumWriter for writing data from ISpecificRecord classes.
    /// </summary>
    /// <see cref="PreresolvingDatumWriter{T}">For more information about performance considerations for choosing this implementation</see>
    public class SpecificDatumWriter : PreresolvingDatumWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificDatumWriter{T}"/> class.
        /// </summary>
        /// <param name="schema">Schema to use when writing data.</param>
        public SpecificDatumWriter(Schema.Schema schema) : base(schema, new SpecificArrayAccess(), new DictionaryMapAccess())
        {
        }

        /// <inheritdoc/>
        protected override void WriteRecordFields(object recordObj, RecordFieldWriter[] writers, Encoder encoder)
        {
//            var record = (ISpecificRecord) recordObj;
            for (int i = 0; i < writers.Length; i++)
            {
                var writer = writers[i];

                //                writer.WriteField(record.Get(writer.Field.Pos), encoder);




                string name = writer.Field.Aliases?[0] ?? writer.Field.Name;
          
                object value = recordObj.GetType().GetProperty(name)?.GetValue(recordObj, null);
                if (value == null)
                {
                    value = recordObj.GetType().GetField(name)?.GetValue(recordObj);
                }

                if (value != null)
                {
                    writer.WriteField(value, encoder);
                }
            }
        }

        /// <inheritdoc/>
        protected override void EnsureRecordObject(RecordSchema recordSchema, object value)
        {
//            if (!(value is ISpecificRecord))
//                throw new AvroTypeException("Record object is not derived from ISpecificRecord");
        }

        /// <inheritdoc/>
        protected override void WriteField(object record, string fieldName, int fieldPos, WriteItem writer, Encoder encoder)
        {
            writer(((ISpecificRecord)record).Get(fieldPos), encoder);
        }

        /// <inheritdoc/>
        protected override WriteItem ResolveEnum(EnumSchema es)
        {
            var type = ObjectCreator.Instance.GetType(es);

            var enumNames = Enum.GetNames(type);
            var translator = new int[enumNames.Length];
            for(int i = 0; i < enumNames.Length; i++)
            {
                if(es.Contains(enumNames[i]))
                {
                    translator[i] = es.Ordinal(enumNames[i]);
                }
                else
                {
                    translator[i] = -1;
                }
            }

            return (v,e) =>
                       {
                           if(v == null)
                                throw new AvroTypeException("value is null in SpecificDefaultWriter.WriteEnum");
                           if(v.GetType() == type)
                           {
                               int translated = translator[(int)v];
                               if (translated == -1)
                               {
                                   throw new AvroTypeException("Unknown enum value:" + v.ToString());
                               }
                               else
                               {
                                   e.WriteEnum(translated);
                               }
                           }
                           else
                           {
                               e.WriteEnum(es.Ordinal(v.ToString()));
                           }
                       };
        }

        /// <inheritdoc/>
        protected override void WriteFixed(FixedSchema schema, object value, Encoder encoder)
        {
            var fixedrec = value as SpecificFixed;
            if (fixedrec == null)
                throw new AvroTypeException("Fixed object is not derived from SpecificFixed");

            encoder.WriteFixed(fixedrec.Value);
        }

        /// <inheritdoc/>
        protected override bool UnionBranchMatches( Schema.Schema sc, object obj )
        {
            if (obj == null && sc.Tag != V2.Schema.Schema.Type.Null) return false;
            switch (sc.Tag)
            {
                case V2.Schema.Schema.Type.Null:
                    return obj == null;
                case V2.Schema.Schema.Type.Boolean:
                    return obj is bool;
                case V2.Schema.Schema.Type.Int:
                    return obj is int;
                case V2.Schema.Schema.Type.Long:
                    return obj is long;
                case V2.Schema.Schema.Type.Float:
                    return obj is float;
                case V2.Schema.Schema.Type.Double:
                    return obj is double;
                case V2.Schema.Schema.Type.Bytes:
                    return obj is byte[];
                case V2.Schema.Schema.Type.String:
                    return obj is string;
                case V2.Schema.Schema.Type.Error:
                case V2.Schema.Schema.Type.Record:
                    return obj is ISpecificRecord &&
                           ((obj as ISpecificRecord).Schema as RecordSchema).SchemaName.Equals((sc as RecordSchema).SchemaName);
                case V2.Schema.Schema.Type.Enumeration:
                    return obj.GetType().IsEnum && (sc as EnumSchema).Symbols.Contains(obj.ToString());
                case V2.Schema.Schema.Type.Array:
                    return obj is System.Collections.IList;
                case V2.Schema.Schema.Type.Map:
                    return obj is System.Collections.IDictionary;
                case V2.Schema.Schema.Type.Union:
                    return false;   // Union directly within another union not allowed!
                case V2.Schema.Schema.Type.Fixed:
                    return obj is SpecificFixed &&
                           ((obj as SpecificFixed).Schema as FixedSchema).SchemaName.Equals((sc as FixedSchema).SchemaName);
                case V2.Schema.Schema.Type.Logical:
                    return (sc as LogicalSchema).LogicalType.IsInstanceOfLogicalType(obj);
                default:
                    throw new AvroException("Unknown schema type: " + sc.Tag);
            }
        }

        class SpecificArrayAccess : ArrayAccess
        {
            public void EnsureArrayObject( object value )
            {
                if( !( value is System.Collections.IList ) )
                {
                    throw new AvroTypeException( "Array does not implement non-generic IList" );
                }
            }

            public long GetArrayLength(object value)
            {
                return ((IList)value).Count;
            }

            public void WriteArrayValues(object array, WriteItem valueWriter, Encoder encoder)
            {
                var list = (IList) array;
                for (int i = 0; i < list.Count; i++ )
                {
                    valueWriter(list[i], encoder);
                }
            }
        }
    }
}
