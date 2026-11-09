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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.AvroToJson
{
    /// <summary>
    /// Writes the decoded data as JSON: records and maps as objects, arrays as arrays, the values as Newtonsoft writes them
    /// with its default settings. JsonConvert.DefaultSettings of the application are not used: the names are the names of
    /// the schema, not of a naming policy for .NET types.
    /// </summary>
    /// <remarks>
    /// Newtonsoft's recursion has no stack check, and it catches an exception at every level to build the JSON path,
    /// on the stack that is already short. Data the decoder still read (about 1,200 levels on a 1 MB stack) overflowed
    /// the stack, which ends the process. Here every level checks the stack and nothing catches on the way up.
    /// </remarks>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal static class JsonOutput
    {
        internal static string Write(object value)
        {
            var text = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(text))
            {
                WriteValue(writer, value);
            }

            return text.ToString();
        }

        private static void WriteValue(JsonWriter writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNull();
                    return;
                // The values the decoder gives for primitive types, written as Newtonsoft writes them.
                case string text:
                    writer.WriteValue(text);
                    return;
                case int number:
                    writer.WriteValue(number);
                    return;
                case long number:
                    writer.WriteValue(number);
                    return;
                case double number:
                    writer.WriteValue(number);
                    return;
                case float number:
                    writer.WriteValue(number);
                    return;
                case bool flag:
                    writer.WriteValue(flag);
                    return;
                case byte[] bytes:
                    writer.WriteValue(bytes);
                    return;
                // Logical types: the JsonWriter overloads Newtonsoft calls for them with its default settings.
                case decimal number:
                    writer.WriteValue(number);
                    return;
                case DateTime date:
                    writer.WriteValue(date);
                    return;
                case DateTimeOffset date:
                    writer.WriteValue(date);
                    return;
                case Guid guid:
                    writer.WriteValue(guid);
                    return;
                case TimeSpan time:
                    writer.WriteValue(time);
                    return;
                case Dictionary<string, object> record:
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    writer.WriteStartObject();
                    foreach (var field in record)
                    {
                        writer.WritePropertyName(field.Key);
                        WriteValue(writer, field.Value);
                    }
                    writer.WriteEndObject();
                    return;
                case Dictionary<object, object> dictionary:
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    writer.WriteStartObject();
                    foreach (var entry in dictionary)
                    {
                        writer.WritePropertyName(PropertyName(entry.Key));
                        WriteValue(writer, entry.Value);
                    }
                    writer.WriteEndObject();
                    return;
                case object[] items:
                    WriteArray(writer, items);
                    return;
                case List<object> items:
                    WriteArray(writer, items);
                    return;
                default:
                    // DateOnly, TimeOnly and anything else.
                    Json.Serializer.Serialize(writer, value);
                    return;
            }
        }

        private static void WriteArray(JsonWriter writer, IEnumerable<object> items)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            writer.WriteStartArray();
            foreach (var item in items)
            {
                WriteValue(writer, item);
            }
            writer.WriteEndArray();
        }

        /// <summary>The name Newtonsoft gives a dictionary key that is not a string (a date in ISO 8601, a number in the invariant culture…).</summary>
        private static string PropertyName(object key)
        {
            switch (key)
            {
                case string name:
                    return name;
                // Newtonsoft names these with Convert.ToString in the invariant culture; dates and floating point have formats of their own.
                case Guid:
                case IConvertible and not (DateTime or double or float or Enum):
                    return Convert.ToString(key, CultureInfo.InvariantCulture);
            }

            var json = new StringWriter(CultureInfo.InvariantCulture);
            Json.Serializer.Serialize(json, new Dictionary<object, object> { [key] = null });
            using var reader = new JsonTextReader(new StringReader(json.ToString()));
            reader.Read();
            reader.Read();
            return (string)reader.Value;
        }
    }
}
