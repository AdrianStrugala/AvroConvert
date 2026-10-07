using System;
using System.Linq;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.Infrastructure
{
    /// <summary>
    /// Behaviour-affecting part of <see cref="AvroConvertOptions"/>, usable as a cache key. Naming policy and converters
    /// are keyed by their types (they are expected to be stateless) so caches cannot grow with every options instance.
    /// </summary>
    internal readonly record struct OptionsKey(
        bool IncludeOnlyDataContractMembers,
        int MaxItemsInSchemaTree,
        AvroNumberHandling NumberHandling,
        AvroMissingFieldHandling MissingFieldHandling,
        Type NamingPolicy,
        string Converters)
    {
        internal static readonly OptionsKey Default = From(null);

        internal static OptionsKey From(AvroConvertOptions options)
        {
            if (options == null)
            {
                return new OptionsKey(false, 1024, AvroNumberHandling.Strict, AvroMissingFieldHandling.Throw, null, null);
            }

            return new OptionsKey(
                options.IncludeOnlyDataContractMembers,
                options.MaxItemsInSchemaTree,
                options.NumberHandling,
                options.MissingFieldHandling,
                options.NamingPolicy?.GetType(),
                options.AvroConverters.Count == 0
                    ? null
                    : string.Join("|", options.AvroConverters.Select(c => c.GetType().AssemblyQualifiedName)));
        }
    }
}
