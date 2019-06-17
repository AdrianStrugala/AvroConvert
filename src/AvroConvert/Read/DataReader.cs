namespace AvroConvert.Read
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Schema;
    using Enum = Models.Enum;

    public delegate T Reader<out T>();

    public sealed class DataReader
    {
        private readonly DefaultReader _reader;

        public DataReader(Schema writerSchema, Schema readerSchema)
            : this(new DefaultReader(writerSchema, readerSchema))
        {
        }

        public DataReader(DefaultReader reader)
        {
            _reader = reader;
        }

        public object Read(IReader d)
        {
            return _reader.Read(d);
        }
    }

    public class DefaultReader
    {
        public Schema ReaderSchema { get; }
        public Schema WriterSchema { get; }

        public DefaultReader(Schema writerSchema, Schema readerSchema)
        {
            ReaderSchema = readerSchema;
            WriterSchema = writerSchema;
        }

        public object Read(IReader reader)
        {
            var result = Read(WriterSchema, ReaderSchema, reader);
            return result;
        }

        public object Read(Schema writerSchema, Schema readerSchema, IReader d)
        {
            if (readerSchema.Tag == Schema.Type.Union && writerSchema.Tag != Schema.Type.Union)
            {
                readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
            }

            switch (writerSchema.Tag)
            {
                case Schema.Type.Null:
                    return ReadNull(readerSchema, d);
                case Schema.Type.Boolean:
                    return Read<bool>(writerSchema.Tag, readerSchema, d.ReadBoolean);
                case Schema.Type.Int:
                    {
                        int i = Read<int>(writerSchema.Tag, readerSchema, d.ReadInt);
                        switch (readerSchema.Tag)
                        {
                            case Schema.Type.Long:
                                return (long)i;
                            case Schema.Type.Float:
                                return (float)i;
                            case Schema.Type.Double:
                                return (double)i;
                            default:
                                return i;
                        }
                    }
                case Schema.Type.Long:
                    {
                        long l = Read<long>(writerSchema.Tag, readerSchema, d.ReadLong);
                        switch (readerSchema.Tag)
                        {
                            case Schema.Type.Float:
                                return (float)l;
                            case Schema.Type.Double:
                                return (double)l;
                            default:
                                return l;
                        }
                    }
                case Schema.Type.Float:
                    {
                        float f = Read<float>(writerSchema.Tag, readerSchema, d.ReadFloat);
                        switch (readerSchema.Tag)
                        {
                            case Schema.Type.Double:
                                return (double)f;
                            default:
                                return f;
                        }
                    }
                case Schema.Type.Double:
                    return Read<double>(writerSchema.Tag, readerSchema, d.ReadDouble);
                case Schema.Type.String:
                    return Read<string>(writerSchema.Tag, readerSchema, d.ReadString);
                case Schema.Type.Bytes:
                    return Read<byte[]>(writerSchema.Tag, readerSchema, d.ReadBytes);
                case Schema.Type.Error:
                case Schema.Type.Record:
                    return ReadRecord((RecordSchema)writerSchema, readerSchema, d);
                case Schema.Type.Enumeration:
                    return ReadEnum((EnumSchema)writerSchema, readerSchema, d);
                case Schema.Type.Fixed:
                    return ReadFixed((FixedSchema)writerSchema, readerSchema, d);
                case Schema.Type.Array:
                    return ReadArray((ArraySchema)writerSchema, readerSchema, d);
                case Schema.Type.Map:
                    return ReadMap((MapSchema)writerSchema, readerSchema, d);
                case Schema.Type.Union:
                    return ReadUnion((UnionSchema)writerSchema, readerSchema, d);
                default:
                    throw new AvroException("Unknown schema type: " + writerSchema);
            }
        }


        protected virtual object ReadNull(Schema readerSchema, IReader d)
        {
            d.ReadNull();
            return null;
        }

        protected S Read<S>(Schema.Type tag, Schema readerSchema, Reader<S> reader)
        {
            return reader();
        }


        protected virtual IDictionary<string, object> ReadRecord(RecordSchema writerSchema, Schema readerSchema, IReader dec)
        {
            RecordSchema rs = (RecordSchema)readerSchema;

            Record result = new Record(rs);
            foreach (Field wf in writerSchema)
            {
                Field rf;
                if (rs.TryGetFieldAlias(wf.Name, out rf))
                {
                    object obj = null;
                    TryGetField(result, wf.Name, rf.Pos, out obj);

                    AddField(result, rf.aliases?[0] ?? wf.Name, rf.Pos, Read(wf.Schema, rf.Schema, dec));
                }
                else
                    Skip(wf.Schema, dec);

            }
            return result.Contents;
        }

        protected virtual bool TryGetField(object record, string fieldName, int fieldPos, out object value)
        {
            return ((Record)record).TryGetValue(fieldName, out value);
        }


        protected virtual void AddField(Record record, string fieldName, int fieldPos, object fieldValue)
        {
            record.Contents[fieldName] = fieldValue;
        }


        protected virtual object ReadEnum(EnumSchema writerSchema, Schema readerSchema, IReader d)
        {
            EnumSchema es = readerSchema as EnumSchema;
            return new Enum(es, writerSchema[d.ReadEnum()]);
        }


        protected virtual object ReadArray(ArraySchema writerSchema, Schema readerSchema, IReader d)
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
                    result[i] = Read(writerSchema.ItemSchema, rs.ItemSchema, d);
                }
            }

            if (result[0] is IDictionary)
            {
                Dictionary<object, object> resultDictionary = new Dictionary<object, object>();
                foreach (Dictionary<string, object> keyValue in result)
                {
                    if (!keyValue.ContainsKey("Key") || !keyValue.ContainsKey("Value"))
                    {
                        //HACK for reading c# dictionaries, which are not avro maps

                        return result;
                    }
                    resultDictionary.Add(keyValue["Key"], keyValue["Value"]);
                }

                return resultDictionary;
            }

            return result;
        }

        protected virtual object ReadMap(MapSchema writerSchema, Schema readerSchema, IReader d)
        {
            MapSchema rs = (MapSchema)readerSchema;
            Dictionary<string, object> result = new Dictionary<string, object>();
            for (int n = (int)d.ReadMapStart(); n != 0; n = (int)d.ReadMapNext())
            {
                for (int j = 0; j < n; j++)
                {
                    string k = d.ReadString();
                    result.Add(k, Read(writerSchema.ValueSchema, rs.ValueSchema, d));
                }
            }

            return result;
        }

        protected virtual object ReadUnion(UnionSchema writerSchema, Schema readerSchema, IReader d)
        {
            int index = d.ReadUnionIndex();
            Schema ws = writerSchema[index];

            if (readerSchema is UnionSchema unionSchema)
                readerSchema = FindBranch(unionSchema, ws);
            else
                if (!readerSchema.CanRead(ws))
                throw new AvroException("Schema mismatch. Reader: " + ReaderSchema + ", writer: " + WriterSchema);

            return Read(ws, readerSchema, d);
        }

        protected virtual object ReadFixed(FixedSchema writerSchema, Schema readerSchema, IReader d)
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

            if (bb.Length == 16)
            {
                return new Guid(bb);
            }

            return ru;
        }

        protected virtual void Skip(Schema writerSchema, IReader d)
        {
            switch (writerSchema.Tag)
            {
                case Schema.Type.Null:
                    d.SkipNull();
                    break;
                case Schema.Type.Boolean:
                    d.SkipBoolean();
                    break;
                case Schema.Type.Int:
                    d.SkipInt();
                    break;
                case Schema.Type.Long:
                    d.SkipLong();
                    break;
                case Schema.Type.Float:
                    d.SkipFloat();
                    break;
                case Schema.Type.Double:
                    d.SkipDouble();
                    break;
                case Schema.Type.String:
                    d.SkipString();
                    break;
                case Schema.Type.Bytes:
                    d.SkipBytes();
                    break;
                case Schema.Type.Record:
                    foreach (Field f in (RecordSchema)writerSchema) Skip(f.Schema, d);
                    break;
                case Schema.Type.Enumeration:
                    d.SkipEnum();
                    break;
                case Schema.Type.Fixed:
                    d.SkipFixed(((FixedSchema)writerSchema).Size);
                    break;
                case Schema.Type.Array:
                    {
                        Schema s = ((ArraySchema)writerSchema).ItemSchema;
                        for (long n = d.ReadArrayStart(); n != 0; n = d.ReadArrayNext())
                        {
                            for (long i = 0; i < n; i++) Skip(s, d);
                        }
                    }
                    break;
                case Schema.Type.Map:
                    {
                        Schema s = ((MapSchema)writerSchema).ValueSchema;
                        for (long n = d.ReadMapStart(); n != 0; n = d.ReadMapNext())
                        {
                            for (long i = 0; i < n; i++) { d.SkipString(); Skip(s, d); }
                        }
                    }
                    break;
                case Schema.Type.Union:
                    Skip(((UnionSchema)writerSchema)[d.ReadUnionIndex()], d);
                    break;
                case Schema.Type.Error:
                    break;
                default:
                    throw new AvroException("Unknown schema type: " + writerSchema);
            }
        }

        protected static Schema FindBranch(UnionSchema us, Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }

    }
}
