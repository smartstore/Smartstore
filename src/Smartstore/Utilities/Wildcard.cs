using System.Text;
using System.Text.RegularExpressions;

namespace Smartstore.Utilities
{
    /// <summary>
    /// This class is used to use wildcards and number ranges while
    /// searching in text. * is used for any chars, ? for one char and
    /// a number range is used with the - char. (12-232)
    /// </summary>
    public partial class Wildcard : Regex
    {
        [GeneratedRegex("[0-9]+-[0-9]+", RegexOptions.NonBacktracking)]
        private static partial Regex NumberRangeRegex();

        private readonly string _pattern;

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

        }

        internal Wildcard(string parsedPattern, RegexOptions options, TimeSpan matchTimeout)
            : base(parsedPattern, options, matchTimeout)
        {
            _pattern = parsedPattern;
        }

        public string Pattern
        {
            get => _pattern;
        }

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

            // convert the number ranges into regular expression
            if (parseNumberRanges)
            {
                var re = NumberRangeRegex();
                MatchCollection collection = re.Matches(pattern);
                foreach (var match in collection.Cast<Match>())
                {
                    var split = match.Value.Split(new char[] { '-' });
                    var min = split[0];
                    var max = split[1];
                    var term = NumberRangeRegexGenerator.Generate(min, max);

                    pattern = pattern.Replace(match.Value, term);
                }
            }

            return pattern;
        }

        #region Escaping

        /* --------------------------------------------------------------
		Stuff here partly copied over from .NET's internal RegexParser 
		class and modified for performance reasons: we don't want to escape 
		'[^]' chars, but Regex.Escape() does. Besides, wen need 
		'*' and '?' as wildcard chars.
		-------------------------------------------------------------- */

        const byte W = 6;    // wildcard char
        const byte Q = 5;    // quantifier
        const byte S = 4;    // ordinary stopper
        const byte Z = 3;    // ScanBlank stopper
        const byte X = 2;    // whitespace
        const byte E = 1;    // should be escaped

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
        {
            return ch <= '|' && _category[ch] >= E;
        }

        private static bool IsGlob(char ch)
        {
            return ch <= '|' && _category[ch] >= W;
        }

        private static string ToGlobPattern(string input)
        {
            Guard.NotNull(input, nameof(input));

            var sb = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (IsMetachar(input[i]))
                {
                    sb.Clear();
                    sb.Append('^');

                    char ch = input[i];
                    int lastpos;

                    sb.Append(input, 0, i);
                    do
                    {
                        if (IsGlob(ch))
                        {
                            sb.Append('.'); // '?' > '.'
                            if (ch == '*') sb.Append('*'); // '*' > '.*'
                        }
                        else
                        {
                            sb.Append('\\');
                            switch (ch)
                            {
                                case '\n':
                                    ch = 'n';
                                    break;
                                case '\r':
                                    ch = 'r';
                                    break;
                                case '\t':
                                    ch = 't';
                                    break;
                                case '\f':
                                    ch = 'f';
                                    break;
                            }
                            sb.Append(ch);
                        }

                        i++;
                        lastpos = i;

                        while (i < input.Length)
                        {
                            ch = input[i];
                            if (IsMetachar(ch))
                                break;

                            i++;
                        }

                        sb.Append(input, lastpos, i - lastpos);
                    } while (i < input.Length);

                    sb.Append('$');
                    return sb.ToString();
                }
            }

            return '^' + input + '$';
        }

        #endregion
    }

}
