using System;
using System.Collections;
using System.Collections.Generic;
using AvroConvert.Models;
using AvroConvert.Schema;
using AvroConvert.Skip;
using Enum = AvroConvert.Models.Enum;

namespace AvroConvert.Read
{
    public class Resolver
    {
        private readonly Skipper _skipper;

        public Schema.Schema ReaderSchema { get; }
        public Schema.Schema WriterSchema { get; }

        public Resolver(Schema.Schema writerSchema, Schema.Schema readerSchema)
        {
            ReaderSchema = readerSchema;
            WriterSchema = writerSchema;

            _skipper = new Skipper();
        }

        public object Resolve(IReader reader)
        {
            var result = Resolve(WriterSchema, ReaderSchema, reader);
            return result;
        }

        public object Resolve(Schema.Schema writerSchema, Schema.Schema readerSchema, IReader d)
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
                    return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, d);
                case Schema.Schema.Type.Enumeration:
                    return ResolveEnum((EnumSchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Fixed:
                    return ResolveFixed((FixedSchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Array:
                    return ResolveArray((ArraySchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Map:
                    return ResolveMap((MapSchema)writerSchema, readerSchema, d);
                case Schema.Schema.Type.Union:
                    return ResolveUnion((UnionSchema)writerSchema, readerSchema, d);
                default:
                    throw new AvroException("Unknown schema type: " + writerSchema);
            }
        }

        protected virtual IDictionary<string, object> ResolveRecord(RecordSchema writerSchema, RecordSchema readerSchema, IReader dec)
        {
            Record result = new Record(readerSchema);
            foreach (Field wf in writerSchema)
            {
                if (readerSchema.Contains(wf.Name))
                {
                    Field rf = readerSchema.GetField(wf.Name);
                    object value = Resolve(wf.Schema, rf.Schema, dec) ?? wf.DefaultValue?.ToObject(typeof(object));

                    AddField(result, rf.aliases?[0] ?? wf.Name, rf.Pos, value);
                }
                else
                    _skipper.Skip(wf.Schema, dec);

            }
            return result.Contents;
        }


        protected virtual void AddField(Record record, string fieldName, int fieldPos, object fieldValue)
        {
            record.Contents[fieldName] = fieldValue;
        }


        protected virtual object ResolveEnum(EnumSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            EnumSchema es = readerSchema as EnumSchema;
            return new Enum(es, writerSchema[d.ReadEnum()]);
        }


        protected virtual object ResolveArray(ArraySchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            ArraySchema rs = (ArraySchema)readerSchema;
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
                    result[i] = Resolve(writerSchema.ItemSchema, rs.ItemSchema, d);
                }
            }

            if (result[0] is IDictionary)
            {
                return ResolveDictionaryFromArray(result);
            }

            return result;
        }

        protected object ResolveDictionaryFromArray(object[] array)
        {
            //HACK for reading c# dictionaries, which are not avro maps

            Dictionary<object, object> resultDictionary = new Dictionary<object, object>();
            foreach (Dictionary<string, object> keyValue in array)
            {
                if (!keyValue.ContainsKey("Key") || !keyValue.ContainsKey("Value"))
                {
                    return array;
                }

                resultDictionary.Add(keyValue["Key"], keyValue["Value"]);
            }

            return resultDictionary;
        }

        protected virtual object ResolveMap(MapSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            MapSchema rs = (MapSchema)readerSchema;
            Dictionary<string, object> result = new Dictionary<string, object>();
            for (int n = (int)d.ReadMapStart(); n != 0; n = (int)d.ReadMapNext())
            {
                for (int j = 0; j < n; j++)
                {
                    string k = d.ReadString();
                    result.Add(k, Resolve(writerSchema.ValueSchema, rs.ValueSchema, d));
                }
            }

            return result;
        }

        protected virtual object ResolveUnion(UnionSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            int index = d.ReadUnionIndex();
            Schema.Schema ws = writerSchema[index];

            if (readerSchema is UnionSchema unionSchema)
                readerSchema = FindBranch(unionSchema, ws);
            else
            if (!readerSchema.CanRead(ws))
                throw new AvroException("Schema mismatch. Reader: " + ReaderSchema + ", writer: " + WriterSchema);

            return Resolve(ws, readerSchema, d);
        }

        protected virtual object ResolveFixed(FixedSchema writerSchema, Schema.Schema readerSchema, IReader d)
        {
            FixedSchema rs = (FixedSchema)readerSchema;
            if (rs.Size != writerSchema.Size)
            {
                throw new AvroException("Size mismatch between reader and writer fixed schemas. Encoder: " + writerSchema +
                                        ", reader: " + readerSchema);
            }

            object ru = new Fixed(rs);
            byte[] bb = ((Fixed)ru).Value;
            d.ReadFixed(bb);
            return ru;
        }

        protected static Schema.Schema FindBranch(UnionSchema us, Schema.Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }
    }
}