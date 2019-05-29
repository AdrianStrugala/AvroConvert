namespace AvroConvert.Read
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Models;
    using Schema;
    using Write;
    using Enum = global::AvroConvert.Models.Enum;

    public delegate T Reader<T>();

    public sealed class DataReader
    {
        private readonly DefaultReader reader;

        public DataReader(Schema writerSchema, Schema readerSchema)
            : this(new DefaultReader(writerSchema, readerSchema))
        {
        }

        public DataReader(DefaultReader reader)
        {
            this.reader = reader;
        }

        public Schema WriterSchema { get { return reader.WriterSchema; } }

        public Schema ReaderSchema { get { return reader.ReaderSchema; } }

        public object Read(IDecoder d)
        {
            return reader.Read(d);
        }
    }

    public class DefaultReader
    {
        public Schema ReaderSchema { get; private set; }
        public Schema WriterSchema { get; private set; }

        public DefaultReader(Schema writerSchema, Schema readerSchema)
        {
            this.ReaderSchema = readerSchema;
            this.WriterSchema = writerSchema;
        }

        public object Read(IDecoder decoder)
        {
            if (!ReaderSchema.CanRead(WriterSchema))
                throw new AvroException("Schema mismatch. Reader: " + ReaderSchema + ", writer: " + WriterSchema);

            var result = Read(WriterSchema, ReaderSchema, decoder);
            return result;
        }

        public object Read(Schema writerSchema, Schema readerSchema, IDecoder d)
        {
            if (readerSchema.Tag == Schema.Type.Union && writerSchema.Tag != Schema.Type.Union)
            {
                readerSchema = findBranch(readerSchema as UnionSchema, writerSchema);
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


        protected virtual object ReadNull(Schema readerSchema, IDecoder d)
        {
            d.ReadNull();
            return null;
        }

        protected S Read<S>(Schema.Type tag, Schema readerSchema, Reader<S> reader)
        {
            return reader();
        }


        protected virtual IDictionary<string, object> ReadRecord(RecordSchema writerSchema, Schema readerSchema, IDecoder dec)
        {
            RecordSchema rs = (RecordSchema)readerSchema;

            Record result = CreateRecord(rs);
            foreach (Field wf in writerSchema)
            {
                try
                {
                    Field rf;
                    if (rs.TryGetFieldAlias(wf.Name, out rf))
                    {
                        object obj = null;
                        TryGetField(result, wf.Name, rf.Pos, out obj);

                        AddField(result, wf.Name, rf.Pos, Read(wf.Schema, rf.Schema, dec));
                    }
                    else
                        Skip(wf.Schema, dec);
                }
                catch (Exception ex)
                {
                    throw new AvroException(ex.Message + " in field " + wf.Name);
                }
            }

            var defaultStream = new MemoryStream();
            var defaultEncoder = new Writer(defaultStream);
            var defaultDecoder = new BinaryDecoder(defaultStream);
            foreach (Field rf in rs)
            {
                if (writerSchema.Contains(rf.Name)) continue;

                defaultStream.Position = 0; // reset for writing
                Resolver.EncodeDefaultValue(defaultEncoder, rf.Schema, rf.DefaultValue);
                defaultStream.Flush();
                defaultStream.Position = 0; // reset for reading

                object obj = null;
                TryGetField(result, rf.Name, rf.Pos, out obj);
                AddField(result, rf.Name, rf.Pos, Read(rf.Schema, rf.Schema, defaultDecoder));
            }

            return result.Contents;
        }

        protected virtual Record CreateRecord(RecordSchema readerSchema)
        {
            Record ru =
                new Record(readerSchema);
            return ru;
        }


        protected virtual bool TryGetField(object record, string fieldName, int fieldPos, out object value)
        {
            return (record as Record).TryGetValue(fieldName, out value);
        }


        protected virtual void AddField(object record, string fieldName, int fieldPos, object fieldValue)
        {
            (record as Record).Add(fieldName, fieldValue);
        }


        protected virtual object ReadEnum(EnumSchema writerSchema, Schema readerSchema, IDecoder d)
        {
            EnumSchema es = readerSchema as EnumSchema;
            return new Enum(es, writerSchema[d.ReadEnum()]);
        }


        protected virtual object ReadArray(ArraySchema writerSchema, Schema readerSchema, IDecoder d)
        {
            ArraySchema rs = (ArraySchema)readerSchema;
            object result = new object[0];
            int i = 0;
            for (int n = (int)d.ReadArrayStart(); n != 0; n = (int)d.ReadArrayNext())
            {
                if (GetArraySize(result) < (i + n)) ResizeArray(ref result, i + n);
                for (int j = 0; j < n; j++, i++)
                {
                    SetArrayElement(result, i, Read(writerSchema.ItemSchema, rs.ItemSchema, d));
                }
            }
            if (GetArraySize(result) != i) ResizeArray(ref result, i);
            return result;
        }


        protected virtual int GetArraySize(object array)
        {
            return (array as object[]).Length;
        }


        protected virtual void ResizeArray(ref object array, int n)
        {
            object[] o = array as object[];
            Array.Resize(ref o, n);
            array = o;
        }


        protected virtual void SetArrayElement(object array, int index, object value)
        {
            object[] a = array as object[];
            a[index] = value;
        }


        protected virtual object ReadMap(MapSchema writerSchema, Schema readerSchema, IDecoder d)
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


        protected virtual object ReadUnion(UnionSchema writerSchema, Schema readerSchema, IDecoder d)
        {
            int index = d.ReadUnionIndex();
            Schema ws = writerSchema[index];

            if (readerSchema is UnionSchema)
                readerSchema = findBranch(readerSchema as UnionSchema, ws);
            else
                if (!readerSchema.CanRead(ws))
                throw new AvroException("Schema mismatch. Reader: " + ReaderSchema + ", writer: " + WriterSchema);

            return Read(ws, readerSchema, d);
        }

        protected virtual object ReadFixed(FixedSchema writerSchema, Schema readerSchema, IDecoder d)
        {
            FixedSchema rs = (FixedSchema)readerSchema;
            if (rs.Size != writerSchema.Size)
            {
                throw new AvroException("Size mismatch between reader and writer fixed schemas. Encoder: " + writerSchema +
                    ", reader: " + readerSchema);
            }

            object ru = new Fixed(rs);
            byte[] bb = (ru as Fixed).Value;
            d.ReadFixed(bb);
            return ru;
        }

        protected virtual void Skip(Schema writerSchema, IDecoder d)
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
                    foreach (Field f in writerSchema as RecordSchema) Skip(f.Schema, d);
                    break;
                case Schema.Type.Enumeration:
                    d.SkipEnum();
                    break;
                case Schema.Type.Fixed:
                    d.SkipFixed((writerSchema as FixedSchema).Size);
                    break;
                case Schema.Type.Array:
                    {
                        Schema s = (writerSchema as ArraySchema).ItemSchema;
                        for (long n = d.ReadArrayStart(); n != 0; n = d.ReadArrayNext())
                        {
                            for (long i = 0; i < n; i++) Skip(s, d);
                        }
                    }
                    break;
                case Schema.Type.Map:
                    {
                        Schema s = (writerSchema as MapSchema).ValueSchema;
                        for (long n = d.ReadMapStart(); n != 0; n = d.ReadMapNext())
                        {
                            for (long i = 0; i < n; i++) { d.SkipString(); Skip(s, d); }
                        }
                    }
                    break;
                case Schema.Type.Union:
                    Skip((writerSchema as UnionSchema)[d.ReadUnionIndex()], d);
                    break;
                default:
                    throw new AvroException("Unknown schema type: " + writerSchema);
            }
        }

        protected static Schema findBranch(UnionSchema us, Schema s)
        {
            int index = us.MatchingBranch(s);
            if (index >= 0) return us[index];
            throw new AvroException("No matching schema for " + s + " in " + us);
        }

    }
}
