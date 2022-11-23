#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        private const string DumpStr = "------------------------------------------------";

        public const string NotAvailable = "n/a";

        private delegate void ActionLine(TextWriter textWriter, string line);

        [DebuggerStepThrough]
        public static string ToSafe(this string? value, string? defaultValue = null)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return (defaultValue ?? string.Empty);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? TrimSafe(this string? value)
        {
            return !string.IsNullOrEmpty(value) ? value.Trim() : value;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EmptyNull(this string? value)
        {
            return value ?? string.Empty;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? NullEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dump(this string? value, bool appendMarks = false)
        {
            Debug.WriteLine(value);
            Debug.WriteLineIf(appendMarks, DumpStr);
        }

        /// <summary>
        /// Returns n/a if string is empty else self.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NaIfEmpty(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? NotAvailable : value;
        }
    }
}