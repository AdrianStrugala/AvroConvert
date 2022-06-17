#region license
/**Copyright (c) 2021 Adrian Strugala
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
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
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
            if (obj == null && sc.Type != AvroType.Null) return false;
            switch (sc.Type)
            {
                case AvroType.Null:
                    return obj == null;
                case AvroType.Boolean:
                    return obj is bool;
                case AvroType.Int:
                    return obj is int;
                case AvroType.Long:
                    return obj is long;
                case AvroType.Float:
                    return obj is float;
                case AvroType.Double:
                    return obj is double;
                case AvroType.Bytes:
                    return obj is byte[];
                case AvroType.String:
                    return true;
                case AvroType.Error:
                    return true;
                case AvroType.Record:
                    return obj.GetType().FullName.Equals((sc as RecordSchema).FullName);
                case AvroType.Enum:
                    return obj is System.Enum;
                case AvroType.Array:
                    return !(obj is byte[]);
                case AvroType.Map:
                    return true;
                case AvroType.Union:
                    return false; // Union directly within another union not allowed!
                case AvroType.Fixed:
                    //return obj is GenericFixed && (obj as GenericFixed)._schema.Equals(s);
                    return obj is FixedModel &&
                           (obj as FixedModel).Schema.FullName.Equals((sc as FixedSchema).FullName);
                case AvroType.Logical:
                    // return (sc as LogicalTypeSchema).IsInstanceOfLogicalType(obj);
                    return true;
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
