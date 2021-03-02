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
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.AvroToJson
{
    internal partial class Resolver
    {
        private readonly Schema.Schema _readerSchema;

        internal Resolver(Schema.Schema readerSchema)
        {
            _readerSchema = readerSchema;
        }

        internal object Resolve(IReader reader)
        {
            var result = Resolve(_readerSchema, reader);
            return result;
        }

        internal object Resolve(Schema.Schema readerSchema, IReader d)
        {
            switch (readerSchema.Tag)
            {
                case Schema.Schema.Type.Null:
                    return null;
                case Schema.Schema.Type.Boolean:
                    return d.ReadBoolean();
                case Schema.Schema.Type.Int:
                    return d.ReadInt();
                case Schema.Schema.Type.Long:
                    return ResolveLong(d);
                case Schema.Schema.Type.Float:
                    return d.ReadFloat();
                case Schema.Schema.Type.Double:
                    return d.ReadDouble();
                case Schema.Schema.Type.String:
                    return ResolveString(d);
                case Schema.Schema.Type.Bytes:
                    return d.ReadBytes();
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    return ResolveRecord((RecordSchema)readerSchema, d);
                case Schema.Schema.Type.Enumeration:
                    return ResolveEnum((EnumSchema)readerSchema, d);
                case Schema.Schema.Type.Fixed:
                    return ResolveFixed((FixedSchema)readerSchema, d);
                case Schema.Schema.Type.Array:
                    return ResolveArray(readerSchema, d);
                case Schema.Schema.Type.Map:
                    return ResolveMap((MapSchema)readerSchema, d);
                case Schema.Schema.Type.Union:
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

            foreach (Field rf in readerSchema.Fields)
            {
                string name = rf.Name;
                object value = Resolve(rf.Schema, dec);

                result.Add(name, value);
            }

            return result;
        }

        protected virtual object ResolveFixed(FixedSchema readerSchema, IReader d)
        {
            Fixed ru = new Fixed(readerSchema);
            byte[] bb = ((Fixed)ru).Value;
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

        internal object ResolveArray(Schema.Schema readerSchema, IReader d)
        {
            if (readerSchema.Tag == Schema.Schema.Type.Array)
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
            Schema.Schema ws = readerSchema[index];
            return Resolve(FindBranch(readerSchema, ws), d);
        }

        protected static Schema.Schema FindBranch(UnionSchema us, Schema.Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }
    }
}