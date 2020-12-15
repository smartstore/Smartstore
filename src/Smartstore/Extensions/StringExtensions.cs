using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Smartstore
{
    public static partial class StringExtensions
	{
		public const string CarriageReturnLineFeed = "\r\n";
		public const string Empty = "";
		public const char CarriageReturn = '\r';
		public const char LineFeed = '\n';
		public const char Tab = '\t';

		private delegate void ActionLine(TextWriter textWriter, string line);

		[DebuggerStepThrough]
		public static string ToSafe(this string value, string defaultValue = null)
		{
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}

			return (defaultValue ?? string.Empty);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string TrimSafe(this string value)
		{
			return (!string.IsNullOrEmpty(value) ? value.Trim() : value);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string EmptyNull(this string value)
		{
			return (value ?? string.Empty).Trim();
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string NullEmpty(this string value)
		{
			return (string.IsNullOrEmpty(value)) ? null : value;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Dump(this string value, bool appendMarks = false)
		{
			Debug.WriteLine(value);
			Debug.WriteLineIf(appendMarks, "------------------------------------------------");
		}

		/// <summary>
		/// Returns n/a if string is empty else self.
		/// </summary>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string NaIfEmpty(this string value)
		{
			return (string.IsNullOrWhiteSpace(value) ? "n/a" : value);
		}
	}
}