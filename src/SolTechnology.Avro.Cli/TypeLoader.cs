using System.Reflection;
using System.Runtime.Loader;

namespace SolTechnology.Avro.Cli;

/// <summary>Loads a type from a built assembly (and its dependencies, through the .deps.json next to it).</summary>
internal static class TypeLoader
{
    internal static Type Load(string assemblyPath, string typeName)
    {
        var path = Path.GetFullPath(assemblyPath);
        if (!File.Exists(path))
        {
            throw new CliException($"Assembly '{assemblyPath}' does not exist. Point --assembly to the built .dll (e.g. bin/Release/net10.0/MyApp.dll).");
        }

        var context = new PluginContext(path);
        var assembly = context.LoadFromAssemblyPath(path);

        var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (type != null)
        {
            return type;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types.Where(t => t != null).ToArray()!;
        }

        var matches = types.Where(t => t.Name == typeName || t.FullName == typeName).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new CliException($"Type '{typeName}' was not found in {Path.GetFileName(path)}."),
            _ => throw new CliException($"Type '{typeName}' is ambiguous in {Path.GetFileName(path)}: {string.Join(", ", matches.Select(t => t.FullName))}. Use the full name.")
        };
    }

    private sealed class PluginContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: false)
    {
        private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // AvroConvert itself comes from the tool, so attributes like [AvroUnion] are the ones the tool understands.
            if (assemblyName.Name == typeof(AvroConvert).Assembly.GetName().Name)
            {
                return typeof(AvroConvert).Assembly;
            }

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
        }
    }
}

/// <summary>An error with a message meant for the user; printed without a stack trace.</summary>
internal sealed class CliException(string message) : Exception(message);
