using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Smartstore.Engine
{
    /// <summary>
    /// Provides type discovery. 
    /// </summary>
    public interface ITypeScanner
    {
        /// <summary>
        /// Adds one or more assemblies to the list of scannable assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        void AddAssemblies(params Assembly[] assemblies);

        /// <summary>
        /// Gets all registered scannable assemblies (core & all modules)
        /// </summary>
        IEnumerable<Assembly> Assemblies { get; }

        /// <summary>
        /// Find all types that are subclasses of <paramref name="baseType"/> in all scannable <see cref="Assemblies"/>.
        /// </summary>
        /// <param name="baseType">The base type to check for.</param>
        /// <param name="concreteTypesOnly">Whether abstract types should be skipped.</param>
        /// <param name="ignoreInactiveModules">Specifies whether inactive module's assemblies should be ignored.</param>
        /// <returns>Matching types</returns>
        IEnumerable<Type> FindTypes(Type baseType, bool concreteTypesOnly = true, bool ignoreInactiveModules = false);

        /// <summary>
        /// Find all types that are subclasses of <paramref name="baseType"/> in all passed <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="baseType">The base type to check for.</param>
        /// <param name="concreteTypesOnly">Whether abstract types should be skipped.</param>
        /// <param name="assemblies">Assemblies to scan.</param>
        /// <returns>Matching types</returns>
        IEnumerable<Type> FindTypes(Type baseType, IEnumerable<Assembly> assemblies, bool concreteTypesOnly = true);
    }

    public static class ITypeScannerExtensions
    {
        /// <summary>
        /// Find all types that are subclasses of <typeparamref name="T"/> in all known assemblies.
        /// </summary>
        /// <typeparam name="T">The base type to check for</typeparam>
        /// <param name="concreteTypesOnly">Whether abstract types should be skipped.</param>
        /// <param name="ignoreInactiveModules">Speciifies whether inactive module's assemblies should be ignored.</param>
        /// <returns>Matching types</returns>
        public static IEnumerable<Type> FindTypes<T>(this ITypeScanner scanner, bool concreteTypesOnly = true, bool ignoreInactiveModules = false)
        {
            return scanner.FindTypes(typeof(T), concreteTypesOnly, ignoreInactiveModules);
        }

        /// <summary>
        /// Find all types that are subclasses of <typeparamref name="T"/> in all passed <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="baseType">The base type to check for.</param>
        /// <param name="concreteTypesOnly">Whether abstract types should be skipped.</param>
        /// <param name="assemblies">Assemblies to scan.</param>
        /// <returns>Matching types</returns>
        public static IEnumerable<Type> FindTypes<T>(this ITypeScanner scanner, IEnumerable<Assembly> assemblies, bool concreteTypesOnly = true)
        {
            return scanner.FindTypes(typeof(T), assemblies, concreteTypesOnly);
        }
    }

    public class NullTypeScanner : ITypeScanner
    {
        public static ITypeScanner Instance { get; } = new NullTypeScanner();

        public void AddAssemblies(params Assembly[] assemblies)
        {
        }
        public IEnumerable<Assembly> Assemblies { get; } = Enumerable.Empty<Assembly>();

        public IEnumerable<Type> FindTypes(Type baseType, bool concreteTypesOnly = true, bool ignoreInactiveModules = false)
            => Enumerable.Empty<Type>();

        public IEnumerable<Type> FindTypes(Type baseType, IEnumerable<Assembly> assemblies, bool concreteTypesOnly = true)
            => Enumerable.Empty<Type>();
    }
}