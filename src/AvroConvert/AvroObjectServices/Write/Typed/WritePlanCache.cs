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
        private static readonly ConcurrentDictionary<(TypeSchema Schema, Type Type, OptionsKey Options), Delegate> TypedPlans = new();
        private const int Limit = 1024;

        /// <summary>Strongly typed writer for values whose static type is exactly T (no boxing of the root value).</summary>
        internal static Action<IWriter, T> GetTyped<T>(TypeSchema schema, AvroConvertOptions options)
        {
            var key = (schema, typeof(T), OptionsKey.From(options));
            if (TypedPlans.TryGetValue(key, out var cached))
            {
                return (Action<IWriter, T>)cached;
            }

            var plan = new WriteCompiler(options).CompileTyped<T>(schema);
            if (TypedPlans.Count >= Limit)
            {
                TypedPlans.Clear();
            }
            TypedPlans.TryAdd(key, plan);
            return plan;
        }

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
