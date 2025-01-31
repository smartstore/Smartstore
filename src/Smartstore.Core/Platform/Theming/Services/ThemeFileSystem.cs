#nullable enable

using Microsoft.Extensions.Primitives;
using Smartstore.IO;

namespace Smartstore.Core.Theming
{
    public class ThemeFileSystem : CompositeFileSystem
    {
        const string ThemeConfigFileName = "theme.config";

        private readonly IFileSystem _themeRoot;
        private readonly bool _isDerived;

        public ThemeFileSystem(IFileSystem themeRoot, IFileSystem? baseThemeRoot)
            : base(CreateFileSystems(themeRoot, baseThemeRoot))
        {
            _themeRoot = themeRoot;
            _isDerived = baseThemeRoot != null;
        }

        private static IFileSystem[] CreateFileSystems(IFileSystem themeRoot, IFileSystem? baseThemeRoot)
        {
            return baseThemeRoot == null ? [themeRoot] : [themeRoot, baseThemeRoot];
        }

        public override IChangeToken Watch(string pattern)
        {
            if (!_isDerived || string.IsNullOrEmpty(pattern) || pattern == ThemeConfigFileName)
            {
                // Delegate to base if theme is not derived (nothing special to consider here), or...
                // Watch every theme.config in hierarchy chain, because a config
                // change in a theme affects all its children.
                return base.Watch(pattern);
            }

            if (!IsInheritableFile(pattern))
            {
                // Nothing special to consider if wildcard or directory
                return base.Watch(pattern);
            }

            // If a file is being added to a derived theme's directory, any base file
            // needs to be refreshed/cancelled. This is necessary, because the new file 
            // overwrites the base file now, and RazorViewEngine/SassParser must be notified
            // about this change.
            if (_themeRoot.FileExists(pattern))
            {
                // If file is inherited then only watch inherited file:
                // a change in a base file does not affect theme.
                return _themeRoot.Watch(pattern);
            }
            else
            {
                // If file does not exist in this theme, then watch the whole chain.
                // Only then we are notified about a new derived file in this theme.
                return base.Watch(pattern);
            }
        }

        private static bool IsInheritableFile(string pattern)
        {
            var isWildCard = pattern.IndexOf('*') != -1;
            if (isWildCard)
            {
                return false;
            }

            return !IsDirectoryPath(pattern);
        }


        private static bool IsDirectoryPath(string path)
        {
            return path.Length > 0 &&
                (path[^1] == Path.AltDirectorySeparatorChar ||
                path[^1] == Path.DirectorySeparatorChar);
        }
    }
}
