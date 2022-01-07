using System.Reflection;

namespace Smartstore
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Safely returns the set of loadable types from an assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly from which to load types.
        /// </param>
        /// <returns>
        /// The set of types from the assembly, or the subset of types that could be loaded
        /// if there was any error.
        /// </returns>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            Guard.NotNull(assembly, nameof(assembly));

            try
            {
                return assembly.DefinedTypes.Select(x => x.AsType());
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }
    }
}
