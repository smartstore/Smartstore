using System.Text;
using System.Text.RegularExpressions;

namespace Smartstore.Utilities;

/// <summary>
/// This class is used to use wildcards and number ranges while
/// searching in text. * is used for any chars, ? for one char and
/// a number range is used with the - char. (12-232)
/// </summary>
public sealed class Wildcard : Regex
{
    // Avoid capturing groups; keep compiled + non-backtracking for perf and safety.
    private static readonly Regex _rgNumberRange =
        new(@"[0-9]+-[0-9]+", RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);

    /// <summary>
    /// Initializes a new instance of the <see cref="Wildcard"/> class.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <param name="parseNumberRanges">
    /// Specifies whether number ranges (e.g. 1234-5678) should
    /// be converted to a regular expression pattern.
    /// </param>
    public Wildcard(string pattern, bool parseNumberRanges = false)
        : this(pattern, RegexOptions.None, parseNumberRanges)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wildcard"/> class.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <param name="parseNumberRanges">
    /// Specifies whether number ranges (e.g. 1234-5678) should
    /// be converted to a regular expression pattern.
    /// </param>
    /// <param name="options">The regular expression options.</param>
    public Wildcard(string pattern, RegexOptions options, bool parseNumberRanges = false)
        : this(WildcardToRegex(pattern, parseNumberRanges), options, Timeout.InfiniteTimeSpan)
    {
        RawPattern = pattern;
    }

    internal Wildcard(string parsedPattern, RegexOptions options, TimeSpan matchTimeout)
        : base(parsedPattern, options, matchTimeout)
    {
        Pattern = parsedPattern;
    }

    public string RawPattern { get; }
    public string Pattern { get; }

    /// <summary>
    /// Searches all number range terms and converts them
    /// to a regular expression term.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <returns>A converted regular expression term.</returns>
    private static string WildcardToRegex(string pattern, bool parseNumberRanges)
    {
        // Replace ? with . and * with .*
        // Prepend ^, append $
        // Escape all chars except []^
        pattern = ToGlobPattern(pattern);

        if (!parseNumberRanges)
        {
            return pattern;
        }

        // Fast path: if there's no dash, there can't be a range.
        // (Keeps behavior, just avoids regex allocation/work.)
        if (pattern.IndexOf('-', StringComparison.Ordinal) < 0)
        {
            return pattern;
        }

        // Convert number ranges without:
        // - LINQ/Cast allocations
        // - repeated string.Replace() (O(n*m))
        // - Split allocations
        var matches = _rgNumberRange.Matches(pattern);
        if (matches.Count == 0)
        {
            return pattern;
        }

        var sb = new StringBuilder(pattern.Length + 16);
        int last = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];

            // Append text before the match
            sb.Append(pattern, last, m.Index - last);

            // Parse "min-max" without Split
            var value = m.Value;
            int dash = value.IndexOf('-', StringComparison.Ordinal);
            var min = value[..dash];
            var max = value[(dash + 1)..];

            sb.Append(NumberRangeRegexGenerator.Generate(min, max));

            last = m.Index + m.Length;
        }

        // Append remaining tail
        sb.Append(pattern, last, pattern.Length - last);

        return sb.ToString();
    }

    #region Escaping

    /* --------------------------------------------------------------
		Stuff here partly copied over from .NET's internal RegexParser
		class and modified for performance reasons: we don't want to escape
		'[^]' chars, but Regex.Escape() does. Besides, wen need
		'*' and '?' as wildcard chars.
		-------------------------------------------------------------- */

    private const byte W = 6;    // wildcard char
    private const byte Q = 5;    // quantifier
    private const byte S = 4;    // ordinary stopper
    private const byte Z = 3;    // ScanBlank stopper
    private const byte X = 2;    // whitespace
    private const byte E = 1;    // should be escaped

    /*
     * For categorizing ASCII characters.
     */
    private static readonly byte[] _category = new byte[]
    {
        // 0 1 2 3 4 5 6 7 8 9 A B C D E F 0 1 2 3 4 5 6 7 8 9 A B C D E F
           0,0,0,0,0,0,0,0,0,X,X,0,X,X,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        //   ! " # $ % & ' ( ) * + , - . / 0 1 2 3 4 5 6 7 8 9 : ; < = > ?
           X,0,0,Z,S,0,0,0,S,S,W,Q,0,0,S,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,W,
        // @ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z [ \ ] ^ _
           0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,S,0,0,0,
        // ' a b c d e f g h i j k l m n o p q r s t u v w x y z { | } ~
           0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,S,0,0,0
    };

    private static bool IsMetachar(char ch)
        => ch <= '|' && _category[ch] >= E;

    private static bool IsGlob(char ch)
        => ch <= '|' && _category[ch] >= W;

    private static string ToGlobPattern(string input)
    {
        Guard.NotNull(input);

        // Fast path: if there are no metacharacters, just add anchors.
        for (int i = 0; i < input.Length; i++)
        {
            if (IsMetachar(input[i]))
            {
                // Rare path: build escaped regex
                // Approx capacity: anchors + input + escapes. Keep it simple but avoid growth.
                var sb = new StringBuilder(input.Length + 2);
                sb.Append('^');

                char ch = input[i];
                int lastpos;

                sb.Append(input, 0, i);
                do
                {
                    if (IsGlob(ch))
                    {
                        sb.Append('.'); // '?' -> '.'
                        if (ch == '*')
                        {
                            sb.Append('*'); // '*' -> '.*'
                        }
                    }
                    else
                    {
                        sb.Append('\\');
                        switch (ch)
                        {
                            case '\n': ch = 'n'; break;
                            case '\r': ch = 'r'; break;
                            case '\t': ch = 't'; break;
                            case '\f': ch = 'f'; break;
                        }
                        sb.Append(ch);
                    }

                    i++;
                    lastpos = i;

                    while (i < input.Length)
                    {
                        ch = input[i];
                        if (IsMetachar(ch))
                        {
                            break;
                        }

                        i++;
                    }

                    sb.Append(input, lastpos, i - lastpos);
                }
                while (i < input.Length);

                sb.Append('$');
                return sb.ToString();
            }
        }

        // No metacharacters => just anchor, no StringBuilder needed.
        return string.Concat("^", input, "$");
    }

    #endregion
}
