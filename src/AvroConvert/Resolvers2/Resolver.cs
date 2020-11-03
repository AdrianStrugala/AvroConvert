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
using System.Linq.Expressions;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.Skip;

namespace SolTechnology.Avro.Resolvers2
{
    internal partial class Resolver<T>
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
                return (T) ResolveArray<T>(
                    _writerSchema,
                    _readerSchema,
                    reader,  itemsCount);
            }

            var result = Resolve<T>(_writerSchema, _readerSchema, reader);
            return (T)result;
        }

        Action<object> Resolve(
            Schema.Schema writerSchema,
            Schema.Schema readerSchema,
            IReader d,
            Type type)
        {
            var genericCreateMethod = typeof(Resolver).GetMethod("Resolve");
            var specificCreateMethod = genericCreateMethod.MakeGenericMethod(type);
            return specificCreateMethod.Invoke(null, null);
        }



        internal object Resolve<T>(
            Schema.Schema writerSchema,
            Schema.Schema readerSchema,
            IReader d)
        {
            try
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
                        return d.ReadInt();
                    case Schema.Schema.Type.Long:
                        return ResolveLong<T>(d);
                    case Schema.Schema.Type.Float:
                        return d.ReadFloat();
                    case Schema.Schema.Type.Double:
                        return d.ReadDouble();
                    case Schema.Schema.Type.String:
                        return ResolveString<T>(d);
                    case Schema.Schema.Type.Bytes:
                        return d.ReadBytes();
                    case Schema.Schema.Type.Error:
                    case Schema.Schema.Type.Record:
                        return ResolveRecord<T>((RecordSchema)writerSchema, (RecordSchema)readerSchema, d);
                    case Schema.Schema.Type.Enumeration:
                        return ResolveEnum<T>((EnumSchema)writerSchema, readerSchema, d);
                    case Schema.Schema.Type.Fixed:
                        return ResolveFixed<T>((FixedSchema)writerSchema, readerSchema, d);
                    case Schema.Schema.Type.Array:
                        return ResolveArray<T>(writerSchema, readerSchema, d);
                    case Schema.Schema.Type.Map:
                        return ResolveMap<T>((MapSchema)writerSchema, readerSchema, d);
                    case Schema.Schema.Type.Union:
                        return ResolveUnion<T>((UnionSchema)writerSchema, readerSchema, d);
                    default:
                        throw new AvroException("Unknown schema type: " + writerSchema);
                }
            }
            catch (Exception e)
            {
                throw new AvroTypeMismatchException($"Unable to deserialize [{writerSchema.Name}] of schema [{writerSchema.Tag}] to the target type [{typeof(T)}]", e);
            }
        }

        protected virtual object ResolveEnum<T>(EnumSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            int position = d.ReadEnum();
            string value = writerSchema.Symbols[position];
            return Enum.Parse(typeof(T), value);
        }

        protected virtual object ResolveMap<T>(MapSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            var type = typeof(T);
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