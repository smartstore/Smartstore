using System.Diagnostics;
using System.Globalization;

namespace Smartstore.WebApi.Client
{
    internal static class StringExtensions
    {
        public static void Dump(this string value, bool appendMarks = false)
        {
            Debug.WriteLine(value);
            Debug.WriteLineIf(appendMarks, "------------------------------------------------");
        }

        public static DialogResult Box(this string message, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            return MessageBox.Show(message, Program.AppName, buttons, icon);
        }

        public static int ToInt(this string value, int defaultValue = 0)
        {
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        public static string EmptyNull(this string value)
        {
            return (value ?? string.Empty).Trim();
        }

        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static string FormatInvariant(this string format, params object[] objects)
        {
            return string.Format(CultureInfo.InvariantCulture, format, objects);
        }
    }
}
