using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avro
{
    public class CodeGen
    {
        internal static string getType(Schema schema, bool nullible, ref bool nullibleEnum)
        {
            switch (schema.Tag)
            {
                case Schema.Type.Null:
                    return "System.Object";
                case Schema.Type.Boolean:
                    if (nullible) return "System.Nullable<bool>";
                    else return typeof(bool).ToString();
                case Schema.Type.Int:
                    if (nullible) return "System.Nullable<int>";
                    else return typeof(int).ToString();
                case Schema.Type.Long:
                    if (nullible) return "System.Nullable<long>";
                    else return typeof(long).ToString();
                case Schema.Type.Float:
                    if (nullible) return "System.Nullable<float>";
                    else return typeof(float).ToString();
                case Schema.Type.Double:
                    if (nullible) return "System.Nullable<double>";
                    else return typeof(double).ToString();

                case Schema.Type.Bytes:
                    return typeof(byte[]).ToString();
                case Schema.Type.String:
                    return typeof(string).ToString();

                case Schema.Type.Enumeration:
                    var namedSchema = schema as NamedSchema;
                    if (null == namedSchema)
                        throw new CodeGenException("Unable to cast schema into a named schema");
                    if (nullible)
                    {
                        nullibleEnum = true;
                        return "System.Nullable<" + CodeGenUtil.Instance.Mangle(namedSchema.Fullname) + ">";
                    }
                    else return CodeGenUtil.Instance.Mangle(namedSchema.Fullname);

                case Schema.Type.Fixed:
                case Schema.Type.Record:
                case Schema.Type.Error:
                    namedSchema = schema as NamedSchema;
                    if (null == namedSchema)
                        throw new CodeGenException("Unable to cast schema into a named schema");
                    return CodeGenUtil.Instance.Mangle(namedSchema.Fullname);

                case Schema.Type.Array:
                    var arraySchema = schema as ArraySchema;
                    if (null == arraySchema)
                        throw new CodeGenException("Unable to cast schema into an array schema");

                    return "IList<" + getType(arraySchema.ItemSchema, false, ref nullibleEnum) + ">";

                case Schema.Type.Map:
                    var mapSchema = schema as MapSchema;
                    if (null == mapSchema)
                        throw new CodeGenException("Unable to cast schema into a map schema");
                    return "IDictionary<string," + getType(mapSchema.ValueSchema, false, ref nullibleEnum) + ">";

                case Schema.Type.Union:
                    var unionSchema = schema as UnionSchema;
                    if (null == unionSchema)
                        throw new CodeGenException("Unable to cast schema into a union schema");
                    Schema nullibleType = getNullableType(unionSchema);
                    if (null == nullibleType)
                        return CodeGenUtil.Object;
                    else
                        return getType(nullibleType, true, ref nullibleEnum);
            }
            throw new CodeGenException("Unable to generate CodeTypeReference for " + schema.Name + " type " + schema.Tag);
        }

        /// <summary>
        /// Gets the schema of a union with null
        /// </summary>
        /// <param name="schema">union schema</param>
        /// <returns>schema that is nullible</returns>
        public static Schema getNullableType(UnionSchema schema)
        {
            Schema ret = null;
            if (schema.Count == 2)
            {
                bool nullable = false;
                foreach (Schema childSchema in schema.Schemas)
                {
                    if (childSchema.Tag == Schema.Type.Null)
                        nullable = true;
                    else
                        ret = childSchema;
                }
                if (!nullable)
                    ret = null;
            }
            return ret;
        }
    }
}
