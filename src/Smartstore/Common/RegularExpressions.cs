using System.Text.RegularExpressions;

namespace Smartstore
{
    public static partial class RegularExpressions
    {
        #region GeneratedRegex

        [GeneratedRegex("\\d", RegexOptions.Compiled)]
        private static partial Regex IsDigitRegex();

        [GeneratedRegex("[^a-zA-Z]", RegexOptions.Compiled)]
        private static partial Regex IsAlphaRegex();

        [GeneratedRegex("[^a-zA-Z0-9]", RegexOptions.Compiled)]
        private static partial Regex IsAlphaNumericRegex();

        [GeneratedRegex("[^0-9.-]", RegexOptions.Compiled)]
        private static partial Regex IsNotNumberRegex();

        [GeneratedRegex("^\\d{1,10}", RegexOptions.Compiled)]
        private static partial Regex IsPositiveIntegerRegex();

        [GeneratedRegex("^(?:[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+\\.)*[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!\\.)){0,61}[a-zA-Z0-9]?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\\[(?:(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\.){3}(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\]))$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsEmailRegex();

        [GeneratedRegex("\\{?[a-fA-F0-9]{8}(?:-(?:[a-fA-F0-9]){4}){3}-[a-fA-F0-9]{12}\\}?", RegexOptions.Compiled)]
        private static partial Regex IsGuidRegex();

        [GeneratedRegex("[a-zA-Z0-9+/=]{22,24}", RegexOptions.Compiled)]
        private static partial Regex IsBase64GuidRegex();

        [GeneratedRegex("^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex IsCultureCodeRegex();

        [GeneratedRegex("^(\\d{4})-(\\d{4})$", RegexOptions.Compiled)]
        private static partial Regex IsYearRangeRegex();

        [GeneratedRegex("[a-zA-Z]{2}[0-9]{2}[a-zA-Z0-9]{4}[0-9]{7}([a-zA-Z0-9]?){0,16}", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex IsIbanRegex();

        [GeneratedRegex("([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex IsBicRegex();

        [GeneratedRegex("^.+\\.min(\\.[^\\.]*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex IsMinFileRegex();

        [GeneratedRegex("^[0-9]{3,4}$", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex IsCvvRegex();

        [GeneratedRegex("[0-9]*[.][0-9]*[.][0-9]*", RegexOptions.Compiled)]
        private static partial Regex HasTwoDotRegex();

        [GeneratedRegex("[0-9]*[-][0-9]*[-][0-9]*", RegexOptions.Compiled)]
        private static partial Regex HasTwoMinusRegex();

        #endregion

        internal static readonly string ValidRealPattern = "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$";
        internal static readonly string ValidIntegerPattern = "^([-]|[0-9])[0-9]*$";
        public static readonly Regex IsNumeric = new("(" + ValidRealPattern + ")|(" + ValidIntegerPattern + ")", RegexOptions.Compiled);

        internal static readonly Regex HasTwoDot = HasTwoDotRegex();
        internal static readonly Regex HasTwoMinus = HasTwoMinusRegex();

        public static readonly Regex IsDigit = IsDigitRegex();
        public static readonly Regex IsAlpha = IsAlphaRegex();
        public static readonly Regex IsAlphaNumeric = IsAlphaNumericRegex();
        public static readonly Regex IsNotNumber = IsNotNumberRegex();
        public static readonly Regex IsPositiveInteger = IsPositiveIntegerRegex();
        public static readonly Regex IsEmail = IsEmailRegex();
        public static readonly Regex IsGuid = IsGuidRegex();
        public static readonly Regex IsBase64Guid = IsBase64GuidRegex();
        public static readonly Regex IsCultureCode = IsCultureCodeRegex();
        public static readonly Regex IsYearRange = IsYearRangeRegex();
        public static readonly Regex IsIban = IsIbanRegex();
        public static readonly Regex IsBic = IsBicRegex();
        public static readonly Regex IsMinFile = IsMinFileRegex();
        public static readonly Regex IsCvv = IsCvvRegex();
    }
}
