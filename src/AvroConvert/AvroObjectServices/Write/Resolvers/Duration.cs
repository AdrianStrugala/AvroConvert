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

using System;
using System.Runtime.InteropServices;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal void ResolveDuration(DurationSchema schema, object logicalValue, IWriter writer)
        {
            var duration = (TimeSpan)logicalValue;

            var baseSchema = (FixedSchema)schema.BaseTypeSchema;
            Span<byte> buffer = stackalloc byte[baseSchema.Size];
            buffer.Slice(0, 4).Fill(0);

#if NET6_0_OR_GREATER
            var days = duration.Days;
            var daysBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref days, 1));
#else
            var daysBytes = BitConverter.GetBytes(duration.Days);
#endif
            daysBytes.CopyTo(buffer.Slice(4, 4));

            var milliseconds = ((duration.Hours * 60 + duration.Minutes) * 60 + duration.Seconds) * 1000 +
                               duration.Milliseconds;
#if NET6_0_OR_GREATER
            var millisecondsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref milliseconds, 1));
#else
            var millisecondsBytes = BitConverter.GetBytes(milliseconds);
#endif
            millisecondsBytes.CopyTo(buffer.Slice(8, 4));

            if (!BitConverter.IsLittleEndian)
                buffer.Reverse();
#if NET6_0_OR_GREATER
            writer.WriteFixed(buffer);
#else
            writer.WriteFixed(buffer.ToArray());
#endif

        }
    }
}
