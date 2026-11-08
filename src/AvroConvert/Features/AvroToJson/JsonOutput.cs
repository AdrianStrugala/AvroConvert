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
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace SolTechnology.Avro.Features.AvroToJson
{
    /// <summary>
    /// Writes the decoded data as JsonConvert.SerializeObject does, with the records, maps and arrays written here and
    /// the values in them by Newtonsoft.
    /// </summary>
    /// <remarks>
    /// Newtonsoft's recursion has no stack check, and it catches an exception at every level to build the JSON path,
    /// on the stack that is already short. Data the decoder still read (about 1,200 levels on a 1 MB stack) overflowed
    /// the stack, which ends the process. Here every level checks the stack and nothing catches on the way up.
    /// </remarks>
    internal static class JsonOutput
    {
        internal static string Write(object value)
        {
            var serializer = JsonSerializer.CreateDefault();
            var text = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(text) { Formatting = serializer.Formatting, StringEscapeHandling = serializer.StringEscapeHandling })
            {
                WriteValue(writer, value, serializer);
            }

            return text.ToString();
        }

        private static void WriteValue(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case Dictionary<string, object> record:
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    writer.WriteStartObject();
                    foreach (var field in record)
                    {
                        writer.WritePropertyName(field.Key);
                        WriteValue(writer, field.Value, serializer);
                    }
                    writer.WriteEndObject();
                    return;

                case Dictionary<object, object> dictionary:
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    writer.WriteStartObject();
                    foreach (var entry in dictionary)
                    {
                        writer.WritePropertyName(PropertyName(entry.Key, serializer));
                        WriteValue(writer, entry.Value, serializer);
                    }
                    writer.WriteEndObject();
                    return;

                case object[] or List<object>:
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    writer.WriteStartArray();
                    foreach (var item in (IEnumerable<object>)value)
                    {
                        WriteValue(writer, item, serializer);
                    }
                    writer.WriteEndArray();
                    return;

                default:
                    serializer.Serialize(writer, value);
                    return;
            }
        }

        /// <summary>The name Newtonsoft gives a dictionary key that is not a string (a date in ISO 8601, a number in the invariant culture…).</summary>
        private static string PropertyName(object key, JsonSerializer serializer)
        {
            if (key is string name)
            {
                return name;
            }

            var json = new StringWriter(CultureInfo.InvariantCulture);
            serializer.Serialize(json, new Dictionary<object, object> { [key] = null });
            using var reader = new JsonTextReader(new StringReader(json.ToString()));
            reader.Read();
            reader.Read();
            return (string)reader.Value;
        }
    }
}
