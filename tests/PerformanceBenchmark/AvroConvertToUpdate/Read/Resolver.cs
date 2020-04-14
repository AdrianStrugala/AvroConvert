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
using System.Reflection;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Exceptions;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Schema;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Skip;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Read
{
    internal partial class Resolver
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

        internal T Resolve<T>(IReader reader, long itemsCount = 0)
        {
            if (itemsCount > 1)
            {
                return (T)ResolveArray(
                        _writerSchema,
                        _readerSchema,
                        reader, typeof(T), itemsCount);
            }

            var result = Resolve(_writerSchema, _readerSchema, reader, typeof(T));
            return (T)result;
        }

        internal object Resolve(
            Schema.Schema writerSchema,
            Schema.Schema readerSchema,
            IReader d,
            Type type)
        {
            if (readerSchema.Tag == Schema.Schema.Type.Union && writerSchema.Tag != Schema.Schema.Type.Union)
            {
                readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
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
                        return ResolveLong(type, d);
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
                    return ResolveString(type, d);
                case Schema.Schema.Type.Bytes:
                    return d.ReadBytes();
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, d, type);
                case Schema.Schema.Type.Enumeration:
                    return ResolveEnum((EnumSchema)writerSchema, readerSchema, d, type);
                case Schema.Schema.Type.Fixed:
                    return ResolveFixed((FixedSchema)writerSchema, readerSchema, d, type);
                case Schema.Schema.Type.Array:
                    return ResolveArray(
                    writerSchema,
                    readerSchema,
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

        protected virtual object ResolveEnum(EnumSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            int position = d.ReadEnum();
            string value = writerSchema.Symbols[position];
            return Enum.Parse(type, value);
        }

        protected virtual object ResolveMap(MapSchema writerSchema, Schema.Schema readerSchema, IReader d, Type type)
        {
            var containingTypes = type.GetGenericArguments();
            dynamic result = Activator.CreateInstance(type);

            Schema.Schema stringSchema = PrimitiveSchema.NewInstance("string");

            MapSchema rs = (MapSchema)readerSchema;
            for (int n = (int)d.ReadMapStart(); n != 0; n = (int)d.ReadMapNext())
            {
                for (int j = 0; j < n; j++)
                {
                    dynamic key = Resolve(stringSchema, stringSchema, d, containingTypes[0]);
                    dynamic value = Resolve(writerSchema.ValueSchema, rs.ValueSchema, d, containingTypes[1]);
                    result.Add(key, value);
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

        protected static Schema.Schema FindBranch(UnionSchema us, Schema.Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }
    }
}