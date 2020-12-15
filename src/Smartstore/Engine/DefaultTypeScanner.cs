using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;

namespace Smartstore.Engine
{
    /// <inheritdoc/>
    public class DefaultTypeScanner : ITypeScanner
    {
        private readonly bool _ignoreReflectionErrors = true;
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

            var result = new List<Type>();

            try
            {
                foreach (var asm in assemblies)
                {
                    Type[] types = null;
                    try
                    {
                        types = asm.GetExportedTypes();
                    }
                    catch
                    {
                        // Some assemblies don't allow getting types
                        if (!_ignoreReflectionErrors)
                        {
                            throw;
                        }
                    }

                    if (types == null)
                        continue;

                    foreach (var t in types)
                    {
                        if (baseType.IsAssignableFrom(t) || (baseType.IsGenericTypeDefinition && DoesTypeImplementOpenGeneric(t, baseType)))
                        {
                            if (t.IsInterface)
                                continue;

                            if (concreteTypesOnly)
                            {
                                if (t.IsClass && !t.IsAbstract)
                                {
                                    result.Add(t);
                                }
                            }
                            else
                            {
                                result.Add(t);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.Error(ex);

                var msg = string.Empty;
                foreach (var e in ex.LoaderExceptions)
                {
                    msg += e.Message + Environment.NewLine;
                }

                var fail = new Exception(msg, ex);
                throw fail;
            }

            return result;
        }

        protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
        {
            try
            {
                var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
                foreach (var implementedInterface in type.FindInterfaces((objType, objCriteria) => true, null))
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    var isMatch = genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition());
                    return isMatch;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
