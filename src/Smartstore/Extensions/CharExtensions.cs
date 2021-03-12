using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Smartstore
{
    public static class CharExtensions
    {
        [DebuggerStepThrough]
        public static int ToInt(this char value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }
            else if (value >= 'a' && value <= 'f')
            {
                return (value - 'a') + 10;
            }
            else if (value >= 'A' && value <= 'F')
            {
                return (value - 'A') + 10;
            }

            return -1;
        }

        [DebuggerStepThrough]
        public static string ToUnicode(this char c)
        {
            using (var w = new StringWriter(CultureInfo.InvariantCulture))
            {
                WriteCharAsUnicode(c, w);
                return w.ToString();
            }
        }

        internal static void WriteCharAsUnicode(char c, TextWriter writer)
        {
            Guard.NotNull(writer, "writer");

            char h1 = ((c >> 12) & '\x000f').ToHex();
            char h2 = ((c >> 8) & '\x000f').ToHex();
            char h3 = ((c >> 4) & '\x000f').ToHex();
            char h4 = (c & '\x000f').ToHex();

            writer.Write('\\');
            writer.Write('u');
            writer.Write(h1);
            writer.Write(h2);
            writer.Write(h3);
            writer.Write(h4);
        }

        public static char TryRemoveDiacritic(this char c)
        {
            var normalized = c.ToString().Normalize(NormalizationForm.FormD);
            if (normalized.Length > 1)
            {
                return normalized[0];
            }

            return c;
        }
    }
}
