using System;
using System.Buffers.Binary;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>Encoders for logical types whose representation is not a plain primitive conversion.</summary>
    internal static class LogicalWriters
    {
        internal static void WriteDecimal(IWriter writer, decimal value, DecimalSchema schema, AvroNumberHandling numberHandling)
        {
            var avroDecimal = new AvroDecimal(value);
            var logicalScale = schema.Scale;
            var scale = avroDecimal.Scale;

            // Resize the value to match the schema scale.
            int sizeDiff = logicalScale - scale;
            if (sizeDiff < 0)
            {
                switch (numberHandling)
                {
                    case AvroNumberHandling.Strict:
                        throw new AvroTypeException(
                            $@"Decimal Scale for value [{value}] is equal to [{scale}]. This exceeds default setting [{logicalScale}].
Consider adding following attribute to your property:
[AvroDecimal(Precision = 28, Scale = {scale})]
");
                    case AvroNumberHandling.Truncate:
                        var multiplier = (decimal)Math.Pow(10, logicalScale);
                        value = Math.Truncate(value * multiplier) / multiplier;
                        avroDecimal = new AvroDecimal(value);
                        sizeDiff = 0;
                        break;
                    case AvroNumberHandling.Rounding:
                        value = Math.Round(value, logicalScale);
                        avroDecimal = new AvroDecimal(value);
                        sizeDiff = 0;
                        break;
                }
            }

            string trailingZeros = new string('0', sizeDiff);
            var valueString = value.ToString();
            var valueWithTrailingZeros = valueString.Contains(avroDecimal.SeparatorCharacter)
                ? $"{valueString}{trailingZeros}"
                : $"{valueString}{avroDecimal.SeparatorCharacter}{trailingZeros}";

            avroDecimal = new AvroDecimal(valueWithTrailingZeros);

            Span<byte> buffer = stackalloc byte[avroDecimal.UnscaledValue.GetByteCount(isUnsigned: false)];
            avroDecimal.UnscaledValue.TryWriteBytes(buffer, out _);
            buffer.Reverse();

            if (schema.BaseTypeSchema.Type == AvroType.Bytes)
            {
                writer.WriteBytes(buffer);
                return;
            }

            // Fixed base: left-pad with the sign byte up to the fixed size.
            int size = ((FixedSchema)schema.BaseTypeSchema).Size;
            Span<byte> padded = stackalloc byte[size];
            int offset = size - buffer.Length;
            padded.Slice(0, offset).Fill(avroDecimal.Sign < 0 ? (byte)0xFF : (byte)0x00);
            buffer.CopyTo(padded.Slice(offset));
            writer.WriteBytes(padded);
        }

        internal static void WriteDuration(IWriter writer, TimeSpan duration, DurationSchema schema)
        {
            var baseSchema = (FixedSchema)schema.BaseTypeSchema;
            Span<byte> buffer = stackalloc byte[baseSchema.Size];
            buffer.Slice(0, 4).Fill(0);
            var milliseconds = ((duration.Hours * 60 + duration.Minutes) * 60 + duration.Seconds) * 1000 + duration.Milliseconds;
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), duration.Days);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8, 4), milliseconds);
            writer.WriteFixed(buffer);
        }

        internal static void WriteTimeMicros(IWriter writer, TimeOnly time)
        {
            if (time > TimeMicrosecondsSchema.MaxTime)
                throw new ArgumentOutOfRangeException(nameof(time), "A 'time-micros' value can only have the range '00:00:00' to '23:59:59'.");
            writer.WriteLong((time - DateTimeExtensions.UnixEpochTime).Ticks / 10);
        }

        internal static void WriteTimeMillis(IWriter writer, TimeOnly time)
        {
            if (time > TimeMillisecondsSchema.MaxTime)
                throw new ArgumentOutOfRangeException(nameof(time), "A 'time-millis' value can only have the range '00:00:00' to '23:59:59'.");
            writer.WriteInt((int)(time - DateTimeExtensions.UnixEpochTime).TotalMilliseconds);
        }

        internal static void WriteFixed(IWriter writer, AvroFixed value, FixedSchema schema)
        {
            if (value == null || value.Schema.FullName != schema.FullName || value.Schema.Size != schema.Size)
            {
                throw new AvroTypeMismatchException($"[AvroFixed] of schema [{schema.FullName}] required to write against [Fixed] schema but found {value?.Schema.FullName ?? "null"}");
            }

            writer.WriteFixed(value.Value);
        }
    }
}
