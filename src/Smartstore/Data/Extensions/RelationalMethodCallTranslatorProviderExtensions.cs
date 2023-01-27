using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Smartstore
{
    public static class RelationalMethodCallTranslatorProviderExtensions
    {
        private static readonly FieldInfo _translatorsField = typeof(RelationalMethodCallTranslatorProvider)
            .GetField("_translators", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _pluginsField = typeof(RelationalMethodCallTranslatorProvider)
            .GetField("_plugins", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Gets a list of all registered translator instances.
        /// </summary>
        public static List<IMethodCallTranslator> GetTranslators(this RelationalMethodCallTranslatorProvider provider)
        {
            Guard.NotNull(provider);
            return _translatorsField.GetValue(provider) as List<IMethodCallTranslator>;
        }

        /// <summary>
        /// Gets a list of all registered plugin instances.
        /// </summary>
        public static List<IMethodCallTranslator> GetPlugins(this RelationalMethodCallTranslatorProvider provider)
        {
            Guard.NotNull(provider);
            return _pluginsField.GetValue(provider) as List<IMethodCallTranslator>;
        }
    }
}
