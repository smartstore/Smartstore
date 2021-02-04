using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore
{
    public static partial class StringExtensions
    {
		/// <summary>
		/// Formats a string to an invariant culture
		/// </summary>
		/// <param name="format">The format string.</param>
		/// <param name="objects">The objects.</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatInvariant(this string format, params object[] objects)
		{
			return string.Format(CultureInfo.InvariantCulture, format, objects);
		}

		/// <summary>
		/// Formats a string to the current culture.
		/// </summary>
		/// <param name="format">The format string.</param>
		/// <param name="objects">The objects.</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatCurrent(this string format, params object[] objects)
		{
			return string.Format(CultureInfo.CurrentCulture, format, objects);
		}

		/// <summary>
		/// Formats a string to the current UI culture.
		/// </summary>
		/// <param name="format">The format string.</param>
		/// <param name="objects">The objects.</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatCurrentUI(this string format, params object[] objects)
		{
			return string.Format(CultureInfo.CurrentUICulture, format, objects);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string FormatWith(this string format, params object[] args)
		{
			return FormatWith(format, CultureInfo.CurrentCulture, args);
		}

		[DebuggerStepThrough]
		public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
		{
			return string.Format(provider, format, args);
		}

		/// <summary>
		/// Mask by replacing characters with asterisks.
		/// </summary>
		/// <param name="value">The string</param>
		/// <param name="length">Number of characters to leave untouched.</param>
		/// <returns>The mask string</returns>
		[DebuggerStepThrough]
		public static string Mask(this string value, int length)
		{
			if (value.HasValue())
				return value.Substring(0, length) + new String('*', value.Length - length);

			return value;
		}

		/// <summary>
		/// Ensures that a string only contains numeric values
		/// </summary>
		/// <param name="str">Input string</param>
		/// <returns>Input string with only numeric values, empty string if input is null or empty</returns>
		[DebuggerStepThrough]
		public static string EnsureNumericOnly(this string str)
		{
			if (string.IsNullOrEmpty(str))
				return string.Empty;

			return new String(str.Where(c => char.IsDigit(c)).ToArray());
		}

		[DebuggerStepThrough]
		public static string Truncate(this string value, int maxLength, string suffix = "")
		{
			if (suffix == null)
				throw new ArgumentNullException(nameof(suffix));

			Guard.IsPositive(maxLength, nameof(maxLength));

			int subStringLength = maxLength - suffix.Length;

			if (subStringLength <= 0)
				throw Error.Argument(nameof(maxLength), "Length of suffix string is greater or equal to maximumLength");

			if (value != null && value.Length > maxLength)
			{
				var truncatedString = value.Substring(0, subStringLength);
				// in case the last character is a space
				truncatedString = truncatedString.Trim();
				truncatedString += suffix;

				return truncatedString;
			}
			else
			{
				return value;
			}
		}

		/// <summary>
		/// Removes all redundant whitespace (empty lines, double space etc.).
		/// Use ~! literal to keep whitespace wherever necessary.
		/// </summary>
		/// <param name="input">Input</param>
		/// <returns>The compacted string</returns>
		public static string Compact(this string input, bool removeEmptyLines = false)
		{
			Guard.NotNull(input, nameof(input));

			using var psb = StringBuilderPool.Instance.Get(out var sb);
			var lines = GetLines(input.Trim(), true, removeEmptyLines).ToArray();

			foreach (var line in lines)
			{
				var len = line.Length;
				var sbLine = StringBuilderPool.Instance.Get();
				var isChar = false;
				var isLiteral = false; // When we detect the ~! literal
				int i = 0;
				var eof = false;

				for (i = 0; i < len; i++)
				{
					var c = line[i];

					eof = i == len - 1;

					if (char.IsWhiteSpace(c))
					{
						// Space, Tab etc.
						if (isChar)
						{
							// If last char not empty, append the space.
							sbLine.Append(' ');
						}

						isLiteral = false;
						isChar = false;
					}
					else
					{
						// Char or Literal (~!)

						isLiteral = c == '~' && !eof && line[i + 1] == '!';
						isChar = true;

						if (isLiteral)
						{
							sbLine.Append(' ');
							i++; // skip next "!" char
						}
						else
						{
							sbLine.Append(c);
						}
					}
				}

				// Append the compacted and trimmed line
				sb.AppendLine(sbLine.ToString().Trim().Trim(','));
				StringBuilderPool.Instance.Return(sbLine);
			}

			return sb.ToString().Trim();
		}

		/// <summary>
		/// Ensure that a string starts with a string.
		/// </summary>
		/// <param name="value">The target string</param>
		/// <param name="startsWith">The string the target string should start with</param>
		/// <returns>The resulting string</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string EnsureStartsWith(this string value, string startsWith)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (startsWith == null)
				throw new ArgumentNullException(nameof(startsWith));

			return value.StartsWith(startsWith) ? value : (startsWith + value);
		}

		/// <summary>
		/// Ensure that a string starts with a given char.
		/// </summary>
		/// <param name="value">The target string</param>
		/// <param name="startsWith">The char the target string should start with</param>
		/// <returns>The resulting string</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string EnsureStartsWith(this string value, char startsWith)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			return value.StartsWith(startsWith) ? value : (startsWith + value);
		}

		/// <summary>
		/// Ensures the target string ends with the specified string.
		/// </summary>
		/// <param name="endsWith">The target.</param>
		/// <param name="value">The value.</param>
		/// <returns>The target string with the value string at the end.</returns>
		[DebuggerStepThrough]
		public static string EnsureEndsWith(this string value, string endsWith)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (endsWith == null)
				throw new ArgumentNullException(nameof(endsWith));

			if (value.Length >= endsWith.Length)
			{
				if (string.Compare(value, value.Length - endsWith.Length, endsWith, 0, endsWith.Length, StringComparison.OrdinalIgnoreCase) == 0)
					return value;

				string trimmedString = value.TrimEnd(null);

				if (string.Compare(trimmedString, trimmedString.Length - endsWith.Length, endsWith, 0, endsWith.Length, StringComparison.OrdinalIgnoreCase) == 0)
					return value;
			}

			return value + endsWith;
		}

		/// <summary>
		/// Ensures the target string ends with the specified char.
		/// </summary>
		/// <param name="endsWith">The char the target string should end with.</param>
		/// <param name="value">The value.</param>
		/// <returns>The target string with the value string at the end.</returns>
		[DebuggerStepThrough]
		public static string EnsureEndsWith(this string value, char endsWith)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			return value.EndsWith(endsWith) ? value : (value + endsWith);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RemoveHtml(this string source)
		{
			return HtmlUtils.StripTags(source).Trim().HtmlDecode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RemoveEncloser(this string value, string encloser)
		{
			return value.RemoveEncloser(encloser, StringComparison.CurrentCulture);
		}

		public static string RemoveEncloser(this string value, string encloser, StringComparison comparisonType)
		{
			if (value.IsEnclosedIn(encloser, comparisonType))
			{
				int len = encloser.Length / 2;
				return value.Substring(
					len,
					value.Length - (len * 2));
			}

			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RemoveEncloser(this string value, string start, string end)
		{
			return value.RemoveEncloser(start, end, StringComparison.CurrentCulture);
		}

		public static string RemoveEncloser(this string value, string start, string end, StringComparison comparisonType)
		{
			if (value.IsEnclosedIn(start, end, comparisonType))
				return value.Substring(
					start.Length,
					value.Length - (start.Length + end.Length));

			return value;
		}

		/// <summary>
		/// Appends a string and uses the delimiter if the string is not empty.
		/// </summary>
		/// <param name="value">Source string.</param>
		/// <param name="strToAppend">The string to be appended.</param>
		/// <param name="delimiter">Delimiter.</param>
		[DebuggerStepThrough]
		public static string Grow(this string value, string strToAppend, string delimiter)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.IsNullOrEmpty(strToAppend) ? string.Empty : strToAppend;
			}

			if (string.IsNullOrEmpty(strToAppend))
			{
				return string.IsNullOrEmpty(value) ? string.Empty : value;
			}

			return value + delimiter + strToAppend;
		}

		/// <summary>
		/// Appends a string and uses the delimiter if the string is not empty.
		/// </summary>
		/// <param name="sb">Target string builder.</param>
		/// <param name="strToAppend">The string to be appended.</param>
		/// <param name="delimiter">Delimiter.</param>
		[DebuggerStepThrough]
		public static void Grow(this StringBuilder sb, string strToAppend, string delimiter)
		{
			Guard.NotNull(delimiter, nameof(delimiter));

			if (!string.IsNullOrWhiteSpace(strToAppend))
			{
				if (sb.Length > 0)
				{
					sb.Append(delimiter);
				}

				sb.Append(strToAppend);
			}
		}

		/// <summary>
		/// Left-pads a string. Always returns empty string if source is null or empty.
		/// </summary>
		[DebuggerStepThrough]
		public static string LeftPad(this string value, string format = null, char pad = ' ', int count = 1)
		{
			if (string.IsNullOrEmpty(value))
				return string.Empty;

			Guard.NotNull(pad, nameof(pad));

			if (count < 1)
				return value;

			var left = new String(pad, count);
			var right = value;

			if (!string.IsNullOrWhiteSpace(format))
			{
				right = string.Format(CultureInfo.InvariantCulture, format, value);
			}

			return left + right;
		}

		/// <summary>
		/// Replaces substring with position x1 to x2 by replaceBy.
		/// </summary>
		[DebuggerStepThrough]
		public static string Replace(this string value, int x1, int x2, string replaceBy = null)
		{
			if (!string.IsNullOrWhiteSpace(value) && x1 > 0 && x2 > x1 && x2 < value.Length)
			{
				return value.Substring(0, x1) + (replaceBy.EmptyNull()) + value[(x2 + 1)..];
			}

			return value;
		}

		[DebuggerStepThrough]
		public static string Replace(this string value, string oldValue, string newValue, StringComparison comparisonType)
		{
			try
			{
				int startIndex = 0;
				while (true)
				{
					startIndex = value.IndexOf(oldValue, startIndex, comparisonType);
					if (startIndex == -1)
						break;

					value = value.Substring(0, startIndex) + newValue + value[(startIndex + oldValue.Length)..];

					startIndex += newValue.Length;
				}
			}
			catch (Exception exc)
			{
				exc.Dump();
			}

			return value;
		}

		/// <summary>
		/// Replaces digits in a string with culture native digits (if digit substitution for culture is required)
		/// </summary>
		[DebuggerStepThrough]
		public static string ReplaceNativeDigits(this string value, IFormatProvider provider = null)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			provider ??= NumberFormatInfo.CurrentInfo;
			var nfi = NumberFormatInfo.GetInstance(provider);

			if (nfi.DigitSubstitution == DigitShapes.None)
			{
				return value;
			}

			var nativeDigits = nfi.NativeDigits;
			var rg = new Regex(@"\d");

			var result = rg.Replace(value, m => nativeDigits[m.Value.ToInt()]);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RegexRemove(this string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
		{
			return Regex.Replace(input, pattern, string.Empty, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RegexReplace(this string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
		{
			return Regex.Replace(input, pattern, replacement, options);
		}

		[DebuggerStepThrough]
		public static string RemoveInvalidXmlChars(this string s)
		{
			if (s.IsEmpty())
				return s;

			return Regex.Replace(s, @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", "", RegexOptions.Compiled);
		}

		[DebuggerStepThrough]
		public static string ReplaceCsvChars(this string s)
		{
			if (s.IsEmpty())
			{
				return "";
			}

			s = s.Replace(';', ',');
			s = s.Replace('\r', ' ');
			s = s.Replace('\n', ' ');
			return s.Replace("'", "");
		}

		[DebuggerStepThrough]
		public static string HighlightKeywords(this string input, string keywords, string preMatch = "<strong>", string postMatch = "</strong>")
		{
			Guard.NotNull(preMatch, nameof(preMatch));
			Guard.NotNull(postMatch, nameof(postMatch));

			if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keywords))
			{
				return input;
			}

			var pattern = string.Join("|", keywords.Trim().Tokenize(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => Regex.Escape(x))
				.Distinct());

			if (!string.IsNullOrWhiteSpace(pattern))
			{
				var rg = new Regex(pattern, RegexOptions.IgnoreCase);
				input = rg.Replace(input, m => preMatch + m.Value.EmptyNull().HtmlEncode() + postMatch);
			}

			return input;
		}

		/// <summary>
		/// Capitalizes the word.
		/// </summary>
		/// <param name="word">string. The word to capitalize.</param>
		/// <returns>The Capitalized word.</returns>
		public static string Capitalize(this string word)
		{
			return word.Substring(0, 1).ToUpper() + word[1..].ToLower();
		}

		/// <summary>
		/// Revers of <see cref="Capitalize"/>
		/// </summary>
		/// <param name="word">string. The word to un-capitalize.</param>
		/// <returns></returns>
		public static string Uncapitalize(this string word)
		{
			return word.Substring(0, 1).ToLower() + word[1..];
		}
	}
}
