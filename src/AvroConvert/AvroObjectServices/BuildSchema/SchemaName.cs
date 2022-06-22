// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License.  You may obtain a copy
// of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

/** Modifications copyright(C) 2020 Adrian Struga³a **/

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    /// Represents a name of the schema. For details, please see <a href="http://avro.apache.org/docs/current/spec.html#Names">the specification</a>.
    /// </summary>
    internal sealed class SchemaName
    {
        private static readonly Regex NamePattern = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");
        private static readonly Regex NamespacePattern = new Regex("^([A-Za-z_][A-Za-z0-9_]*)?(?:\\.[A-Za-z_][A-Za-z0-9_]*)*$");

        private readonly string name;
        private readonly string @namespace;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SchemaName" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        internal SchemaName(string name) : this(name, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaName" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="namespace">The namespace.</param>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="name"/> is empty or null.</exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">Thrown when any argument is invalid.</exception>
        internal SchemaName(string name, string @namespace)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Name is not allowed to be null or empty."), "name");
            }

            this.@namespace = @namespace ?? string.Empty;

            int lastDot = name.LastIndexOf('.');
            if (lastDot == name.Length - 1)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Invalid name specified '{0}'.", name));
            }

            if (lastDot != -1)
            {
                this.name = name.Substring(lastDot + 1, name.Length - lastDot - 1);
                this.@namespace = name.Substring(0, lastDot);
            }
            else
            {
                this.name = name;
            }

            this.CheckNameAndNamespace();
        }

        internal string Name
        {
            get { return this.name; }
        }

        internal string Namespace
        {
            get { return this.@namespace; }
        }

        internal string FullName
        {
            get { return string.IsNullOrEmpty(this.@namespace) ? this.name : this.@namespace + "." + this.name; }
        }


        private void CheckNameAndNamespace()
        {
            if (!NamePattern.IsMatch(this.name))
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Name '{0}' contains invalid characters.", this.name));
            }

            if (!NamespacePattern.IsMatch(this.@namespace))
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Namespace '{0}' contains invalid characters.", this.@namespace));
            }
        }
    }
}
