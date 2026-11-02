#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    /// State of writing one schema to JSON: named schemas already written and the enclosing namespace, which a named type
    /// declared without a namespace inherits.
    /// </summary>
    internal sealed class SchemaWriteContext : HashSet<NamedSchema>
    {
        private readonly Stack<string> _namespaces = new();
        private readonly HashSet<string> _definedNames = new();
        private readonly Dictionary<string, NamedSchema> _definitions = new();

        internal string CurrentNamespace => _namespaces.Count == 0 ? string.Empty : _namespaces.Peek();

        internal void EnterNamespace(string @namespace) =>
            _namespaces.Push(string.IsNullOrEmpty(@namespace) ? CurrentNamespace : @namespace);

        internal void ExitNamespace() => _namespaces.Pop();

        /// <summary>Full name the type gets where it is written now (its own namespace or the enclosing one).</summary>
        internal string EffectiveFullName(NamedSchema schema)
        {
            if (!string.IsNullOrEmpty(schema.Namespace) || string.IsNullOrEmpty(CurrentNamespace))
            {
                return schema.FullName;
            }

            return CurrentNamespace + "." + schema.Name;
        }

        /// <summary>Records a definition by its effective full name; false when that name has already been defined.</summary>
        internal bool TryDefine(string effectiveFullName) => _definedNames.Add(effectiveFullName);

        /// <summary>
        /// Records the definition of <paramref name="schema"/>; false when its name is defined already, so a reference is
        /// enough. One type can come as several objects (a schema completed from values is put together from many schemas),
        /// but two different types of one name would make the reference point to the wrong one.
        /// </summary>
        /// <exception cref="SerializationException">The name is defined already by a different type.</exception>
        internal bool TryDefine(NamedSchema schema)
        {
            var name = EffectiveFullName(schema);
            if (TryDefine(name))
            {
                _definitions[name] = schema;
                return true;
            }

            if (_definitions.TryGetValue(name, out var defined) && !ReferenceEquals(defined, schema)
                && defined.ToString() != schema.ToString() && ParsingCanonicalForm.From(defined) != ParsingCanonicalForm.From(schema))
            {
                throw new SerializationException(
                    $"Two different types have the name '{name}' in one schema, which Avro does not allow: {defined} and {schema}. " +
                    "Give one of them another name, e.g. with [DataContract(Name = \"...\")].");
            }

            return false;
        }
    }
}
