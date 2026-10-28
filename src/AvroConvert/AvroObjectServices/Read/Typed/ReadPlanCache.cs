using System;
using System.Collections.Concurrent;
using System.Threading;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    internal sealed class ReadPlan<T>
    {
        /// <summary>Reads a single T.</summary>
        internal Func<Reader, T> One { get; init; }

        /// <summary>Reads <c>count</c> container entries into T; null when T is not a collection.</summary>
        internal Func<Reader, long, T> Many { get; init; }

        /// <summary>Block-wise variant of <see cref="Many"/>: create an accumulator, append each block, finish into T.</summary>
        internal ManyPlan<T> Blocks { get; init; }
    }

    internal sealed class ManyPlan<T>
    {
        internal Func<object> NewAccumulator { get; init; }
        internal Action<Reader, long, object> Append { get; init; }
        internal Func<object, T> Finish { get; init; }
    }

    /// <summary>
    /// Process-wide cache of compiled read plans. Schemas are keyed by reference: instances come from the per-type and
    /// per-text schema caches, so the same logical schema is the same object.
    /// </summary>
    internal static class ReadPlanCache
    {
        private static readonly ConcurrentDictionary<(TypeSchema Writer, TypeSchema Reader, Type Target, OptionsKey Options), object> Plans = new();
        private const int Limit = 1024;

        /// <summary>The last plan used for <typeparamref name="T"/>: a consumer reads one type with the same schemas for every message.</summary>
        private static class Last<T>
        {
            internal static LastPlan<T> Entry;
        }

        private sealed record LastPlan<T>(TypeSchema Writer, TypeSchema Reader, OptionsKey Options, ReadPlan<T> Plan);

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static ReadPlan<T> Get<T>(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options)
        {
            var optionsKey = options == null ? OptionsKey.Default : OptionsKey.From(options);
            var last = Volatile.Read(ref Last<T>.Entry);
            if (last != null && ReferenceEquals(last.Writer, writerSchema) && ReferenceEquals(last.Reader, readerSchema) && last.Options.Equals(optionsKey))
            {
                return last.Plan;
            }

            var plan = GetShared<T>(writerSchema, readerSchema, options, optionsKey);
            Volatile.Write(ref Last<T>.Entry, new LastPlan<T>(writerSchema, readerSchema, optionsKey, plan));
            return plan;
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static ReadPlan<T> GetShared<T>(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options, OptionsKey optionsKey)
        {
            var key = (writerSchema, readerSchema, typeof(T), optionsKey);
            if (Plans.TryGetValue(key, out var cached))
            {
                return (ReadPlan<T>)cached;
            }

            var compiler = new ReadCompiler(options);
            var blocks = compiler.CompileMany<T>(writerSchema, readerSchema);
            var plan = new ReadPlan<T>
            {
                One = compiler.CompileOne<T>(writerSchema, readerSchema),
                Blocks = blocks,
                Many = blocks == null
                    ? null
                    : (reader, count) =>
                    {
                        var accumulator = blocks.NewAccumulator();
                        blocks.Append(reader, count, accumulator);
                        return blocks.Finish(accumulator);
                    }
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
