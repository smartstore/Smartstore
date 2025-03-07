using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;
namespace SmartStore.Web.Framework.Persian
{
    public static class PersianDateExtensionMethods
    {
        private static CultureInfo _Culture;
        public static CultureInfo GetPersianCulture()
        {
            if (_Culture == null)
            {
                _Culture = new CultureInfo("fa-IR");
                DateTimeFormatInfo formatInfo = _Culture.DateTimeFormat;
                formatInfo.AbbreviatedDayNames = new[] { "ی", "د", "س", "چ", "پ", "ج", "ش" };
                formatInfo.DayNames = new[] { "یکشنبه", "دوشنبه", "سه شنبه", "چهار شنبه", "پنجشنبه", "جمعه", "شنبه" };
                var monthNames = new[]
                {
                    "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن",
                    "اسفند",
                    ""
                };
                formatInfo.AbbreviatedMonthNames =
                    formatInfo.MonthNames =
                    formatInfo.MonthGenitiveNames = formatInfo.AbbreviatedMonthGenitiveNames = monthNames;
              formatInfo.AMDesignator = "ق.ظ";
                formatInfo.PMDesignator = "ب.ظ";
                formatInfo.ShortDatePattern = "yyyy/MM/dd";
                formatInfo.ShortTimePattern = "HH:mm";
                formatInfo.LongDatePattern = "dddd, dd MMMM,yyyy";
                  formatInfo.LongTimePattern = "HH:mm:ss";
                formatInfo.FullDateTimePattern = "yyyy/MM/dd HH:mm:ss";
                formatInfo.FirstDayOfWeek = DayOfWeek.Saturday;
                System.Globalization.Calendar cal = new PersianCalendar();

                FieldInfo fieldInfo = _Culture.GetType().GetField("calendar", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                    fieldInfo.SetValue(_Culture, cal);

                FieldInfo info = formatInfo.GetType().GetField("calendar", BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null)
                    info.SetValue(formatInfo, cal);

                _Culture.NumberFormat.NumberDecimalSeparator = "/";
                _Culture.NumberFormat.DigitSubstitution = DigitShapes.NativeNational;
                _Culture.NumberFormat.NumberNegativePattern = 0;
            }
            return _Culture;
        }

        public static string ToPeString(this DateTime date,string format = "yyyy/MM/dd HH:mm")
        {
            return  date.ToString(format,GetPersianCulture());
        }
        public static DateTime? ConvertDateTime(string date)
        {
            var persianCal = new PersianCalendar();
            try
            {
                // Convert Persian digits to English digits
                date = date.Replace("۰", "0")
                           .Replace("۱", "1")
                           .Replace("۲", "2")
                           .Replace("۳", "3")
                           .Replace("۴", "4")
                           .Replace("۵", "5")
                           .Replace("۶", "6")
                           .Replace("۷", "7")
                           .Replace("۸", "8")
                           .Replace("۹", "9");

                if (date.Length != 12)
                {
                    List<string> splitDate = date.Split(new char[] { '/', '\\', '-', ' ', ':' }).ToList();
                    splitDate.RemoveAll(x => string.IsNullOrEmpty(x.Trim()));
                    if (splitDate[0].Length == 4) { date = splitDate[0]; } else if (splitDate[0].Length == 2) { date = "13" + splitDate[0]; }
                    int intyear1 = 0;
                    int.TryParse(splitDate[0], out intyear1);
                    int intMon1 = 0;
                    int.TryParse(splitDate[1], out intMon1);
                    int intDay1 = 0;
                    int.TryParse(splitDate[2], out intDay1);
                    int intHour1 = 0;
                    if (splitDate.Count > 3)
                        int.TryParse(splitDate[3], out intHour1);
                    int intMin1 = 0;
                    if (splitDate.Count > 4)
                        int.TryParse(splitDate[4], out intMin1);
                    int intsec = 0;
                    if (splitDate.Count > 5)
                        int.TryParse(splitDate[5], out intsec);

                    return persianCal.ToDateTime(intyear1, intMon1, intDay1, intHour1, intMin1, intsec, 0);
                }
                int intyear = int.Parse(date.Substring(0, 4));
                int intMon = int.Parse(date.Substring(4, 2));
                int intDay = int.Parse(date.Substring(6, 2));
                int intHour = int.Parse(date.Substring(8, 2));
                int intMin = int.Parse(date.Substring(10, 2));
                return persianCal.ToDateTime(intyear, intMon, intDay, intHour, intMin, 0, 0);
            }
            catch
            {
                return null;
            }
        }
    }
}