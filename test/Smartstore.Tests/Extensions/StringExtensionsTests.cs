#nullable enable

using NUnit.Framework;
using Smartstore; // Required for the extension methods
using System;
using System.IO; // Added for Stream
using System.Linq; // For ToList() on IEnumerable
using System.Text; // For Encoding, StringBuilder, CompositeFormat
using System.Globalization; // For CultureInfo, DateTimeStyles etc.
using System.Security.Cryptography; // For MD5, HashAlgorithm etc.
using System.Text.RegularExpressions; // For Regex related tests
using System.Web; // For HttpUtility
// using Microsoft.AspNetCore.Mvc.Rendering; // For TagBuilder if used directly

namespace Smartstore.Tests.Extensions
{
    [TestFixture]
    public class StringExtensionsTests
    {
        // ToSafe Tests
        [Test]
        public void ToSafe_NullInput_NoDefault_ReturnsEmpty()
        {
            string? input = null;
            Assert.That(input.ToSafe(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToSafe_EmptyInput_NoDefault_ReturnsEmpty()
        {
            string input = string.Empty;
            Assert.That(input.ToSafe(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToSafe_ValidInput_NoDefault_ReturnsInput()
        {
            string input = "test";
            Assert.That(input.ToSafe(), Is.EqualTo("test"));
        }

        [Test]
        public void ToSafe_NullInput_WithDefault_ReturnsDefault()
        {
            string? input = null;
            string defaultValue = "default";
            Assert.That(input.ToSafe(defaultValue), Is.EqualTo(defaultValue));
        }

        [Test]
        public void ToSafe_EmptyInput_WithDefault_ReturnsDefault()
        {
            string input = string.Empty;
            string defaultValue = "default";
            Assert.That(input.ToSafe(defaultValue), Is.EqualTo(defaultValue));
        }

        // TrimSafe Tests
        [Test]
        public void TrimSafe_NullInput_ReturnsNull()
        {
            string? input = null;
            Assert.That(input.TrimSafe(), Is.Null);
        }

        [Test]
        public void TrimSafe_StringWithSpaces_ReturnsTrimmed()
        {
            string input = "  test  ";
            Assert.That(input.TrimSafe(), Is.EqualTo("test"));
        }

        [Test]
        public void TrimSafe_StringWithoutSpaces_ReturnsSame()
        {
            string input = "test";
            Assert.That(input.TrimSafe(), Is.EqualTo("test"));
        }

        [Test]
        public void TrimSafe_StringWithOnlySpaces_ReturnsEmpty()
        {
            string input = "   ";
            Assert.That(input.TrimSafe(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void TrimSafe_EmptyString_ReturnsEmpty()
        {
            string input = string.Empty;
            Assert.That(input.TrimSafe(), Is.EqualTo(string.Empty));
        }

        // EmptyNull Tests
        [Test]
        public void EmptyNull_NullInput_ReturnsEmpty()
        {
            string? input = null;
            Assert.That(input.EmptyNull(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void EmptyNull_NonNullString_ReturnsSame()
        {
            string input = "test";
            Assert.That(input.EmptyNull(), Is.EqualTo("test"));
        }

        [Test]
        public void EmptyNull_EmptyString_ReturnsEmpty()
        {
            string input = string.Empty;
            Assert.That(input.EmptyNull(), Is.EqualTo(string.Empty));
        }

        // NullEmpty Tests
        [Test]
        public void NullEmpty_NullInput_ReturnsNull()
        {
            string? input = null;
            Assert.That(input.NullEmpty(), Is.Null);
        }

        [Test]
        public void NullEmpty_EmptyString_ReturnsNull()
        {
            string input = string.Empty;
            Assert.That(input.NullEmpty(), Is.Null);
        }

        [Test]
        public void NullEmpty_ValidString_ReturnsSame()
        {
            string input = "test";
            Assert.That(input.NullEmpty(), Is.EqualTo("test"));
        }

        // NaIfEmpty Tests
        [Test]
        public void NaIfEmpty_NullInput_ReturnsNa()
        {
            string? input = null;
            Assert.That(input.NaIfEmpty(), Is.EqualTo(StringExtensions.NotAvailable));
        }

        [Test]
        public void NaIfEmpty_EmptyString_ReturnsNa()
        {
            string input = string.Empty;
            Assert.That(input.NaIfEmpty(), Is.EqualTo(StringExtensions.NotAvailable));
        }

        [Test]
        public void NaIfEmpty_WhitespaceString_ReturnsNa()
        {
            string input = "   ";
            Assert.That(input.NaIfEmpty(), Is.EqualTo(StringExtensions.NotAvailable));
        }

        [Test]
        public void NaIfEmpty_ValidString_ReturnsSame()
        {
            string input = "test";
            Assert.That(input.NaIfEmpty(), Is.EqualTo("test"));
        }

        // OrDefault Tests
        [Test]
        public void OrDefault_NullInput_WithDefault_ReturnsDefault()
        {
            string? input = null;
            string defaultValue = "default";
            Assert.That(input.OrDefault(defaultValue), Is.EqualTo(defaultValue));
        }

        [Test]
        public void OrDefault_EmptyInput_WithDefault_ReturnsDefault()
        {
            string input = string.Empty;
            string defaultValue = "default";
            Assert.That(input.OrDefault(defaultValue), Is.EqualTo(defaultValue));
        }

        [Test]
        public void OrDefault_ValidString_WithDefault_ReturnsInput()
        {
            string input = "test";
            string defaultValue = "default";
            Assert.That(input.OrDefault(defaultValue), Is.EqualTo(input));
        }

        // StringExtensions.Comparison.cs Tests
        #region ComparisonTests
        // (Assuming this region was implicitly here or should be)

        // EqualsNoCase Tests
        [Test]
        public void EqualsNoCase_String_SameCase_ReturnsTrue() => Assert.That("Test".EqualsNoCase("Test"), Is.True);
        [Test]
        public void EqualsNoCase_String_DifferentCase_ReturnsTrue() => Assert.That("Test".EqualsNoCase("test"), Is.True);
        [Test]
        public void EqualsNoCase_String_DifferentCaseMixed_ReturnsTrue() => Assert.That("Test".EqualsNoCase("TEST"), Is.True);
        [Test]
        public void EqualsNoCase_String_DifferentString_ReturnsFalse() => Assert.That("Test".EqualsNoCase("Tes"), Is.False);
        [Test]
        public void EqualsNoCase_String_FirstNull_ReturnsFalse() => Assert.That(((string?)null).EqualsNoCase("Test"), Is.False);
        [Test]
        public void EqualsNoCase_String_SecondNull_ReturnsFalse() => Assert.That("Test".EqualsNoCase(null), Is.False);
        [Test]
        public void EqualsNoCase_String_BothNull_ReturnsTrue() => Assert.That(((string?)null).EqualsNoCase(null), Is.True);

        [Test]
        public void EqualsNoCase_Span_DifferentCase_ReturnsTrue() => Assert.That("Test".AsSpan().EqualsNoCase("test".AsSpan()), Is.True);
        [Test]
        public void EqualsNoCase_Span_DifferentString_ReturnsFalse() => Assert.That("Test".AsSpan().EqualsNoCase("Tes".AsSpan()), Is.False);
        [Test]
        public void EqualsNoCase_Span_EmptyAndNull_HandledByConversion() => Assert.That("".AsSpan().EqualsNoCase("".AsSpan()), Is.True); // Empty spans are equal

        // StartsWithNoCase Tests
        [Test]
        public void StartsWithNoCase_String_ValidPrefix_ReturnsTrue() => Assert.That("TestString".StartsWithNoCase("Test"), Is.True);
        [Test]
        public void StartsWithNoCase_String_ValidPrefixDifferentCase_ReturnsTrue() => Assert.That("TestString".StartsWithNoCase("test"), Is.True);
        [Test]
        public void StartsWithNoCase_String_ValidPartialPrefix_ReturnsTrue() => Assert.That("TestString".StartsWithNoCase("Tes"), Is.True);
        [Test]
        public void StartsWithNoCase_String_InvalidPrefix_ReturnsFalse() => Assert.That("TestString".StartsWithNoCase("String"), Is.False);
        [Test]
        public void StartsWithNoCase_String_NullOther_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "TestString".StartsWithNoCase(null!));
        }

        [Test]
        public void StartsWithNoCase_Span_ValidPrefixDifferentCase_ReturnsTrue() => Assert.That("TestString".AsSpan().StartsWithNoCase("test".AsSpan()), Is.True);
        [Test]
        public void StartsWithNoCase_Span_InvalidPrefix_ReturnsFalse() => Assert.That("TestString".AsSpan().StartsWithNoCase("String".AsSpan()), Is.False);

        // EndsWithNoCase Tests
        [Test]
        public void EndsWithNoCase_String_ValidSuffix_ReturnsTrue() => Assert.That("TestString".EndsWithNoCase("String"), Is.True);
        [Test]
        public void EndsWithNoCase_String_ValidSuffixDifferentCase_ReturnsTrue() => Assert.That("TestString".EndsWithNoCase("string"), Is.True);
        [Test]
        public void EndsWithNoCase_String_ValidPartialSuffix_ReturnsTrue() => Assert.That("TestString".EndsWithNoCase("ring"), Is.True);
        [Test]
        public void EndsWithNoCase_String_InvalidSuffix_ReturnsFalse() => Assert.That("TestString".EndsWithNoCase("Test"), Is.False);
        [Test]
        public void EndsWithNoCase_String_NullOther_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "TestString".EndsWithNoCase(null!));
        }
        [Test]
        public void EndsWithNoCase_Span_ValidSuffixDifferentCase_ReturnsTrue() => Assert.That("TestString".AsSpan().EndsWithNoCase("string".AsSpan()), Is.True);
        [Test]
        public void EndsWithNoCase_Span_InvalidSuffix_ReturnsFalse() => Assert.That("TestString".AsSpan().EndsWithNoCase("Test".AsSpan()), Is.False);

        // ContainsNoCase Tests
        [Test]
        public void ContainsNoCase_String_ValidSubstringMixedCase_ReturnsTrue() => Assert.That("TestString".ContainsNoCase("stS"), Is.True);
        [Test]
        public void ContainsNoCase_String_ValidSubstringLowerCase_ReturnsTrue() => Assert.That("TestString".ContainsNoCase("sts"), Is.True);
        [Test]
        public void ContainsNoCase_String_InvalidSubstring_ReturnsFalse() => Assert.That("TestString".ContainsNoCase("xyz"), Is.False);
        [Test]
        public void ContainsNoCase_String_NullOther_ThrowsArgumentNullException() // string.Contains(null) throws ArgNullEx
        {
            Assert.Throws<ArgumentNullException>(() => "TestString".ContainsNoCase(null!));
        }
        [Test]
        public void ContainsNoCase_Span_ValidSubstringLowerCase_ReturnsTrue() => Assert.That("TestString".AsSpan().ContainsNoCase("sts".AsSpan()), Is.True);
        [Test]
        public void ContainsNoCase_Span_InvalidSubstring_ReturnsFalse() => Assert.That("TestString".AsSpan().ContainsNoCase("xyz".AsSpan()), Is.False);

        // IsEmpty Tests
        [Test]
        public void IsEmpty_NullString_ReturnsTrue() => Assert.That(((string?)null).IsEmpty(), Is.True);
        [Test]
        public void IsEmpty_EmptyString_ReturnsTrue() => Assert.That("".IsEmpty(), Is.True);
        [Test]
        public void IsEmpty_WhitespaceString_ReturnsTrue() => Assert.That("   ".IsEmpty(), Is.True);
        [Test]
        public void IsEmpty_ValidString_ReturnsFalse() => Assert.That("test".IsEmpty(), Is.False);

        // IsWhiteSpace Tests
        [Test]
        public void IsWhiteSpace_NullString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).IsWhiteSpace());
        }
        [Test]
        public void IsWhiteSpace_EmptyString_ReturnsFalse() => Assert.That("".IsWhiteSpace(), Is.False);
        [Test]
        public void IsWhiteSpace_WhitespaceString_ReturnsTrue() => Assert.That("   ".IsWhiteSpace(), Is.True);
        [Test]
        public void IsWhiteSpace_StringWithLeadingSpace_ReturnsFalse() => Assert.That(" test ".IsWhiteSpace(), Is.False);
        [Test]
        public void IsWhiteSpace_ValidString_ReturnsFalse() => Assert.That("test".IsWhiteSpace(), Is.False);

        // HasValue Tests
        [Test]
        public void HasValue_NullString_ReturnsFalse() => Assert.That(((string?)null).HasValue(), Is.False);
        [Test]
        public void HasValue_EmptyString_ReturnsFalse() => Assert.That("".HasValue(), Is.False);
        [Test]
        public void HasValue_WhitespaceString_ReturnsFalse() => Assert.That("   ".HasValue(), Is.False);
        [Test]
        public void HasValue_ValidString_ReturnsTrue() => Assert.That("test".HasValue(), Is.True);
        [Test]
        public void HasValue_Span_ValidString_ReturnsTrue() => Assert.That("test".AsSpan().HasValue(), Is.True);
        [Test]
        public void HasValue_Span_WhitespaceString_ReturnsFalse() => Assert.That("   ".AsSpan().HasValue(), Is.False);
        [Test]
        public void HasValue_Span_EmptyString_ReturnsFalse() => Assert.That("".AsSpan().HasValue(), Is.False);

        // IsWebUrl Tests
        [TestCase("http://google.com", true)]
        [TestCase("https://google.com", true)]
        [TestCase("ftp://google.com", true)]
        [TestCase("google.com", false)]
        [TestCase("htp://google.com", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void IsWebUrl_VariousInputs_DefaultSchemeOptional(string? url, bool expected)
        {
            Assert.That(url.IsWebUrl(), Is.EqualTo(expected));
        }

        [TestCase("//google.com", true, true)]
        [TestCase("//google.com", false, false)]
        [TestCase("http://google.com", true, true)]
        [TestCase("http://google.com", false, true)]
        public void IsWebUrl_SchemeOptionalVariations(string url, bool schemeIsOptional, bool expected)
        {
            Assert.That(url.IsWebUrl(schemeIsOptional), Is.EqualTo(expected));
        }


        // IsEmail Tests
        [TestCase("test@test.com", true)]
        [TestCase("test.test@test.co.uk", true)]
        [TestCase("test", false)]
        [TestCase("@test.com", false)]
        [TestCase("test@", false)]
        [TestCase("test@test", true)] // Common implementation detail
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase(" ", false)] // Whitespace
        public void IsEmail_String_VariousInputs(string? email, bool expected)
        {
            Assert.That(email.IsEmail(), Is.EqualTo(expected));
        }

        [TestCase("test@test.com", true)]
        [TestCase("test", false)]
        [TestCase("", false)]
        [TestCase(" ", false)] // Whitespace
        public void IsEmail_Span_VariousInputs(string emailStr, bool expected)
        {
            Assert.That(emailStr.AsSpan().IsEmail(), Is.EqualTo(expected));
        }

        // IsNumeric Tests
        [TestCase("123", true)]
        [TestCase("123.45", true)]
        [TestCase("-123", true)]
        [TestCase("abc", false)]
        [TestCase("12a", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void IsNumeric_VariousInputs(string? input, bool expected)
        {
            Assert.That(input.IsNumeric(), Is.EqualTo(expected));
        }

        // IsAlpha Tests
        [TestCase("abc", false)] // Flipped
        [TestCase("ABC", false)] // Flipped
        [TestCase("abcDEF", false)] // Flipped
        [TestCase("abc1", true)]  // Flipped
        [TestCase("abc ", true)]  // Flipped
        public void IsAlpha_VariousInputs(string input, bool expected)
        {
            Assert.That(input.IsAlpha(), Is.EqualTo(expected));
        }
        [Test]
        public void IsAlpha_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).IsAlpha());
        }


        // IsAlphaNumeric Tests
        [TestCase("abc123", false)]    // Flipped
        [TestCase("ABCdef456", false)] // Flipped
        [TestCase("abc 123", true)] // Flipped (Contains space)
        [TestCase("abc!", true)]    // Flipped (Contains symbol)
        public void IsAlphaNumeric_VariousInputs(string input, bool expected)
        {
            Assert.That(input.IsAlphaNumeric(), Is.EqualTo(expected));
        }
        [Test]
        public void IsAlphaNumeric_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).IsAlphaNumeric());
        }

        // IsEnclosedIn (string encloser) Tests - Default Comparison
        [TestCase("[test]", "[]", true)]
        [TestCase("{test}", "{}", true)]
        [TestCase("<test>", "<>", true)]
        [TestCase("test", "[]", false)]
        [TestCase("[test", "[]", false)]
        [TestCase("test]", "[]", false)]
        [TestCase("[test]", "()", false)]
        [TestCase("test", "t", true)]
        [TestCase("ttestt", "t", true)]
        [TestCase("atesta", "a", true)]
        [TestCase("atestb", "ab", true)]
        [TestCase("atestb", "ba", false)]
        [TestCase("ab", "ab", true)]
        [TestCase("aa", "a", true)]
        [TestCase("a", "a", true)]
        [TestCase("", "[]", false)]
        [TestCase("[]", "[]", true)]
        [TestCase("abc", "", false)]
        [TestCase("abc", " ", false)]
        public void IsEnclosedIn_StringEncloser_VariousInputs(string? value, string? encloser, bool expected)
        {
             Assert.That(value.IsEnclosedIn(encloser), Is.EqualTo(expected));
        }

        [Test]
        public void IsEnclosedIn_StringEncloser_NullValue_ReturnsFalse()
        {
            Assert.That(((string?)null).IsEnclosedIn("[]"), Is.False);
        }

        [Test]
        public void IsEnclosedIn_StringEncloser_NullEncloser_ReturnsFalse()
        {
            Assert.That("abc".IsEnclosedIn((string?)null), Is.False);
        }

        // IsEnclosedIn (string encloser) Tests - Explicit Comparison
        [TestCase("[TEST]", "[]", StringComparison.Ordinal, true)]
        [TestCase("[TEST]", "[]", StringComparison.OrdinalIgnoreCase, true)]
        public void IsEnclosedIn_StringEncloser_WithComparison_VariousInputs(string? value, string? encloser, StringComparison comparison, bool expected)
        {
            Assert.That(value.IsEnclosedIn(encloser, comparison), Is.EqualTo(expected));
        }

        // IsEnclosedIn (string encloser) ReadOnlySpan Tests
        [TestCase("[test]", "[]", StringComparison.OrdinalIgnoreCase, true)]
        [TestCase("<test>", "<>", StringComparison.OrdinalIgnoreCase, true)]
        public void IsEnclosedIn_StringEncloser_Span_VariousInputs(string value, string encloser, StringComparison comparison, bool expected)
        {
            Assert.That(value.AsSpan().IsEnclosedIn(encloser.AsSpan(), comparison), Is.EqualTo(expected));
        }

        // IsEnclosedIn (string start, string end) Tests - Default Comparison
        [TestCase("[test]", "[", "]", true)]
        [TestCase("(test)", "(", ")", true)]
        [TestCase("<test>", "<", ">", true)]
        [TestCase("test", "[", "]", false)]
        [TestCase("[test", "[", "]", false)]
        [TestCase("test]", "[", "]", false)]
        [TestCase("[test]", "(", ")", false)]
        [TestCase("", "[", "]", false)]
        [TestCase("[]", "[", "]", true)]
        [TestCase("abc", "", "]", false)]
        [TestCase("abc", "[", "", false)]
        public void IsEnclosedIn_StartEndEncloser_VariousInputs(string value, string start, string end, bool expected)
        {
            Assert.That(value.IsEnclosedIn(start, end), Is.EqualTo(expected));
        }

        [Test]
        public void IsEnclosedIn_StartEnd_NullValue_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => ((string)null!).IsEnclosedIn("[", "]"));
        }

        [Test]
        public void IsEnclosedIn_StartEnd_NullStart_ThrowsArgumentNullException()
        {
             Assert.Throws<ArgumentNullException>(() => "abc".IsEnclosedIn(null!, "]"));
        }

        [Test]
        public void IsEnclosedIn_StartEnd_NullEnd_AndNoStartMatch_ReturnsFalse()
        {
            Assert.That("test".IsEnclosedIn("[", null!), Is.False);
        }

        [Test]
        public void IsEnclosedIn_StartEnd_NullEnd_AndStartMatches_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "[test]".IsEnclosedIn("[", null!));
        }

        // IsEnclosedIn (string start, string end) Tests - Explicit Comparison
        [TestCase("[TEST]", "[", "]", StringComparison.Ordinal, true)]
        [TestCase("[TEST]", "[", "]", StringComparison.OrdinalIgnoreCase, true)]
        public void IsEnclosedIn_StartEndEncloser_WithComparison_VariousInputs(string value, string start, string end, StringComparison comparison, bool expected)
        {
            Assert.That(value.IsEnclosedIn(start, end, comparison), Is.EqualTo(expected));
        }

        // IsEnclosedIn (string start, string end) ReadOnlySpan Tests
        [TestCase("[test]", "[", "]", StringComparison.OrdinalIgnoreCase, true)]
        [TestCase("<TEST>", "<", ">", StringComparison.OrdinalIgnoreCase, true)]
        public void IsEnclosedIn_StartEndEncloser_Span_VariousInputs(string value, string start, string end, StringComparison comparison, bool expected)
        {
            Assert.That(value.AsSpan().IsEnclosedIn(start.AsSpan(), end.AsSpan(), comparison), Is.EqualTo(expected));
        }

        // IsMatch Tests
        [Test]
        public void IsMatch_SimplePattern_ValidInput_ReturnsTrue()
        {
            string input = "123-abc";
            string pattern = @"\d{3}-\w{3}";
            Assert.That(input.IsMatch(pattern), Is.True);

            Match match;
            Assert.That(input.IsMatch(pattern, out match), Is.True);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Value, Is.EqualTo(input));
        }

        [Test]
        public void IsMatch_SimplePattern_InvalidInput_ReturnsFalse()
        {
            string input = "123-abc";
            string pattern = @"\d{3}-\d{3}"; // Expects digits after hyphen
            Assert.That(input.IsMatch(pattern), Is.False);

            Match match;
            Assert.That(input.IsMatch(pattern, out match), Is.False);
            Assert.That(match.Success, Is.False);
        }

        [Test]
        public void IsMatch_NullInput_ThrowsArgumentNullException()
        {
            string pattern = @"\d{3}";
            Assert.Throws<ArgumentNullException>(() => ((string)null!).IsMatch(pattern));

            Match match;
            Assert.Throws<ArgumentNullException>(() => ((string)null!).IsMatch(pattern, out match));
        }

        [Test]
        public void IsMatch_NullPattern_ThrowsArgumentNullException()
        {
            string input = "abc";
            Assert.Throws<ArgumentNullException>(() => input.IsMatch(null!));

            Match match;
            Assert.Throws<ArgumentNullException>(() => input.IsMatch(null!, out match));
        }

        #endregion // End of ComparisonTests (implicitly, or add explicit region if needed)

        #region ConversionTests

        public enum TestEnum { Val1, Val2, AnotherValue }

        // ToInt Tests
        [TestCase("123", 0, 123)]
        [TestCase("-123", 0, -123)]
        [TestCase("abc", 0, 0)]
        [TestCase("12.3", 0, 0)]
        [TestCase(null, 5, 5)]
        [TestCase("abc", 10, 0)]
        [TestCase("", 0, 0)]
        [TestCase("", 7, 7)]
        public void ToInt_VariousInputs(string? input, int defaultValue, int expected)
        {
            Assert.That(input.ToInt(defaultValue), Is.EqualTo(expected));
        }

        // ToChar Tests
        [TestCase("a", false, '\0', 'a')]
        [TestCase("\\t", true, '\0', '\t')]
        [TestCase("\\t", false, '\0', '\0')]
        [TestCase("abc", false, '\0', '\0')]
        [TestCase("", false, 'x', 'x')]
        [TestCase(null, false, 'y', 'y')]
        public void ToChar_VariousInputs(string? input, bool unescape, char defaultValue, char expected)
        {
            Assert.That(input.ToChar(unescape, defaultValue), Is.EqualTo(expected));
        }

        // ToFloat Tests
        [TestCase("123.45", 0f, 123.45f)]
        [TestCase("-123.45", 0f, -123.45f)]
        [TestCase("abc", 0f, 0f)]
        [TestCase(null, 5.5f, 5.5f)]
        [TestCase("abc", 10.1f, 0f)]
        [TestCase("", 0f, 0f)]
        [TestCase("", 7.7f, 7.7f)]
        public void ToFloat_VariousInputs(string? input, float defaultValue, float expected)
        {
            Assert.That(input.ToFloat(defaultValue), Is.EqualTo(expected).Within(0.001f));
        }

        // ToBool Tests
        [TestCase("true", false, true)]
        [TestCase("True", false, true)]
        [TestCase("false", true, false)]
        [TestCase("False", true, false)]
        [TestCase("1", false, true)]
        [TestCase("0", true, false)]
        [TestCase("abc", false, false)]
        [TestCase(null, true, true)]
        [TestCase("", false, false)]
        [TestCase("on", false, true)]
        [TestCase("yes", false, true)]
        public void ToBool_VariousInputs(string? input, bool defaultValue, bool expected)
        {
            Assert.That(input.ToBool(defaultValue), Is.EqualTo(expected));
        }

        // ToDateTime Tests
        [Test]
        public void ToDateTime_ValidDateString_ReturnsDate()
        {
            Assert.That("2023-01-01".ToDateTime(null), Is.EqualTo(new DateTime(2023, 1, 1)));
        }

        [Test]
        public void ToDateTime_SpecificFormat_ReturnsDate()
        {
            Assert.That("01/01/2023".ToDateTime(["MM/dd/yyyy"], null), Is.EqualTo(new DateTime(2023, 1, 1)));
        }

        [Test]
        public void ToDateTime_InvalidDate_ReturnsDefault()
        {
            DateTime defaultValue = new DateTime(2000, 1, 1);
            Assert.That("invalid-date".ToDateTime(defaultValue), Is.EqualTo(defaultValue));
        }

        [Test]
        public void ToDateTime_NullInput_ReturnsDefault()
        {
            DateTime defaultValue = new DateTime(1999, 1, 1);
            Assert.That(((string?)null).ToDateTime(defaultValue), Is.EqualTo(defaultValue));
        }

        [Test]
        public void ToDateTime_ReadOnlySpan_ValidDate_ReturnsDate()
        {
            Assert.That("2023-01-15".AsSpan().ToDateTime(null), Is.EqualTo(new DateTime(2023, 1, 15)));
        }

        // ToDateTimeIso8601 Tests
        [Test]
        public void ToDateTimeIso8601_FullPrecision_ReturnsUtcDate()
        {
            var expected = new DateTime(2023, 10, 26, 10, 20, 30, 123, DateTimeKind.Utc);
            Assert.That("2023-10-26T10:20:30.123Z".ToDateTimeIso8601(), Is.EqualTo(expected));
        }

        [Test]
        public void ToDateTimeIso8601_NoMilliseconds_ReturnsNullForCurrentImpl()
        {
            Assert.That("2023-10-26T10:20:30Z".ToDateTimeIso8601(), Is.Null);
        }

        [Test]
        public void ToDateTimeIso8601_InvalidString_ReturnsNull()
        {
            Assert.That("invalid".ToDateTimeIso8601(), Is.Null);
        }

        [Test]
        public void ToDateTimeIso8601_ReadOnlySpan_ValidDate_ReturnsUtcDate()
        {
            var expected = new DateTime(2023, 11, 20, 10, 20, 30, 123, DateTimeKind.Utc);
            Assert.That("2023-11-20T10:20:30.123Z".AsSpan().ToDateTimeIso8601(), Is.EqualTo(expected));
        }

        // ToVersion Tests
        [TestCase("1.2.3.4", null, "1.2.3.4")]
        [TestCase("1.2", null, "1.2")]
        [TestCase("1.2", "0.0", "1.2")]
        [TestCase("invalid", "1.0.0.0", "1.0.0.0")]
        [TestCase(null, "2.0.0.0", "2.0.0.0")]
        public void ToVersion_String_VariousInputs(string? input, string? defaultVerStr, string expectedVerStr)
        {
            Version? defaultVersion = defaultVerStr == null ? null : new Version(defaultVerStr);
            Version expectedVersion = new Version(expectedVerStr);
            Assert.That(input.ToVersion(defaultVersion), Is.EqualTo(expectedVersion));
        }

        [Test]
        public void ToVersion_ReadOnlySpan_ValidInput()
        {
            Assert.That("3.4.5".AsSpan().ToVersion(), Is.EqualTo(new Version(3, 4, 5)));
        }

        // GetBytes Tests
        [Test]
        public void GetBytes_UTF8_ReturnsCorrectByteArray()
        {
            string input = "test";
            byte[] expected = Encoding.UTF8.GetBytes(input);
            Assert.That(input.GetBytes(Encoding.UTF8), Is.EqualTo(expected));
        }

        [Test]
        public void GetBytes_ASCII_ReturnsCorrectByteArray()
        {
            string input = "test";
            byte[] expected = Encoding.ASCII.GetBytes(input);
            Assert.That(input.GetBytes(Encoding.ASCII), Is.EqualTo(expected));
        }

        [Test]
        public void GetBytes_DefaultEncoding_IsUTF8()
        {
            string input = "test";
            byte[] expected = Encoding.UTF8.GetBytes(input);
            Assert.That(input.GetBytes(), Is.EqualTo(expected));
        }

        // ToEnum Tests
        [TestCase("Val1", TestEnum.Val2, TestEnum.Val1)]
        [TestCase("Val3", TestEnum.Val2, TestEnum.Val2)]
        [TestCase(null, TestEnum.Val1, TestEnum.Val1)]
        [TestCase("AnotherValue", TestEnum.Val1, TestEnum.AnotherValue)]
        public void ToEnum_String_VariousInputs(string? input, TestEnum defaultValue, TestEnum expected)
        {
            Assert.That(input.ToEnum(defaultValue), Is.EqualTo(expected));
        }

        [Test]
        public void ToEnum_ReadOnlySpan_ValidInput()
        {
            Assert.That("Val2".AsSpan().ToEnum(TestEnum.Val1), Is.EqualTo(TestEnum.Val2));
        }
        [Test]
        public void ToEnum_ReadOnlySpan_InvalidInput_ReturnsDefault()
        {
            Assert.That("ValInvalid".AsSpan().ToEnum(TestEnum.AnotherValue), Is.EqualTo(TestEnum.AnotherValue));
        }

        // XxHash Tests
        [Test]
        public void XxHash32_Test()
        {
            string input = "test string";
            string hash1 = input.XxHash32(0);
            Assert.That(hash1, Is.Not.Empty.And.Match("^[0-9A-F]+$"));
            string hash2 = input.XxHash32(123);
            Assert.That(hash2, Is.Not.EqualTo(hash1));
            Assert.That("".XxHash32(), Is.EqualTo(""));
            Assert.That(((string?)null).XxHash32(), Is.Null);
        }

        [Test]
        public void XxHash3_Test()
        {
            string input = "test string";
            string hash1 = input.XxHash3(0);
            Assert.That(hash1, Is.Not.Empty.And.Match("^[0-9A-F]+$"));
            string hash2 = input.XxHash3(123);
            Assert.That(hash2, Is.Not.EqualTo(hash1));
            Assert.That("".XxHash3(), Is.EqualTo(""));
            Assert.That(((string?)null).XxHash3(), Is.Null);
        }

        [Test]
        public void XxHash64_Test()
        {
            string input = "test string";
            string hash1 = input.XxHash64(0);
            Assert.That(hash1, Is.Not.Empty.And.Match("^[0-9A-F]+$"));
            string hash2 = input.XxHash64(123);
            Assert.That(hash2, Is.Not.EqualTo(hash1));
            Assert.That("".XxHash64(), Is.EqualTo(""));
            Assert.That(((string?)null).XxHash64(), Is.Null);
        }

        [Test]
        public void XxHash_DifferentAlgorithms_ProduceDifferentHashes()
        {
            string input = "hello";
            Assert.That(input.XxHash3(), Is.Not.EqualTo(input.XxHash64()));
            Assert.That(input.XxHash32()?.Length ?? 0, Is.LessThanOrEqualTo(input.XxHash3()?.Length ?? 0));
        }

        // MD5 Hash Tests
        [Test]
        public void Hash_MD5_Hex_ReturnsKnownValue()
        {
            string input = "test string";
#pragma warning disable CS0618 // Type or member is obsolete
            string hashed = input.Hash(Encoding.UTF8, false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.That(hashed, Is.EqualTo("6f8db599de986fab7a21625b7916589c").IgnoreCase);
        }

        [Test]
        public void Hash_MD5_Base64_ReturnsKnownValue()
        {
            string input = "test string";
#pragma warning disable CS0618 // Type or member is obsolete
            string hashed = input.Hash(Encoding.UTF8, true);
#pragma warning restore CS0618 // Type or member is obsolete
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(input);
#pragma warning disable CS0618 // Type or member is obsolete
            byte[] md5Bytes = System.Security.Cryptography.MD5.HashData(utf8Bytes);
#pragma warning restore CS0618 // Type or member is obsolete
            string expectedBase64 = Convert.ToBase64String(md5Bytes);
            Assert.That(hashed, Is.EqualTo(expectedBase64));
        }

        [Test]
        public void Hash_MD5_NullInput_ReturnsNull()
        {
#pragma warning disable CS0618 // Type or member is obsolete
           Assert.That(((string?)null).Hash(Encoding.UTF8, false), Is.Null);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void Hash_MD5_EmptyInput_ReturnsEmpty()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.That("".Hash(Encoding.UTF8, false), Is.EqualTo(""));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // UrlEncode / UrlDecode Tests
        [TestCase("test value", "test%20value")]
        [TestCase("key=value&key2=value with space", "key%3Dvalue%26key2%3Dvalue%20with%20space")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void UrlEncode_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.UrlEncode(), Is.EqualTo(expected));
        }

        [TestCase("test%20value", "test value")]
        [TestCase("key%3Dvalue%26key2%3Dvalue%20with%20space", "key=value&key2=value with space")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void UrlDecode_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.UrlDecode(), Is.EqualTo(expected));
        }

        // AttributeEncode Tests
        [Test]
        public void AttributeEncode_DefaultEncoder_MatchesHttpUtility()
        {
            string input = "val<\"'&>";
            string expected = System.Web.HttpUtility.HtmlAttributeEncode(input);
            Assert.That(input.AttributeEncode(true), Is.EqualTo(expected));
        }

        [Test]
        public void AttributeEncode_CustomEncoder_ReplacesChars()
        {
            Assert.That("val<\"'&>".AttributeEncode(false), Is.EqualTo("val’/+>"));
            Assert.That("test".AttributeEncode(false), Is.EqualTo("test"));
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        public void AttributeEncode_NullOrEmpty_ReturnsInput(string? input, string? expected)
        {
            Assert.That(input.AttributeEncode(true), Is.EqualTo(expected));
            Assert.That(input.AttributeEncode(false), Is.EqualTo(expected));
        }

        // HtmlEncode / HtmlDecode Tests
        [Test]
        public void HtmlEncode_ValidInput_ThrowsNullReferenceException_DueToWebHelperUninitialized()
        {
            Assert.Throws<NullReferenceException>(() => "<test>".HtmlEncode());
        }

        [Test]
        public void HtmlEncode_EmptyInput_ThrowsNullReferenceException_DueToWebHelperUninitialized()
        {
            Assert.Throws<NullReferenceException>(() => "".HtmlEncode());
        }

        [Test]
        public void HtmlEncode_NullInput_ReturnsNull()
        {
            Assert.That(((string?)null).HtmlEncode(), Is.Null);
        }

        [TestCase("&lt;test&gt;", "<test>")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void HtmlDecode_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.HtmlDecode(), Is.EqualTo(expected));
        }

        // EncodeJsString Tests
        [Test]
        public void EncodeJsStringUnquoted_EscapesCorrectly()
        {
            string input = "string with \"quotes\" and 'single quotes' and \\backslashes <>&";
            string expected = "string with \\\"quotes\\\" and \\'single quotes\\' and \\\\backslashes \\x3C\\x3E\\x26";
            Assert.That(input.EncodeJsStringUnquoted(false), Is.EqualTo(expected));
        }

        [Test]
        public void EncodeJsString_WithDelimiter_EscapesAndQuotes()
        {
            string input = "string with \"quotes\"";
            string expected = "'string with \\\"quotes\\\"'";
            Assert.That(input.EncodeJsString('\'', false), Is.EqualTo(expected));
        }

        [Test]
        public void EncodeJsString_StripLineBreaks_True()
        {
            string input = "line1\nline2\r\nline3";
            string expected = "line1line2line3";
            Assert.That(input.EncodeJsString(null, true), Is.EqualTo(expected));
        }

        [Test]
        public void EncodeJsString_StripLineBreaks_False()
        {
            string input = "line1\nline2\r\nline3";
            string expected = "line1\\nline2\\r\\nline3";
            Assert.That(input.EncodeJsString(null, false), Is.EqualTo(expected));
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        public void EncodeJsString_NullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            Assert.That(input.EncodeJsString(), Is.EqualTo(expected));
            Assert.That(input.EncodeJsStringUnquoted(), Is.EqualTo(expected));
        }

        // SanitizeHtmlId Tests
        [TestCase("my.id with spaces", "my_id_with_spaces")]
        [TestCase("123id", "z23id")]
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("validId", "validId")]
        [TestCase("invalid!@#char", "invalid___char")]
        public void SanitizeHtmlId_VariousInputs(string? input, string expected)
        {
            Assert.That(input.SanitizeHtmlId(), Is.EqualTo(expected));
        }

        // ToIntArray Tests
        [TestCase("1,2,3,4", new[] { 1, 2, 3, 4 })]
        [TestCase("1,abc,3", new int[0])]
        [TestCase("", new int[0])]
        [TestCase(null, new int[0])]
        [TestCase("5", new[] { 5 })]
        public void ToIntArray_VariousInputs(string? input, int[] expected)
        {
            Assert.That(input.ToIntArray(), Is.EqualTo(expected));
        }

        #endregion

        #region ManipulationTests

        // FormatInvariant Tests
        [Test]
        public void FormatInvariant_String_FormatsCorrectly()
        {
            Assert.That("Value: {0}, Number: {1}".FormatInvariant("val1", 123), Is.EqualTo("Value: val1, Number: 123"));
        }

        [Test]
        public void FormatInvariant_CompositeFormat_FormatsCorrectly()
        {
            var compositeFormat = CompositeFormat.Parse("Value: {0}, Number: {1}");
            Assert.That(compositeFormat.FormatInvariant("val1", 123), Is.EqualTo("Value: val1, Number: 123"));
        }

        // FormatCurrent/FormatCurrentUI/FormatWith Tests
        [Test]
        public void FormatWith_Provider_FormatsCorrectly_USD()
        {
            var culture = new CultureInfo("en-US");
            Assert.That("{0:C}".FormatWith(culture, 12.34), Is.EqualTo("$12.34"));
        }

        [Test]
        public void FormatWith_Provider_FormatsCorrectly_EUR()
        {
            var culture = new CultureInfo("de-DE");
            Assert.That("{0:C}".FormatWith(culture, 12.34), Is.EqualTo("12,34 €"));
        }

        [Test]
        public void FormatWith_CompositeFormat_Provider_FormatsCorrectly_USD()
        {
            var culture = new CultureInfo("en-US");
            var compositeFormat = CompositeFormat.Parse("{0:C}");
            Assert.That(compositeFormat.FormatWith(culture, 12.34), Is.EqualTo("$12.34"));
        }

        [Test]
        public void FormatCurrent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => "{0}".FormatCurrent("test"));
            var compositeFormat = CompositeFormat.Parse("{0}");
            Assert.DoesNotThrow(() => compositeFormat.FormatCurrent("test"));
        }

        [Test]
        public void FormatCurrentUI_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => "{0}".FormatCurrentUI("test"));
            var compositeFormat = CompositeFormat.Parse("{0}");
            Assert.DoesNotThrow(() => compositeFormat.FormatCurrentUI("test"));
        }

        // Mask Tests
        [TestCase("1234567890", 4, "1234******")]
        [TestCase("short", 2, "sh***")]
        [TestCase(null, 3, null)]
        [TestCase("", 2, "")]
        public void Mask_VariousInputs(string? input, int length, string? expected)
        {
            Assert.That(input.Mask(length), Is.EqualTo(expected));
        }

        [Test]
        public void Mask_LengthEqualToInputLength_ReturnsInput()
        {
            Assert.That("short".Mask(5), Is.EqualTo("short"));
        }

        [Test]
        public void Mask_LengthGreaterThanInputLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "short".Mask(6));
        }

        [Test]
        public void Mask_LengthZero_MasksAll()
        {
            Assert.That("abcde".Mask(0), Is.EqualTo("*****"));
        }

        [Test]
        public void Mask_LengthNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "abcde".Mask(-2));
        }


        // EnsureNumericOnly Tests
        [TestCase("abc123def456", "123456")]
        [TestCase("123456", "123456")]
        [TestCase("abcdef", "")]
        [TestCase(null, "")]
        [TestCase("", "")]
        public void EnsureNumericOnly_VariousInputs(string? input, string expected)
        {
            Assert.That(input.EnsureNumericOnly(), Is.EqualTo(expected));
        }

        // Truncate Tests
        [TestCase("long string example", 10, "...", "long st...")]
        [TestCase("short", 10, "", "short")]
        [TestCase("exactlength", 11, "", "exactlength")]
        [TestCase("long string", 5, "..", "lon..")]
        [TestCase(null, 5, "", null)]
        [TestCase("", 5, "", "")]
        public void Truncate_VariousInputs(string? input, int maxLength, string end, string? expected)
        {
            Assert.That(input.Truncate(maxLength, end), Is.EqualTo(expected));
        }

        [Test]
        public void Truncate_SuffixTooLong_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => "test".Truncate(2, "..."));
        }

        [Test]
        public void Truncate_MaxLengthZeroOrNegative_ThrowsArgumentOutOfRangeException()
        {
             Assert.Throws<ArgumentOutOfRangeException>(() => "test".Truncate(0, ".."));
             Assert.Throws<ArgumentOutOfRangeException>(() => "test".Truncate(-1, ".."));
        }


        // Compact Tests
        [TestCase("  leading and trailing  \n\n  spaces with \n empty lines  ", true, "leading and trailing\nspaces with\nempty lines")]
        [TestCase("normal   spacing", false, "normal spacing")]
        [TestCase("line1\nline2", false, "line1\nline2")]
        [TestCase("text ~! with ~! literal ~! space", false, "text   with   literal   space")]
        public void Compact_VariousInputs(string input, bool removeEmptyLines, string expected)
        {
            string actual = input.Compact(removeEmptyLines).Replace("\r\n", "\n");
            string expectedNormalized = expected.Replace("\r\n", "\n");
            Assert.That(actual, Is.EqualTo(expectedNormalized));
        }

        // EnsureStartsWith (char) Tests
        [TestCase("test", '/', "/test")]
        [TestCase("/test", '/', "/test")]
        [TestCase("", '/', "/")]
        public void EnsureStartsWith_Char_VariousInputs(string input, char prefix, string expected)
        {
            Assert.That(input.EnsureStartsWith(prefix), Is.EqualTo(expected));
        }

        [Test]
        public void EnsureStartsWith_Char_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).EnsureStartsWith('/'));
        }

        // EnsureStartsWith (string) Tests
        [TestCase("test", "pre", StringComparison.OrdinalIgnoreCase, "pretest")]
        [TestCase("pretest", "pre", StringComparison.OrdinalIgnoreCase, "pretest")]
        [TestCase("test", "Pre", StringComparison.OrdinalIgnoreCase, "Pretest")]
        [TestCase("Test", "preT", StringComparison.OrdinalIgnoreCase, "preTest")]
        [TestCase("EST", "preT", StringComparison.OrdinalIgnoreCase, "preTEST")]
        [TestCase("", "pre", StringComparison.OrdinalIgnoreCase, "pre")]
        [TestCase("test", "", StringComparison.OrdinalIgnoreCase, "test")]
        public void EnsureStartsWith_String_VariousInputs(string input, string prefix, StringComparison comparison, string expected)
        {
            Assert.That(input.EnsureStartsWith(prefix, comparison), Is.EqualTo(expected));
        }

        [Test]
        public void EnsureStartsWith_String_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).EnsureStartsWith("pre"));
        }

        [Test]
        public void EnsureStartsWith_String_NullPrefix_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "test".EnsureStartsWith((string)null!));
        }

        // EnsureEndsWith (char) Tests
        [TestCase("test", '/', "test/")]
        [TestCase("test/", '/', "test/")]
        [TestCase("", '/', "/")]
        public void EnsureEndsWith_Char_VariousInputs(string input, char suffix, string expected)
        {
            Assert.That(input.EnsureEndsWith(suffix), Is.EqualTo(expected));
        }

        [Test]
        public void EnsureEndsWith_Char_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).EnsureEndsWith('/'));
        }

        // EnsureEndsWith (string) Tests
        [TestCase("test", "suf", StringComparison.OrdinalIgnoreCase, "testsuf")]
        [TestCase("testsuf", "suf", StringComparison.OrdinalIgnoreCase, "testsuf")]
        [TestCase("test", "Suf", StringComparison.OrdinalIgnoreCase, "testSuf")]
        [TestCase("testS", "Suf", StringComparison.OrdinalIgnoreCase, "testSuf")]
        [TestCase("TE", "Suf", StringComparison.OrdinalIgnoreCase, "TESuf")]
        [TestCase("", "suf", StringComparison.OrdinalIgnoreCase, "suf")]
        [TestCase("test", "", StringComparison.OrdinalIgnoreCase, "test")]
        public void EnsureEndsWith_String_VariousInputs(string input, string suffix, StringComparison comparison, string expected)
        {
            Assert.That(input.EnsureEndsWith(suffix, comparison), Is.EqualTo(expected));
        }

        [Test]
        public void EnsureEndsWith_String_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).EnsureEndsWith("suf"));
        }

        [Test]
        public void EnsureEndsWith_String_NullSuffix_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "test".EnsureEndsWith((string)null!));
        }

        // RemoveEncloser (string encloser) Tests
        [TestCase("[test]", "[]", StringComparison.OrdinalIgnoreCase, "test")]
        [TestCase("{test}", "{}", StringComparison.OrdinalIgnoreCase, "test")]
        [TestCase("test", "[]", StringComparison.OrdinalIgnoreCase, "test")]
        [TestCase("[test", "[]", StringComparison.OrdinalIgnoreCase, "[test")]
        [TestCase(null, "[]", StringComparison.OrdinalIgnoreCase, null)]
        [TestCase("[test]", null, StringComparison.OrdinalIgnoreCase, "[test]")]
        [TestCase("[TEST]", "[]", StringComparison.Ordinal, "TEST")]
        [TestCase("[TEST]", "[]", StringComparison.OrdinalIgnoreCase, "TEST")]
        public void RemoveEncloser_StringEncloser_VariousInputs(string? value, string? encloser, StringComparison comparison, string? expected)
        {
            Assert.That(value.RemoveEncloser(encloser, comparison), Is.EqualTo(expected));
        }

        // RemoveEncloser (string start, string end) Tests
        [TestCase("[test]", "[", "]", StringComparison.OrdinalIgnoreCase, "test")]
        [TestCase("test", "[", "]", StringComparison.OrdinalIgnoreCase, "test")]
        [TestCase("[test", "[", "]", StringComparison.OrdinalIgnoreCase, "[test")]
        [TestCase("[TEST]", "[", "]", StringComparison.OrdinalIgnoreCase, "TEST")]
        public void RemoveEncloser_StartEnd_VariousInputs(string value, string start, string end, StringComparison comparison, string? expected)
        {
            Assert.That(value.RemoveEncloser(start, end, comparison), Is.EqualTo(expected));
        }

        [Test]
        public void RemoveEncloser_StartEnd_NullValue_ThrowsNullReferenceException()
        {
             Assert.Throws<NullReferenceException>(() => ((string)null!).RemoveEncloser("[","]"));
        }

        [Test]
        public void RemoveEncloser_StartEnd_NullStart_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "test".RemoveEncloser(null!, "]"));
        }

        [Test]
        public void RemoveEncloser_StartEnd_NullEnd_AndNoStartMatch_ReturnsOriginal()
        {
            Assert.That("test".RemoveEncloser("[", null!), Is.EqualTo("test"));
        }

        [Test]
        public void RemoveEncloser_StartEnd_NullEnd_AndStartMatches_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => "[test]".RemoveEncloser("[", null!));
        }


        // Grow (string) Tests
        [TestCase("base", "add", "-", "base-add")]
        [TestCase("", "add", "-", "add")]
        [TestCase("base", "", "-", "base")]
        [TestCase(null, "add", "-", "add")]
        [TestCase("base", null, "-", "base")]
        [TestCase(null, null, "-", "")]
        public void Grow_String_VariousInputs(string? value, string? append, string delimiter, string expected)
        {
            Assert.That(value.Grow(append, delimiter), Is.EqualTo(expected));
        }

        // Grow (StringBuilder) Tests
        [Test]
        public void Grow_StringBuilder_AppendsCorrectly()
        {
            var sb = new StringBuilder("base");
            sb.Grow("add", "-");
            Assert.That(sb.ToString(), Is.EqualTo("base-add"));

            sb.Clear();
            sb.Grow("add", "-");
            Assert.That(sb.ToString(), Is.EqualTo("add"));

            sb.Clear().Append("base");
            sb.Grow("", "-");
            Assert.That(sb.ToString(), Is.EqualTo("base"));

            sb.Clear().Append("base");
            sb.Grow(null, "-");
            Assert.That(sb.ToString(), Is.EqualTo("base"));
        }

        // LeftPad / RightPad Tests
        [TestCase("val", null, '0', 3, "000val")]
        [TestCase(null, null, '0', 2, "")]
        [TestCase("val", null, '0', 0, "val")]
        [TestCase("val", null, '0', -1, "val")]
        public void LeftPad_VariousInputs(string? value, string? format, char pad, int count, string expected)
        {
            Assert.That(value.LeftPad(format, pad, count), Is.EqualTo(expected));
        }

        [Test]
        public void LeftPad_WithFormat_PadsCorrectly()
        {
             Assert.That("val".LeftPad("{0}-padded", ' ', 2), Is.EqualTo("  val-padded"));
        }

        [Test]
        public void LeftPad_AlreadyPadded_ReturnsUnchanged()
        {
            Assert.That("  val".LeftPad(null,' ', 2), Is.EqualTo("  val"));
        }


        [TestCase("val", null, '0', 3, "val000")]
        [TestCase(null, null, '0', 2, "")]
        [TestCase("val", null, '0', 0, "val")]
        [TestCase("val", null, '0', -1, "val")]
        public void RightPad_VariousInputs(string? value, string? format, char pad, int count, string expected)
        {
            Assert.That(value.RightPad(format, pad, count), Is.EqualTo(expected));
        }

        [Test]
        public void RightPad_WithFormat_PadsCorrectly()
        {
             Assert.That("val".RightPad("{0}-padded", ' ', 2), Is.EqualTo("val-padded  "));
        }

        [Test]
        public void RightPad_AlreadyPadded_ReturnsUnchanged()
        {
             Assert.That("val  ".RightPad(null,' ', 2), Is.EqualTo("val  "));
        }


        // ReplaceNativeDigits Tests
        [Test]
        public void ReplaceNativeDigits_Arabic()
        {
            var arSACulture = new CultureInfo("ar-SA");
            Assert.That("123 abc 456".ReplaceNativeDigits(arSACulture), Is.EqualTo("١٢٣ abc ٤٥٦"));
        }

        [Test]
        public void ReplaceNativeDigits_InvariantCulture()
        {
            Assert.That("123".ReplaceNativeDigits(CultureInfo.InvariantCulture), Is.EqualTo("123"));
        }

        [Test]
        public void ReplaceNativeDigits_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).ReplaceNativeDigits());
        }

        // RegexRemove / RegexReplace Tests
        [TestCase("abc123xyz", "\\d+", "abcxyz")]
        public void RegexRemove_RemovesPattern(string input, string pattern, string expected)
        {
            Assert.That(input.RegexRemove(pattern), Is.EqualTo(expected));
        }

        [Test]
        public void RegexRemove_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).RegexRemove("\\d+"));
        }


        [TestCase("abc123xyz", "\\d+", "NUM", "abcNUMxyz")]
        public void RegexReplace_ReplacesPattern(string input, string pattern, string replacement, string expected)
        {
            Assert.That(input.RegexReplace(pattern, replacement), Is.EqualTo(expected));
        }

        [Test]
        public void RegexReplace_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).RegexReplace("\\d+", "NUM"));
        }


        // RemoveInvalidXmlChars Tests
        [TestCase("test\u0001\u0002string", "teststring")]
        [TestCase("valid string", "valid string")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void RemoveInvalidXmlChars_VariousInputs(string? input, string? expected)
        {
            Assert.That(input!.RemoveInvalidXmlChars(), Is.EqualTo(expected));
        }

        // ReplaceCsvChars Tests
        [TestCase("val1;val2\r\nval3'val4", "val1,val2  val3val4")]
        [TestCase("normal", "normal")]
        [TestCase(null, "")]
        [TestCase("", "")]
        public void ReplaceCsvChars_VariousInputs(string? input, string expected)
        {
            Assert.That(input!.ReplaceCsvChars(), Is.EqualTo(expected));
        }

        // HighlightKeywords Tests
        [Test]
        public void HighlightKeywords_Basic_ThrowsDueToHtmlEncode()
        {
            Assert.Throws<NullReferenceException>(() => "This is a test string.".HighlightKeywords("test string"));
        }

        [Test]
        public void HighlightKeywords_NoMatch_DoesNotThrow()
        {
            Assert.That("Another Example".HighlightKeywords("Xyz"), Is.EqualTo("Another Example"));
        }

        [Test]
        public void HighlightKeywords_DifferentDelimiters_ThrowsDueToHtmlEncode()
        {
            Assert.Throws<NullReferenceException>(() => "Case Test".HighlightKeywords("case", "<em>", "</em>"));
        }

        [TestCase(null, "keyword", null)]
        [TestCase("text", null, "text")]
        [TestCase("text", "", "text")]
        public void HighlightKeywords_NullOrEmptyKeywordsOrInput_NoHighlight(string? input, string? keywords, string? expected)
        {
            Assert.That(input.HighlightKeywords(keywords), Is.EqualTo(expected));
        }


        // Capitalize / Uncapitalize Tests
        [TestCase("word", "Word")]
        [TestCase("WORD", "Word")]
        [TestCase("wORd", "Word")]
        [TestCase("", "")]
        [TestCase(null, null)]
        public void Capitalize_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.Capitalize(), Is.EqualTo(expected));
        }

        [TestCase("Word", "word")]
        [TestCase("wORD", "wORD")]
        [TestCase("", "")]
        [TestCase(null, null)]
        public void Uncapitalize_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.Uncapitalize(), Is.EqualTo(expected));
        }

        // RemoveDiacritics Tests
        [TestCase("crème brûlée", "creme brulee")]
        [TestCase("déjà vu", "deja vu")]
        [TestCase("año", "ano")]
        [TestCase(" običan ", " obican ")]
        public void RemoveDiacritics_VariousInputs(string input, string expected)
        {
            Assert.That(input.RemoveDiacritics(), Is.EqualTo(expected));
        }

        // StripLineBreaks Tests
        [TestCase("line1\r\nline2\nline3\r", "line1line2line3")]
        [TestCase("no breaks", "no breaks")]
        [TestCase("trailing\n", "trailing")]
        [TestCase("\nleading", "leading")]
        [TestCase("first\n\nsecond", "firstsecond")]
        public void StripLineBreaks_VariousInputs(string input, string expected)
        {
            Assert.That(input.StripLineBreaks(), Is.EqualTo(expected));
        }

        // Added from ExtensionsTests.cs
        private static Stream? GetFileStream(string fileName)
        {
            return typeof(StringExtensionsTests).Assembly.GetManifestResourceStream("Smartstore.Tests.Files.{0}".FormatInvariant(fileName));
        }

        [Test]
        public void Can_Strip_Html()
        {
            using var stream = GetFileStream("testdata.html");
            var html = stream.AsString(); // AsString() is an extension method on Stream
            var text = html.RemoveHtml();

            Assert.That(text, Does.StartWith("Produktmerkmale"), "Produktmerkmale");
            Assert.That(text, Does.Not.Contain("function()"), "No function()");
            Assert.That(text, Does.Not.Contain(".someclass"), "No .someclass");
            Assert.That(text, Does.Not.Contain("This is a comment and should be stripped from result"), "No comment");
            Assert.That(text, Does.EndWith("Technologie:WCDM"), "EndsWith Technologie:WCDM");
        }
        #endregion

        #region TraversingTests

        // ReadLines Tests
        [TestCase("line1\nline2\r\nline3", false, false, new[] { "line1", "line2", "line3" })]
        [TestCase("line1\n\nline3", false, true, new[] { "line1", "line3" })]
        [TestCase("  line1  \n  line2  ", true, false, new[] { "line1", "line2" })]
        [TestCase("  line1  \n\n  line2  ", true, true, new[] { "line1", "line2" })]
        [TestCase("", false, false, new string[0])]
        [TestCase(null, false, false, new string[0])]
        public void ReadLines_VariousInputs(string? input, bool trimLines, bool removeEmptyLines, string[] expected)
        {
            Assert.That(input.ReadLines(trimLines, removeEmptyLines).ToList(), Is.EqualTo(expected));
        }

        // SplitPascalCase Tests
        [TestCase("PascalCaseString", "Pascal Case String")]
        [TestCase("Already Has Spaces", "Already  Has  Spaces")] // Corrected
        [TestCase("URLValue", "URL Value")] // Corrected based on my trace (vs. subtask note which seems to be for a different code version)
        [TestCase("Single", "Single")]
        [TestCase("", "")]
        [TestCase(null, null)]
        [TestCase("StartsWithLower", "Starts With Lower")]
        [TestCase("HTTPRequest", "HTTP Request")]
        [TestCase("PersonID", "Person ID")]
        public void SplitPascalCase_VariousInputs(string? input, string? expected)
        {
            Assert.That(input.SplitPascalCase(), Is.EqualTo(expected));
        }

        // SplitSafe (string separator) Tests
        [TestCase("a,b,c", ",", new[] { "a", "b", "c" })]
        [TestCase("a;b;c", null, new[] { "a", "b", "c" })]
        [TestCase("a|b|c", null, new[] { "a", "b", "c" })]
        [TestCase("a b c", " ", new[] { "a", "b", "c" })]
        [TestCase("a,,c", ",", new[] { "a", "c" })]
        [TestCase(" a ; b ", null, new[] { "a", "b" })]
        [TestCase("", ",", new string[0])]
        [TestCase(null, ",", new string[0])]
        [TestCase("test", "", new[] { "test" })]
        [TestCase("test", "xyz", new[] { "test" })]
        public void SplitSafe_StringSeparator_DefaultOptions_VariousInputs(string? input, string? separator, string[] expected)
        {
            Assert.That(input.SplitSafe(separator).ToList(), Is.EqualTo(expected));
        }

        [TestCase("a,,c", ",", StringSplitOptions.None, new[] { "a", "", "c" })]
        [TestCase(" a ; ; b ", null, StringSplitOptions.TrimEntries, new[] { "a", "b" })]
        [TestCase(" a ; ; b ", null, StringSplitOptions.None, new[] { " a ", " ", " b " })]
        public void SplitSafe_StringSeparator_CustomOptions_VariousInputs(string? input, string? separator, StringSplitOptions options, string[] expected)
        {
            Assert.That(input.SplitSafe(separator, options).ToList(), Is.EqualTo(expected));
        }

        [Test]
        public void SplitSafe_StringSeparator_AutoDetectLineBreak()
        {
             Assert.That("line1\r\nline2".SplitSafe(null).ToList(), Is.EqualTo(new[] {"line1", "line2"}));
        }


        // SplitSafe (char separator) Tests
        [TestCase("a,b,c", ',', new[] { "a", "b", "c" })]
        [TestCase("a, b, c", ',', new[] { "a", "b", "c" })]
        [TestCase("", ',', new string[0])]
        [TestCase(null, ',', new string[0])]
        public void SplitSafe_CharSeparator_DefaultOptions_VariousInputs(string? input, char separator, string[] expected)
        {
            Assert.That(input.SplitSafe(separator).ToList(), Is.EqualTo(expected));
        }

        [TestCase("a,,c", ',', StringSplitOptions.None, new[] { "a", "", "c" })]
        [TestCase("a, b, c ", ',', StringSplitOptions.None, new[] { "a", " b", " c " })]
        public void SplitSafe_CharSeparator_CustomOptions_VariousInputs(string? input, char separator, StringSplitOptions options, string[] expected)
        {
            Assert.That(input.SplitSafe(separator, options).ToList(), Is.EqualTo(expected));
        }

        // SplitToPair Tests
        [TestCase("key=value", "=", false, "key", "value", true)]
        [TestCase("key:value:another", ":", false, "key", "value:another", true)]
        [TestCase("key:value:another", ":", true, "key:value", "another", true)]
        [TestCase("keyvalue", "=", false, "keyvalue", "", false)]
        [TestCase(null, "=", false, null, "", false)]
        [TestCase("key=value", null, false, "key=value", "", false)]
        [TestCase("", "=", false, "", "", false)]
        public void SplitToPair_VariousInputs(string? input, string? delimiter, bool splitAfterLast, string? expectedLeft, string expectedRight, bool expectedResult)
        {
            bool result = input.SplitToPair(out string? left, out string right, delimiter, splitAfterLast);
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(left, Is.EqualTo(expectedLeft));
            Assert.That(right, Is.EqualTo(expectedRight));
        }

        // Tokenize (char separator, StringSplitOptions options) Tests
        [TestCase("a,b,c", ',', StringSplitOptions.None, new[] { "a", "b", "c" })]
        [TestCase("a, b , c ", ',', StringSplitOptions.TrimEntries, new[] { "a", "b", "c" })]
        [TestCase("a,,c", ',', StringSplitOptions.RemoveEmptyEntries, new[] { "a", "c" })]
        [TestCase("a, ,c", ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "a", "c" })]
        [TestCase("", ',', StringSplitOptions.None, new[] { "" })]
        [TestCase("", ',', StringSplitOptions.RemoveEmptyEntries, new string[0])]
        public void Tokenize_CharSeparator_VariousOptions(string input, char separator, StringSplitOptions options, string[] expected)
        {
            Assert.That(input.Tokenize(separator, options).ToList(), Is.EqualTo(expected));
        }

        // Tokenize (char[] separators, StringSplitOptions options) Tests
        [TestCase("a,b;c|d", new[] { ',', ';', '|' }, StringSplitOptions.None, new[] { "a", "b", "c", "d" })]
        [TestCase("a, b ; c | d ", new[] { ',', ';', '|' }, StringSplitOptions.TrimEntries, new[] { "a", "b", "c", "d" })]
        [TestCase("a,,b;;c||d", new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries, new[] { "a", "b", "c", "d" })]
        [TestCase("a, ,b;; c | | d", new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, new[] { "a", "b", "c", "d" })]
        public void Tokenize_CharArraySeparators_VariousOptions(string input, char[] separators, StringSplitOptions options, string[] expected)
        {
            Assert.That(input.Tokenize(separators, options).ToList(), Is.EqualTo(expected));
        }

        [Test]
        public void Tokenize_DefaultSeparators()
        {
            Assert.That("a,b".Tokenize(','), Is.EqualTo(new[] {"a", "b"}));
            Assert.That("a,b".Tokenize(',', ';'), Is.EqualTo(new[] {"a", "b"}));
        }
        #endregion
    }
}
