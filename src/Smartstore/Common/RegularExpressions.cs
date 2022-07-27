using System.Text.RegularExpressions;

namespace Smartstore
{
    public static class RegularExpressions
    {
        internal static readonly string ValidRealPattern = "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$";
        internal static readonly string ValidIntegerPattern = "^([-]|[0-9])[0-9]*$";

        internal static readonly Regex HasTwoDot = new("[0-9]*[.][0-9]*[.][0-9]*", RegexOptions.Compiled);
        internal static readonly Regex HasTwoMinus = new("[0-9]*[-][0-9]*[-][0-9]*", RegexOptions.Compiled);

        public static readonly Regex IsDigit = new(@"\d", RegexOptions.Compiled);
        public static readonly Regex IsAlpha = new("[^a-zA-Z]", RegexOptions.Compiled);
        public static readonly Regex IsAlphaNumeric = new("[^a-zA-Z0-9]", RegexOptions.Compiled);
        public static readonly Regex IsNotNumber = new("[^0-9.-]", RegexOptions.Compiled);
        public static readonly Regex IsPositiveInteger = new(@"^\d{1,10}", RegexOptions.Compiled);
        public static readonly Regex IsNumeric = new("(" + ValidRealPattern + ")|(" + ValidIntegerPattern + ")", RegexOptions.Compiled);

        //      //public static readonly Regex IsWebUrl = new Regex(@"(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Singleline | RegexOptions.Compiled);
        ///// <remarks>see https://msdn.microsoft.com/en-us/library/ms998267.aspx</remarks>
        //public static readonly Regex IsWebUrl = new (@"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_\=~]*)?$", RegexOptions.Singleline | RegexOptions.Compiled);

        public static readonly Regex IsEmail = new("^(?:[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+\\.)*[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!\\.)){0,61}[a-zA-Z0-9]?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\\[(?:(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\.){3}(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\]))$", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex IsGuid = new(@"\{?[a-fA-F0-9]{8}(?:-(?:[a-fA-F0-9]){4}){3}-[a-fA-F0-9]{12}\}?", RegexOptions.Compiled);
        public static readonly Regex IsBase64Guid = new(@"[a-zA-Z0-9+/=]{22,24}", RegexOptions.Compiled);

        public static readonly Regex IsCultureCode = new(@"^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.Singleline | RegexOptions.Compiled);

        public static readonly Regex IsYearRange = new(@"^(\d{4})-(\d{4})$", RegexOptions.Compiled);

        public static readonly Regex IsIban = new(@"[a-zA-Z]{2}[0-9]{2}[a-zA-Z0-9]{4}[0-9]{7}([a-zA-Z0-9]?){0,16}", RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly Regex IsBic = new(@"([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly Regex IsMinFile = new(@"^.+\.min(\.[^\.]*)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly Regex IsCvv = new(@"^[0-9]{3,4}$", RegexOptions.Singleline | RegexOptions.Compiled);
    }
}
