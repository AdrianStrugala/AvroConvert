#region license
/**Copyright (c) 2020 Adrian Struga³a
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Extensions;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.Skip;

namespace SolTechnology.Avro.Read
{
    internal class Resolver
    {
        private readonly Skipper _skipper;
        private readonly Schema.Schema _readerSchema;
        private readonly Schema.Schema _writerSchema;

        internal Resolver(Schema.Schema writerSchema, Schema.Schema readerSchema)
        {
            _readerSchema = readerSchema;
            _writerSchema = writerSchema;

            _skipper = new Skipper();
        }

        internal T Resolve<T>(IReader reader, long itemsCount = 1)
        {
            if (itemsCount > 1)
            {
                return (T)ResolveArray(_writerSchema, ((ArraySchema)_readerSchema).ItemSchema, reader, typeof(T), itemsCount);
            }

            var result = Resolve(_writerSchema, _readerSchema, reader, typeof(T));
            return (T)result;
        }

        internal object Resolve(Schema.Schema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            if (readerSchema.Tag == Schema.Schema.Type.Union && writerSchema.Tag != Schema.Schema.Type.Union)
            {
                readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
            }

            //Types not supported by Avro schema
            switch (type)
            {
                case var _ when type == typeof(decimal):
                    return decimal.Parse(d.ReadString());

                case var _ when type == typeof(Guid):
                    return new Guid(ResolveFixed((FixedSchema)writerSchema, readerSchema, d));

                case var _ when type == typeof(DateTimeOffset):
                    return DateTimeOffset.Parse(d.ReadString());

                case var _ when type == typeof(DateTime):
                    return ResolveDateTime(d);

                default:
                    break;
            }

            switch (writerSchema.Tag)
            {
                case Schema.Schema.Type.Null:
                    return null;
                case Schema.Schema.Type.Boolean:
                    return d.ReadBoolean();
                case Schema.Schema.Type.Int:
                    {
                        int i = d.ReadInt();
                        switch (readerSchema.Tag)
                        {
                            case Schema.Schema.Type.Long:
                                return (long)i;
                            case Schema.Schema.Type.Float:
                                return (float)i;
                            case Schema.Schema.Type.Double:
                                return (double)i;
                            default:
                                return i;
                        }
                    }
                case Schema.Schema.Type.Long:
                    {
                        long l = d.ReadLong();
                        switch (readerSchema.Tag)
                        {
                            case Schema.Schema.Type.Float:
                                return (float)l;
                            case Schema.Schema.Type.Double:
                                return (double)l;
                            default:
                                return l;
                        }
                    }
                case Schema.Schema.Type.Float:
                    {
                        float f = d.ReadFloat();
                        switch (readerSchema.Tag)
                        {
                            case Schema.Schema.Type.Double:
                                return (double)f;
                            default:
                                return f;
                        }
                    }
                case Schema.Schema.Type.Double:
                    return d.ReadDouble();
                case Schema.Schema.Type.String:
                    return d.ReadString();
                case Schema.Schema.Type.Bytes:
                    return d.ReadBytes();
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, d, type);
                case Schema.Schema.Type.Enumeration:
                    return ResolveEnum((EnumSchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Fixed:
                    return ResolveFixed((FixedSchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Array:
                    return ResolveArray(
                    ((ArraySchema)writerSchema).ItemSchema,
                    ((ArraySchema)readerSchema).ItemSchema,
                    d, type);
                case Schema.Schema.Type.Map:
                    return ResolveMap((MapSchema)writerSchema, readerSchema, d, type);
                case Schema.Schema.Type.Union:
                    return ResolveUnion((UnionSchema)writerSchema, readerSchema, d, type);
                default:
                    throw new AvroException("Unknown schema type: " + writerSchema);
            }
        }

        protected virtual object ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec, Type type)
        {
            object result = Activator.CreateInstance(type);

            foreach (Field wf in writerSchema)
            {
                if (readerSchema.Contains(wf.Name))
                {
                    Field rf = readerSchema.GetField(wf.Name);
                    string name = rf.aliases?[0] ?? wf.Name;

                    PropertyInfo propertyInfo = result.GetType().GetProperty(name);
                    if (propertyInfo != null)
                    {
                        object value = Resolve(wf.Schema, rf.Schema, dec, propertyInfo.PropertyType) ?? wf.DefaultValue?.ToObject(typeof(object));
                        propertyInfo.SetValue(result, value, null);
                    }

                    FieldInfo fieldInfo = result.GetType().GetField(name);
                    if (fieldInfo != null)
                    {
                        object value = Resolve(wf.Schema, rf.Schema, dec, fieldInfo.FieldType) ?? wf.DefaultValue?.ToObject(typeof(object));
                        fieldInfo.SetValue(result, value);
                    }
                }
                else
                    _skipper.Skip(wf.Schema, dec);
            }

            return result;
        }


        protected virtual object ResolveEnum(EnumSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            int position = d.ReadEnum();

            return position;
        }

        protected virtual object ResolveDateTime(IReader d)
        {
            var dateTime = d.ReadLong();
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = new DateTime();
            result = result.AddTicks(unixEpoch.Ticks);
            result = result.AddSeconds(dateTime);

            return result;
        }


        protected virtual object ResolveArray(Schema.Schema writerSchema, Schema.Schema readerSchema, IReader d, Type type, long itemsCount = 0)
        {
            if (type.IsDictionary())
            {
                return ResolveDictionary((RecordSchema)writerSchema, (RecordSchema)readerSchema, d, type);
            }

            var containingType = type.GetEnumeratedType();
            var containingTypeArray = containingType.MakeArrayType();
            var resultType = typeof(List<>).MakeGenericType(containingType);
            var result = (IList)Activator.CreateInstance(resultType);

            int i = 0;
            if (itemsCount == 0)
            {
                for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
                {
                    for (int j = 0; j < n; j++, i++)
                    {
                        dynamic y = Resolve(writerSchema, readerSchema, d, containingType);
                        result.Add(y);
                    }
                }
            }
            else
            {
                for (int k = 0; k < itemsCount; k++)
                {
                    result.Add(Resolve(writerSchema, readerSchema, d, containingType));
                }
            }

            if (type.IsArray)
            {
                dynamic resultArray = Activator.CreateInstance(containingTypeArray, new object[] { result.Count });
                result.CopyTo(resultArray, 0);
                return resultArray;
            }

            if (type.IsList())
            {
                return result;
            }

            var hashSetType = typeof(HashSet<>).MakeGenericType(containingType);
            if (type == hashSetType)
            {
                dynamic resultHashSet = Activator.CreateInstance(hashSetType);
                foreach (dynamic item in result)
                {
                    resultHashSet.Add(item);
                }

                return resultHashSet;
            }

            return result;
        }

        protected object ResolveDictionary(RecordSchema writerSchema, RecordSchema readerSchema, IReader d, Type type)
        {
            var containingTypes = type.GetGenericArguments();
            dynamic resultDictionary = Activator.CreateInstance(type);

            for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
            {
                for (int j = 0; j < n; j++)
                {
                    dynamic key = Resolve(writerSchema.GetField("Key").Schema, readerSchema.GetField("Key").Schema, d, containingTypes[0]);
                    dynamic value = Resolve(writerSchema.GetField("Value").Schema, readerSchema.GetField("Value").Schema, d, containingTypes[1]);
                    resultDictionary.Add(key, value);
                }
            }
            return resultDictionary;
        }

        protected virtual object ResolveMap(MapSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            var containingTypes = type.GetGenericArguments();
            dynamic result = Activator.CreateInstance(type);

            MapSchema rs = (MapSchema)readerSchema;
            for (int n = (int)d.ReadMapStart(); n != 0; n = (int)d.ReadMapNext())
            {
                for (int j = 0; j < n; j++)
                {
                    string k = d.ReadString();
                    dynamic value = Resolve(writerSchema.ValueSchema, rs.ValueSchema, d, containingTypes[1]);
                    result.Add(k, value);
                }
            }

            return result;
        }

        protected virtual object ResolveUnion(UnionSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            int index = d.ReadUnionIndex();
            Schema.Schema ws = writerSchema[index];

            if (readerSchema is UnionSchema unionSchema)
                readerSchema = FindBranch(unionSchema, ws);
            else
            if (!readerSchema.CanRead(ws))
                throw new AvroException("Schema mismatch. Reader: " + _readerSchema + ", writer: " + _writerSchema);

            return Resolve(ws, readerSchema, d, type);
        }

        protected virtual byte[] ResolveFixed(FixedSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            FixedSchema rs = (FixedSchema)readerSchema;
            if (rs.Size != writerSchema.Size)
            {
                throw new AvroException("Size mismatch between reader and writer fixed schemas. Encoder: " + writerSchema +
                                        ", reader: " + readerSchema);
            }

            Fixed ru = new Fixed(rs);
            byte[] bb = ((Fixed)ru).Value;
            d.ReadFixed(bb);
            return ru.Value;
        }

        protected static Schema.Schema FindBranch(UnionSchema us, Schema.Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }
    }
}