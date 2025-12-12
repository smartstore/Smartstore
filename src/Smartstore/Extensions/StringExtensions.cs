#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Smartstore;

public static partial class StringExtensions
{
    private const string DumpStr = "------------------------------------------------";

    public const string NotAvailable = "n/a";

    private delegate void ActionLine(TextWriter textWriter, string line);

    extension(string? value)
    {
        [DebuggerStepThrough]
        public string ToSafe(string? defaultValue = null)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return (defaultValue ?? string.Empty);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(value))]
        public string? TrimSafe()
        {
            return !string.IsNullOrEmpty(value) ? value.Trim() : value;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EmptyNull()
        {
            return value ?? string.Empty;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(value))]
        public string? NullEmpty()
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dump(bool appendMarks = false)
        {
            Debug.WriteLine(value);
            Debug.WriteLineIf(appendMarks, DumpStr);
        }

        /// <summary>
        /// Returns n/a if string is empty else self.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string NaIfEmpty()
        {
            return string.IsNullOrWhiteSpace(value) ? NotAvailable : value;
        }

        /// <summary>
        /// Returns <paramref name="value"/> if it is not null or empty, otherwise returns <paramref name="defaultValue"/>
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrDefault(string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}