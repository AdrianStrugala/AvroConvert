using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace AvroConvertComponentTests.Infrastructure;

public static class AssertExtensions
{
    /// <summary>
    /// Asserts that two JSON strings are structurally equal.
    /// If they are not, a detailed diff message is provided.
    /// </summary>
    /// <param name="expected">The expected JSON string.</param>
    /// <param name="actual">The actual JSON string.</param>
    public static void JsonEqual(string expected, string actual)
    {
        string diff = GetJsonDiff(expected, actual);
        Assert.True(string.IsNullOrEmpty(diff), diff);
    }

    private static string GetJsonDiff(string expected, string actual)
    {
        try
        {
            using JsonDocument expectedDoc = JsonDocument.Parse(expected);
            using JsonDocument actualDoc = JsonDocument.Parse(actual);
            var differences = new List<string>();
            CompareJsonElements(expectedDoc.RootElement, actualDoc.RootElement, "$", differences);
            return differences.Any() ? string.Join(Environment.NewLine, differences) : string.Empty;
        }
        catch (Exception ex)
        {
            return $"Error parsing JSON: {ex.Message}";
        }
    }

    private static void CompareJsonElements(JsonElement expected, JsonElement actual, string path, List<string> differences)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            differences.Add($"path: {path}: Expected type {expected.ValueKind} but got {actual.ValueKind}");
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                // Convert object properties to dictionaries for easier lookup.
                var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                // Check for missing or mismatched properties.
                foreach (var kvp in expectedProps)
                {
                    if (!actualProps.TryGetValue(kvp.Key, out var actualVal))
                    {
                        differences.Add($"path: {path}: Missing property '{kvp.Key}'");
                    }
                    else
                    {
                        CompareJsonElements(kvp.Value, actualVal, $"{path}.{kvp.Key}", differences);
                    }
                }
                // Check for unexpected properties.
                foreach (var key in actualProps.Keys)
                {
                    if (!expectedProps.ContainsKey(key))
                    {
                        differences.Add($"path: {path}: Unexpected property '{key}'");
                    }
                }
                break;

            case JsonValueKind.Array:
                var expectedArray = expected.EnumerateArray().ToList();
                var actualArray = actual.EnumerateArray().ToList();
                if (expectedArray.Count != actualArray.Count)
                {
                    differences.Add($"path: {path}: Array length mismatch. Expected {expectedArray.Count} elements but got {actualArray.Count}");
                }
                int minCount = Math.Min(expectedArray.Count, actualArray.Count);
                for (int i = 0; i < minCount; i++)
                {
                    CompareJsonElements(expectedArray[i], actualArray[i], $"{path}[{i}]", differences);
                }
                break;

            default:
                // For primitives, compare their string representations.
                string expectedPrimitive = expected.ToString();
                string actualPrimitive = actual.ToString();
                if (!string.Equals(expectedPrimitive, actualPrimitive, StringComparison.Ordinal))
                {
                    differences.Add($"path: {path}: Value mismatch. Expected '{expectedPrimitive}' but got '{actualPrimitive}'");
                }
                break;
        }
    }
}