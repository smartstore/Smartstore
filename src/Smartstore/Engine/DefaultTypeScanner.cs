using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;

namespace Smartstore.Engine
{
    /// <inheritdoc/>
    public class DefaultTypeScanner : ITypeScanner
    {
        private readonly IModuleCatalog _moduleCatalog;
        private HashSet<Assembly> _activeAssemblies = new();

        public DefaultTypeScanner(IModuleCatalog moduleCatalog, ILogger logger, params Assembly[] assemblies)
        {
            Guard.NotNull(moduleCatalog, nameof(moduleCatalog));
            Guard.NotNull(logger, nameof(logger));

            // TODO: (core) Impl > PluginManager stuff etc.

            Logger = logger;

            _moduleCatalog = moduleCatalog;
            AddAssemblies(assemblies);
        }

        public ILogger Logger
        {
            get;
            set;
        } = NullLogger.Instance;

        /// <inheritdoc/>
        public void AddAssemblies(params Assembly[] assemblies)
        {
            var newSet = new HashSet<Assembly>(Assemblies ?? Enumerable.Empty<Assembly>());
            newSet.AddRange(assemblies);
            Assemblies = newSet.AsReadOnly();

            _activeAssemblies.AddRange(assemblies.Where(x => _moduleCatalog.IsActiveModuleAssembly(x)));
        }

        /// <inheritdoc/>
        public IEnumerable<Assembly> Assemblies { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<Type> FindTypes(Type baseType, bool concreteTypesOnly = true, bool ignoreInactiveModules = false)
        {
            Guard.NotNull(baseType, nameof(baseType));

            var assemblies = ignoreInactiveModules ? _activeAssemblies : Assemblies;
            return FindTypes(baseType, assemblies, concreteTypesOnly);
        }

        /// <inheritdoc/>
        public IEnumerable<Type> FindTypes(Type baseType, IEnumerable<Assembly> assemblies, bool concreteTypesOnly = true)
        {
            Guard.NotNull(baseType, nameof(baseType));

            foreach (var t in assemblies.SelectMany(x => x.GetLoadableTypes()))
            {
                if (t.IsInterface || t.IsDelegate() || t.IsCompilerGenerated())
                    continue;

                if (baseType.IsAssignableFrom(t) || t.IsOpenGenericTypeOf(baseType))
                {
                    if (concreteTypesOnly)
                    {
                        if (t.IsClass && !t.IsAbstract)
                        {
                            yield return t;
                        }
                    }
                    else
                    {
                        yield return t;
                    }
                }
            }
        }
    }
}
