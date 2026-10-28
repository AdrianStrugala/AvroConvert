using System;
using System.Collections.Concurrent;
using System.Threading;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure;
using System.Diagnostics.CodeAnalysis;

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
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static Action<Writer, T> GetTyped<T>(TypeSchema schema, AvroConvertOptions options)
        {
            var key = (schema, typeof(T), OptionsKey.From(options));
            if (TypedPlans.TryGetValue(key, out var cached))
            {
                return (Action<Writer, T>)cached;
            }

            var plan = new WriteCompiler(options).CompileTyped<T>(schema);
            if (TypedPlans.Count >= Limit)
            {
                TypedPlans.Clear();
            }
            TypedPlans.TryAdd(key, plan);
            return plan;
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static Encoder.WriteItem Get(TypeSchema schema, Type type, AvroConvertOptions options)
        {
            // Hot path: the same (schema, type, options) as the previous call, compared by reference.
            var optionsKey = OptionsKey.From(options);
            var last = Volatile.Read(ref _last);
            if (last != null && ReferenceEquals(last.Schema, schema) && ReferenceEquals(last.Type, type) && last.Options.Equals(optionsKey))
            {
                return last.Plan;
            }

            var plan = GetShared(schema, type, options, optionsKey);
            Volatile.Write(ref _last, new LastPlan(schema, type, optionsKey, plan));
            return plan;
        }

        private static LastPlan _last;

        private sealed record LastPlan(TypeSchema Schema, Type Type, OptionsKey Options, Encoder.WriteItem Plan);

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static Encoder.WriteItem GetShared(TypeSchema schema, Type type, AvroConvertOptions options, OptionsKey optionsKey)
        {
            var key = (schema, type, optionsKey);
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
