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
        private readonly Dictionary<string, NamedSchema> _definedNames = new();

        internal string CurrentNamespace => _namespaces.Count == 0 ? string.Empty : _namespaces.Peek();

        internal void EnterNamespace(string @namespace) =>
            _namespaces.Push(string.IsNullOrEmpty(@namespace) ? CurrentNamespace : @namespace);

        internal void ExitNamespace() => _namespaces.Pop();

        /// <summary>Full name the type gets where it is written now (its own namespace or the enclosing one).</summary>
        internal string EffectiveFullName(NamedSchema schema) => EffectiveFullName(schema, schema.Name);

        private string EffectiveFullName(NamedSchema schema, string name)
        {
            var @namespace = string.IsNullOrEmpty(schema.Namespace) ? CurrentNamespace : schema.Namespace;
            return string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name;
        }

        /// <summary>Records a definition by its effective full name; false when that name has already been defined.</summary>
        internal bool TryDefine(string effectiveFullName, NamedSchema schema) => _definedNames.TryAdd(effectiveFullName, schema);

        /// <summary>
        /// True when <paramref name="schema"/> is written already, so a reference to <paramref name="name"/> (a full name) is
        /// enough; otherwise <paramref name="name"/> is the name to define it under. One type can come as several objects (a
        /// schema completed from values is put together from many schemas), so the same schema under a name is that name. A
        /// different type of a name defined already gets the name with a number (Status_2): Avro allows one type per name.
        /// </summary>
        internal bool IsDefined(NamedSchema schema, out string name)
        {
            name = schema.Name;
            var fullName = EffectiveFullName(schema);
            for (var i = 2; _definedNames.TryGetValue(fullName, out var defined); i++)
            {
                if (ReferenceEquals(defined, schema) || defined.ToString() == schema.ToString())
                {
                    name = fullName;
                    return true;
                }

                name = $"{schema.Name}_{i}";
                fullName = EffectiveFullName(schema, name);
            }

            _definedNames.Add(fullName, schema);
            return false;
        }
    }
}
