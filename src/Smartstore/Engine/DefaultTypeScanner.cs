using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;

namespace Smartstore.Engine;

/// <inheritdoc/>
public class DefaultTypeScanner : ITypeScanner
{
    public DefaultTypeScanner(params Assembly[] assemblies)
    {
        // No edit allowed from now on
        Assemblies = assemblies.ToImmutableArray();
    }

    public DefaultTypeScanner(IEnumerable<Assembly> coreAssemblies, IModuleCatalog moduleCatalog, ILogger logger)
    {
        Guard.NotNull(coreAssemblies);
        Guard.NotNull(moduleCatalog);
        Guard.NotNull(logger);

        Logger = logger;

        // No edit allowed from now on
        Assemblies = coreAssemblies
            // Add all module assemblies to assemblies list
            .Concat(moduleCatalog.GetInstalledModules().Select(x => x.Module.Assembly))
            // Remove dupes, Keep first occurrence = preserve order
            .Distinct()
            .ToImmutableArray();
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<Type> FindTypes(Type baseType, bool concreteTypesOnly = true)
    {
        Guard.NotNull(baseType);

        return FindTypes(baseType, Assemblies, concreteTypesOnly);
    }

    /// <inheritdoc/>
    public IEnumerable<Type> FindTypes(Type baseType, IEnumerable<Assembly> assemblies, bool concreteTypesOnly = true)
    {
        Guard.NotNull(baseType);

        var isOpenGeneric = baseType.IsGenericTypeDefinition;

        foreach (var t in assemblies.SelectMany(x => x.GetLoadableTypes()))
        {
            if (t.IsInterface)
                continue;

            // INFO (perf): scanning is 2x faster without these extra checks.
            //if (t.IsInterface || t.IsCompilerGenerated() || t.IsRazorCompiledItem() || t.IsDelegate())
            //    continue;

            var isCandidate = (!concreteTypesOnly || !t.IsAbstract) && (isOpenGeneric
                ? t.IsClosedGenericTypeOf(baseType)
                : baseType.IsAssignableFrom(t));

            if (isCandidate)
            {
                yield return t;
            }
        }
    }
}