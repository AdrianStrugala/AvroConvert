using System;
using System.Collections.Concurrent;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>
    /// Process-wide cache of compiled write plans. Schemas are keyed by reference: instances come from the per-type and
    /// per-text schema caches, so the same logical schema is the same object.
    /// </summary>
    internal static class WritePlanCache
    {
        private static readonly ConcurrentDictionary<(TypeSchema Schema, Type Type, OptionsKey Options), Encoder.WriteItem> Plans = new();
        private const int Limit = 1024;

        internal static Encoder.WriteItem Get(TypeSchema schema, Type type, AvroConvertOptions options)
        {
            var key = (schema, type, OptionsKey.From(options));
            if (Plans.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var plan = new WriteCompiler(options).Compile(schema, type);
            if (Plans.Count >= Limit)
            {
                Plans.Clear();
            }
            Plans.TryAdd(key, plan);
            return plan;
        }
    }
}
