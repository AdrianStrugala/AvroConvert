using System.Collections.Generic;
using System.Linq;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Write.Resolvers
{
    public class Union
    {
        public Encoder.WriteItem Resolve(UnionSchema unionSchema)
        {
            var branchSchemas = unionSchema.Schemas.ToArray();
            var branchWriters = new Encoder.WriteItem[branchSchemas.Length];
            int branchIndex = 0;
            foreach (var branch in branchSchemas)
            {
                branchWriters[branchIndex++] = Resolver.ResolveWriter(branch);
            }


            return (v, e) => WriteUnion(unionSchema, branchSchemas, branchWriters, v, e);
        }

        /*TODO:
         * FIXME: This method of determining the Union branch has problems. If the data is IDictionary<string, object>
         * if there are two branches one with record schema and the other with map, it choose the first one. Similarly if
         * the data is byte[] and there are fixed and bytes schemas as branches, it choose the first one that matches.
         * Also it does not recognize the arrays of primitive types.
         */
        private bool UnionBranchMatches(Schema.Schema sc, object obj)
        {
            if (obj == null && sc.Tag != Schema.Schema.Type.Null) return false;
            switch (sc.Tag)
            {
                case Schema.Schema.Type.Null:
                    return obj == null;
                case Schema.Schema.Type.Boolean:
                    return obj is bool;
                case Schema.Schema.Type.Int:
                    return obj is int;
                case Schema.Schema.Type.Long:
                    return obj is long;
                case Schema.Schema.Type.Float:
                    return obj is float;
                case Schema.Schema.Type.Double:
                    return obj is double;
                case Schema.Schema.Type.Bytes:
                    return obj is byte[];
                case Schema.Schema.Type.String:
                    return obj is string;
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    //return obj is GenericRecord && (obj as GenericRecord)._schema.Equals(s);
                    return obj is Models.Record &&
                           (obj as Models.Record).Schema.SchemaName.Equals((sc as RecordSchema).SchemaName);
                case Schema.Schema.Type.Enumeration:
                    //return obj is GenericEnum && (obj as GenericEnum)._schema.Equals(s);
                    return obj is Models.Enum &&
                           (obj as Models.Enum).Schema.SchemaName.Equals((sc as EnumSchema).SchemaName);
                case Schema.Schema.Type.Array:
                    return obj is System.Array && !(obj is byte[]);
                case Schema.Schema.Type.Map:
                    return obj is IDictionary<string, object>;
                case Schema.Schema.Type.Union:
                    return false; // Union directly within another union not allowed!
                case Schema.Schema.Type.Fixed:
                    //return obj is GenericFixed && (obj as GenericFixed)._schema.Equals(s);
                    return obj is Models.Fixed &&
                           (obj as Models.Fixed).Schema.SchemaName.Equals((sc as FixedSchema).SchemaName);
                default:
                    throw new AvroException("Unknown schema type: " + sc.Tag);
            }
        }

        private void WriteUnion(UnionSchema unionSchema, Schema.Schema[] branchSchemas, Encoder.WriteItem[] branchWriters, object value, IWriter encoder)
        {
            int index = ResolveUnion(unionSchema, branchSchemas, value);
            encoder.WriteUnionIndex(index);
            branchWriters[index](value, encoder);
        }

        private int ResolveUnion(UnionSchema us, Schema.Schema[] branchSchemas, object obj)
        {
            for (int i = 0; i < branchSchemas.Length; i++)
            {
                if (UnionBranchMatches(branchSchemas[i], obj)) return i;
            }
            throw new AvroException("Cannot find a match for " + obj.GetType() + " in " + us);
        }
    }
}
