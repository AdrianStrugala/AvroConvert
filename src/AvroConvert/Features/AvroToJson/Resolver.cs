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
using System.Collections.Generic;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.AvroToJson
{
    internal partial class Resolver
    {
        private readonly TypeSchema _readerSchema;

        internal Resolver(TypeSchema readerSchema)
        {
            _readerSchema = readerSchema;
        }

        internal object Resolve(IReader reader)
        {
            var result = Resolve(_readerSchema, reader);
            return result;
        }

        internal object Resolve(TypeSchema readerSchema, IReader d)
        {
            switch (readerSchema.Type)
            {
                case AvroType.Null:
                    return null;
                case AvroType.Boolean:
                    return d.ReadBoolean();
                case AvroType.Int:
                    return d.ReadInt();
                case AvroType.Long:
                    return ResolveLong(d);
                case AvroType.Float:
                    return d.ReadFloat();
                case AvroType.Double:
                    return d.ReadDouble();
                case AvroType.String:
                    return ResolveString(d);
                case AvroType.Bytes:
                    return d.ReadBytes();
                case AvroType.Error:
                case AvroType.Record:
                    return ResolveRecord((RecordSchema)readerSchema, d);
                case AvroType.Enum:
                    return ResolveEnum((EnumSchema)readerSchema, d);
                case AvroType.Fixed:
                    return ResolveFixed((FixedSchema)readerSchema, d);
                case AvroType.Array:
                    return ResolveArray(readerSchema, d);
                case AvroType.Map:
                    return ResolveMap((MapSchema)readerSchema, d);
                case AvroType.Union:
                    return ResolveUnion((UnionSchema)readerSchema, d);
                default:
                    throw new AvroException("Unknown schema type: " + readerSchema);
            }

        }

        protected object ResolveLong(IReader reader)
        {
            long value = reader.ReadLong();

            return value;
        }

        protected object ResolveString(IReader reader)
        {
            var value = reader.ReadString();
            return value;
        }

        protected virtual Dictionary<string, object> ResolveRecord(RecordSchema readerSchema, IReader dec)
        {
            var result = new Dictionary<string, object>();

            foreach (var rf in readerSchema.Fields)
            {
                string name = rf.Name;
                object value = Resolve(rf.TypeSchema, dec);

                result.Add(name, value);
            }

            return result;
        }

        protected virtual object ResolveFixed(FixedSchema readerSchema, IReader d)
        {
            FixedModel ru = new FixedModel(readerSchema);
            byte[] bb = ((FixedModel)ru).Value;
            d.ReadFixed(bb);
            return ru.Value;
        }

        protected virtual object ResolveEnum(EnumSchema readerSchema, IReader d)
        {
            int position = d.ReadEnum();
            string value = readerSchema.Symbols[position];
            return value;
        }

        protected virtual object ResolveMap(MapSchema readerSchema, IReader d)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            for (int n = (int)d.ReadMapStart(); n != 0; n = (int)d.ReadMapNext())
            {
                for (int j = 0; j < n; j++)
                {
                    string k = d.ReadString();
                    result.Add(k, Resolve(readerSchema.ValueSchema, d));
                }
            }

            return result;
        }

        internal object ResolveArray(TypeSchema readerSchema, IReader d)
        {
            if (readerSchema.Type == AvroType.Array)
            {
                readerSchema = ((ArraySchema)readerSchema).ItemSchema;
            }

            object[] result = new object[0];
            int i = 0;

            for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
            {
                if (result.Length < i + n)
                {
                    Array.Resize(ref result, i + n);
                }

                for (int j = 0; j < n; j++, i++)
                {
                    result[i] = Resolve(readerSchema, d);
                }
            }

            return result;
        }

        protected virtual object ResolveUnion(UnionSchema readerSchema, IReader d)
        {
            int index = d.ReadUnionIndex();
            TypeSchema ws = readerSchema.Schemas[index];
            return Resolve(FindBranch(readerSchema, ws), d);
        }

        protected static TypeSchema FindBranch(UnionSchema us, TypeSchema schema)
        {
            var resultSchema = us.Schemas.FirstOrDefault(s => s.Type == schema.Type);

            if (resultSchema == null)
            {
                throw new AvroException("No matching schema for " + schema + " in " + us);
            }

            return resultSchema;
        }
    }
}