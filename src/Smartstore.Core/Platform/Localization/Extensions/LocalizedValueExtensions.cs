using System.Runtime.CompilerServices;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Wrapper for the most common string extension helpers used in views.
    /// Just here to avoid runtime exceptions in views after refactoring GetLocalized() helper.
    /// </summary>
    public static class LocalizedValueExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(this LocalizedValue<string> value) => value?.Value?.HasValue() == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this LocalizedValue<string> value) => value?.Value?.IsEmpty() == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Truncate(this LocalizedValue<string> value, int maxLength, string suffix = "") => value?.Value?.Truncate(maxLength, suffix);
    }
}
