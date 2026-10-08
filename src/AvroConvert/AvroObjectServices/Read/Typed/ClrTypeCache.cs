using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SolTechnology.Avro.AvroObjectServices.Schemas;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>
    /// For dynamic deserialization: finds a loaded CLR type whose full name (or simple name) matches a record schema.
    /// </summary>
    internal static class ClrTypeCache
    {
        private static readonly ConcurrentDictionary<string, Type> BySchemaName = new();

        internal static Type Find(RecordSchema schema) =>
            BySchemaName.GetOrAdd(schema.FullName, _ => Search(schema));

        private static Type Search(RecordSchema schema)
        {
            Assembly[] preferred = { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() };
            return Find(preferred) ?? Find(AppDomain.CurrentDomain.GetAssemblies());

            Type Find(Assembly[] assemblies)
            {
                var types = assemblies.Where(a => a != null).SelectMany(SafeGetTypes).ToList();
                return types.FirstOrDefault(t => t.FullName == schema.FullName)
                       ?? types.FirstOrDefault(t => t.Name == schema.Name);
            }

            static IEnumerable<Type> SafeGetTypes(Assembly assembly)
            {
                try { return assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            }
        }
    }
}
