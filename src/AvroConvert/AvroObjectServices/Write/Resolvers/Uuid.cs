#region license
/**Copyright (c) 2021 Adrian Strugała
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

using System;
using System.Buffers.Text;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveUuid(UuidSchema schema)
        {
            return (value, encoder) =>
            {
                if (value is not Guid guid)
                {
                    throw new AvroTypeMismatchException($"[Guid] required to write against [string] of [Uuid] schema but found [{value.GetType()}]");
                }

#if NET6_0_OR_GREATER
                Span<byte> buffer = stackalloc byte[36];
                Utf8Formatter.TryFormat(guid, buffer, out _);
                encoder.WriteBytes(buffer);
#else
                encoder.WriteString(guid.ToString());
#endif
            };
        }
    }
}
