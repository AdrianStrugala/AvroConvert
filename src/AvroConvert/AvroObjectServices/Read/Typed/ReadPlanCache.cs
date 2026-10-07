using System;
using System.Collections.Concurrent;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    internal sealed class ReadPlan<T>
    {
        /// <summary>Reads a single T.</summary>
        internal Func<IReader, T> One { get; init; }

        /// <summary>Reads <c>count</c> container entries into T; null when T is not a collection.</summary>
        internal Func<IReader, long, T> Many { get; init; }
    }

    /// <summary>
    /// Process-wide cache of compiled read plans. Schemas are keyed by reference: instances come from the per-type and
    /// per-text schema caches, so the same logical schema is the same object.
    /// </summary>
    internal static class ReadPlanCache
    {
        private static readonly ConcurrentDictionary<(TypeSchema Writer, TypeSchema Reader, Type Target, OptionsKey Options), object> Plans = new();
        private const int Limit = 1024;

        internal static ReadPlan<T> Get<T>(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options)
        {
            var key = (writerSchema, readerSchema, typeof(T), OptionsKey.From(options));
            if (Plans.TryGetValue(key, out var cached))
            {
                return (ReadPlan<T>)cached;
            }

            var compiler = new ReadCompiler(writerSchema, readerSchema, options);
            var plan = new ReadPlan<T>
            {
                One = compiler.CompileOne<T>(writerSchema, readerSchema),
                Many = compiler.CompileMany<T>(writerSchema, readerSchema)
            };

            if (Plans.Count >= Limit)
            {
                Plans.Clear();
            }
            Plans.TryAdd(key, plan);
            return plan;
        }
    }
}
