using System.Runtime.CompilerServices;
using System.Text;

namespace Smartstore;

public static class CharExtensions
{
    private const int _size = 256;
    private static readonly string[] _table = new string[_size];

    // Prebuilt base-char table for U+0080–U+1EFF (Latin, Greek, Cyrillic, Vietnamese, …).
    // Value '\0' means no diacritic. Built once via Normalize(FormD), looked up O(1) at runtime.
    private const char _diacriticBase = '\u0080';
    private static readonly char[] _diacriticTable;

    static CharExtensions()
    {
        for (int i = 0; i < _size; i++)
        {
            _table[i] = ((char)i).ToString();
        }

        var diacriticTableSize = '\u1F00' - _diacriticBase; // 7808 entries ≈ 15 KB
        _diacriticTable = new char[diacriticTableSize];
        for (int i = 0; i < diacriticTableSize; i++)
        {
            var c = (char)(_diacriticBase + i);
            var formD = c.ToString().Normalize(NormalizationForm.FormD);
            if (formD.Length > 1)
                _diacriticTable[i] = formD[0];
            // else stays '\0' — no diacritic
        }
    }

    extension(char ch)
    {
        public int ToInt()
        {
            if (ch >= '0' && ch <= '9')
            {
                return ch - '0';
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                return (ch - 'a') + 10;
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                return (ch - 'A') + 10;
            }

            return -1;
        }

        public string ToUnicode()
        {
            var chars = new[]
            {
                '\\',
                'u',
                ((ch >> 12) & '\x000f').ToHex(),
                ((ch >> 8) & '\x000f').ToHex(),
                ((ch >> 4) & '\x000f').ToHex(),
                (ch & '\x000f').ToHex()
            };

            return new string(chars);
        }

        public char RemoveDiacritic()
            => TryRemoveDiacritic(ch, out var b) ? b : ch;

        public bool TryRemoveDiacritic(out char normalized)
        {
            var idx = (uint)(ch - _diacriticBase);
            if (idx < (uint)_diacriticTable.Length)
            {
                var b = _diacriticTable[idx];
                if (b != '\0')
                {
                    normalized = b;
                    return true;
                }
            }
            else
            {
                var formD = ch.AsString().Normalize(NormalizationForm.FormD);
                if (formD.Length > 1)
                {
                    normalized = formD[0];
                    return true;
                }
            }

            normalized = default;
            return false;
        }

        public bool IsInRange(char a, char b)
            => ch >= a && ch <= b;

        /// <summary>
        /// Maps a char to a string while reducing memory allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AsString()
        {
            string[] table = _table;
            if (ch < (uint)table.Length)
            {
                return table[ch];
            }

            return ch.ToString();
        }

        /// <summary>
        /// Determines if the given character is a line break character as
        /// specified here:
        /// http://www.w3.org/TR/html401/struct/text.html#h-9.3.2
        /// </summary>
        /// <param name="c">The character to examine.</param>
        /// <returns>The result of the test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLineBreak()
            => ch == '\n' || ch == '\r';
    }
}