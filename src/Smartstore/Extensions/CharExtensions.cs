using System.Runtime.CompilerServices;
using System.Text;

namespace Smartstore
{
    public static class CharExtensions
    {
        private const int _size = 256;
        private static readonly string[] _table = new string[_size];

        static CharExtensions()
        {
            for (int i = 0; i < _size; i++)
            {
                _table[i] = ((char)i).ToString();
            }
        }

        public static int ToInt(this char ch)
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

        public static string ToUnicode(this char ch)
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

        public static char RemoveDiacritic(this char ch)
        {
            var normalized = ch.AsString().Normalize(NormalizationForm.FormD);
            if (normalized.Length > 1)
            {
                return normalized[0];
            }

            return ch;
        }

        public static bool TryRemoveDiacritic(this char ch, out char normalized)
        {
            normalized = default;

            var formD = ch.AsString().Normalize(NormalizationForm.FormD);
            if (formD.Length > 1)
            {
                normalized = formD[0];
                return true;
            }

            return false;
        }

        public static bool IsInRange(this char ch, char a, char b)
            => ch >= a && ch <= b;

        /// <summary>
        /// Maps a char to a string while reducing memory allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this char ch)
        {
            string[] table = _table;
            if (ch < (uint)table.Length)
            {
                return table[ch];
            }

            return ch.ToString();
        }
    }
}
