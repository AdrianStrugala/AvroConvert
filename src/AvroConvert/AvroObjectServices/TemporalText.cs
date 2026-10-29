using System;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace SolTechnology.Avro.AvroObjectServices
{
    /// <summary>
    /// Date and time types stored in an Avro "string": ISO 8601 text, invariant culture. The format is the round-trip
    /// pattern ("O") and an ISO 8601 duration for TimeSpan, the same text that Chr.Avro writes by default.
    /// </summary>
    internal static class TemporalText
    {
        internal static bool IsTemporal(Type type) =>
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(DateOnly) || type == typeof(TimeOnly);

        internal static MethodInfo FormatMethod(Type type) =>
            typeof(TemporalText).GetMethod(nameof(Format), BindingFlags.Static | BindingFlags.NonPublic, new[] { type })!;

        internal static MethodInfo ParseMethod(Type type) =>
            typeof(TemporalText).GetMethod("Parse" + type.Name, BindingFlags.Static | BindingFlags.NonPublic)!;

        internal static string Format(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);

        internal static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

        internal static string Format(TimeSpan value) => XmlConvert.ToString(value);

        internal static string Format(DateOnly value) => value.ToString("O", CultureInfo.InvariantCulture);

        internal static string Format(TimeOnly value) => value.ToString("O", CultureInfo.InvariantCulture);

        internal static DateTime ParseDateTime(string text) =>
            DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        internal static DateTimeOffset ParseDateTimeOffset(string text) =>
            IsIsoDate(text)
                ? DateTimeOffset.Parse(text, CultureInfo.InvariantCulture)
                : DateTimeOffset.Parse(text, CultureInfo.CurrentCulture); // text from DateTimeOffset.ToString() in the current culture

        /// <summary>True for text starting with an ISO 8601 date (yyyy-MM-dd), which reads the same in every culture.</summary>
        private static bool IsIsoDate(string text) =>
            text.Length >= 10 && char.IsAsciiDigit(text[0]) && char.IsAsciiDigit(text[1]) && char.IsAsciiDigit(text[2]) && char.IsAsciiDigit(text[3])
            && text[4] == '-' && char.IsAsciiDigit(text[5]) && char.IsAsciiDigit(text[6]) && text[7] == '-' && char.IsAsciiDigit(text[8]) && char.IsAsciiDigit(text[9]);

        internal static TimeSpan ParseTimeSpan(string text) =>
            text.StartsWith('P') || text.StartsWith("-P", StringComparison.Ordinal)
                ? XmlConvert.ToTimeSpan(text)
                : TimeSpan.Parse(text, CultureInfo.InvariantCulture);

        internal static DateOnly ParseDateOnly(string text) => DateOnly.Parse(text, CultureInfo.InvariantCulture);

        internal static TimeOnly ParseTimeOnly(string text) => TimeOnly.Parse(text, CultureInfo.InvariantCulture);
    }
}
