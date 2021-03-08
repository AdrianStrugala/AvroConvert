#region license
/**Copyright (c) 2020 Adrian Strugała
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

using System.Linq;
using SolTechnology.Avro.BuildSchema.SchemaModels;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;
using SolTechnology.Avro.Exceptions;

namespace SolTechnology.Avro.Write.Resolvers
{
    internal class Union
    {
        internal Encoder.WriteItem Resolve(UnionSchema unionSchema)
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
        private bool UnionBranchMatches(TypeSchema sc, object obj)
        {
            if (obj == null && sc.Type != Schema.Schema.Type.Null) return false;
            switch (sc.Type)
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
                    return true;
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    return true;
                case Schema.Schema.Type.Enumeration:
                    return obj is System.Enum;
                case Schema.Schema.Type.Array:
                    return !(obj is byte[]);
                case Schema.Schema.Type.Map:
                    return true;
                case Schema.Schema.Type.Union:
                    return false; // Union directly within another union not allowed!
                case Schema.Schema.Type.Fixed:
                    //return obj is GenericFixed && (obj as GenericFixed)._schema.Equals(s);
                    return obj is FixedModel &&
                           (obj as FixedModel).Schema.FullName.Equals((sc as FixedSchema).FullName);
                default:
                    throw new AvroException("Unknown schema type: " + sc.Type);
            }
        }

        private void WriteUnion(UnionSchema unionSchema, TypeSchema[] branchSchemas, Encoder.WriteItem[] branchWriters, object value, IWriter encoder)
        {
            int index = ResolveUnion(unionSchema, branchSchemas, value);
            encoder.WriteUnionIndex(index);
            branchWriters[index](value, encoder);
        }

        private int ResolveUnion(UnionSchema us, TypeSchema[] branchSchemas, object obj)
        {
            for (int i = 0; i < branchSchemas.Length; i++)
            {
                if (UnionBranchMatches(branchSchemas[i], obj)) return i;
            }
            throw new AvroException("Cannot find a match for " + obj.GetType() + " in " + us);
        }
    }
}
