using System;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace SolTechnology.Avro.AvroObjectServices
{
    /// <summary>
    /// Date and time types stored in an Avro "string". Written as ISO 8601 text in the invariant culture: the round-trip
    /// pattern ("O") and an ISO 8601 duration for TimeSpan, the same text that Chr.Avro writes by default. Read: ISO 8601
    /// text in the invariant culture, any other text (what ToString() gives) in the current culture.
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
            DateTime.Parse(text, IsIsoDate(text) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind);

        internal static DateTimeOffset ParseDateTimeOffset(string text) =>
            DateTimeOffset.Parse(text, IsIsoDate(text) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

        /// <summary>True for text starting with an ISO 8601 date (yyyy-MM-dd).</summary>
        private static bool IsIsoDate(string text) =>
            text.Length >= 10 && char.IsAsciiDigit(text[0]) && char.IsAsciiDigit(text[1]) && char.IsAsciiDigit(text[2]) && char.IsAsciiDigit(text[3])
            && text[4] == '-' && char.IsAsciiDigit(text[5]) && char.IsAsciiDigit(text[6]) && text[7] == '-' && char.IsAsciiDigit(text[8]) && char.IsAsciiDigit(text[9]);

        internal static TimeSpan ParseTimeSpan(string text) =>
            text.StartsWith('P') || text.StartsWith("-P", StringComparison.Ordinal)
                ? XmlConvert.ToTimeSpan(text)
                : TimeSpan.Parse(text, CultureInfo.InvariantCulture);

        internal static DateOnly ParseDateOnly(string text) =>
            DateOnly.Parse(text, IsIsoDate(text) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

        internal static TimeOnly ParseTimeOnly(string text) =>
            TimeOnly.Parse(text, IsIsoTime(text) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);

        /// <summary>True for an ISO 8601 time (HH:mm, optionally followed by seconds and fractions).</summary>
        private static bool IsIsoTime(string text) =>
            text.Length >= 5 && char.IsAsciiDigit(text[0]) && char.IsAsciiDigit(text[1]) && text[2] == ':'
            && char.IsAsciiDigit(text[3]) && char.IsAsciiDigit(text[4]) && (text.Length == 5 || text[5] == ':');
    }
}
