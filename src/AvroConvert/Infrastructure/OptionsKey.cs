using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SolTechnology.Avro.Converters;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.Infrastructure
{
    /// <summary>
    /// Behaviour-affecting part of <see cref="AvroConvertOptions"/>, usable as a cache key. The naming policy is compared
    /// with its own <see cref="object.Equals(object)"/> (reference equality unless the policy overrides it, as
    /// <see cref="JavaAvroNamingPolicy"/> does) and converters by instance: two differently configured instances of the
    /// same class must not share a schema or a compiled plan. Reusing the same instances across calls keeps the caches warm.
    /// </summary>
    internal readonly record struct OptionsKey(
        bool IncludeOnlyDataContractMembers,
        int MaxItemsInSchemaTree,
        AvroNumberHandling NumberHandling,
        AvroMissingFieldHandling MissingFieldHandling,
        AvroNullValueHandling NullValueHandling,
        IAvroNamingPolicy NamingPolicy,
        ConverterSet Converters,
        AvroTypeRepresentation? DateTimeRepresentation)
    {
        internal static readonly OptionsKey Default = From(null);

        internal static OptionsKey From(AvroConvertOptions options)
        {
            if (options == null)
            {
                return new OptionsKey(false, 1024, AvroNumberHandling.Strict, AvroMissingFieldHandling.Throw, AvroNullValueHandling.Throw, null, null, null);
            }

            return new OptionsKey(
                options.IncludeOnlyDataContractMembers,
                options.MaxItemsInSchemaTree,
                options.NumberHandling,
                options.MissingFieldHandling,
                options.NullValueHandling,
                options.NamingPolicy,
                options.AvroConverters.Count == 0 ? null : new ConverterSet(options.AvroConverters),
                options.DateTimeRepresentation);
        }
    }

    /// <summary>The converters of an options instance, equal when they are the same instances in the same order.</summary>
    internal sealed class ConverterSet : IEquatable<ConverterSet>
    {
        private readonly IAvroConverter[] _converters;

        internal ConverterSet(IEnumerable<IAvroConverter> converters)
        {
            _converters = converters.ToArray();
        }

        public bool Equals(ConverterSet other)
        {
            if (other is null || other._converters.Length != _converters.Length)
            {
                return false;
            }

            for (int i = 0; i < _converters.Length; i++)
            {
                if (!ReferenceEquals(_converters[i], other._converters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ConverterSet);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var converter in _converters)
            {
                hash.Add(RuntimeHelpers.GetHashCode(converter));
            }

            return hash.ToHashCode();
        }
    }
}
