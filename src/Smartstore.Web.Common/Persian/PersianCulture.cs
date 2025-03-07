using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace SmartStore.Web.Framework.Persian
{
    public class PersianCulture : CultureInfo
    {
        private readonly System.Globalization.Calendar cal;
        private readonly System.Globalization.Calendar[] optionals;
        public PersianCulture()
            : this("fa-IR", true)
        { }

        public PersianCulture(string cultureName, bool useUserOverride)
            : base(cultureName, useUserOverride)
        {
            cal = base.OptionalCalendars[0];
            var optionalCalendars = new List<System.Globalization.Calendar>();
            optionalCalendars.AddRange(base.OptionalCalendars);
            optionalCalendars.Insert(0, new PersianCalendar());
            Type formatType = typeof(DateTimeFormatInfo);
            Type calendarType = typeof(System.Globalization.Calendar);
            PropertyInfo idProperty = calendarType.GetProperty("ID", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo optionalCalendarfield = formatType.GetField("optionalCalendars", BindingFlags.Instance | BindingFlags.NonPublic);
            var newOptionalCalendarIDs = new Int32[optionalCalendars.Count];

            for (int i = 0; i < newOptionalCalendarIDs.Length; i++)
                newOptionalCalendarIDs[i] = (Int32)idProperty.GetValue(optionalCalendars[i], null);

            optionalCalendarfield.SetValue(DateTimeFormat, newOptionalCalendarIDs);

            optionals = optionalCalendars.ToArray();

            cal = optionals[0];

            DateTimeFormat.Calendar = optionals[0];

            DateTimeFormat.MonthNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };
            DateTimeFormat.MonthGenitiveNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };
            DateTimeFormat.AbbreviatedMonthNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };
            DateTimeFormat.AbbreviatedMonthGenitiveNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };
            DateTimeFormat.AbbreviatedDayNames = new string[] { "ی", "د", "س", "چ", "پ", "ج", "ش" };
            DateTimeFormat.ShortestDayNames = new string[] { "ی", "د", "س", "چ", "پ", "ج", "ش" };
            DateTimeFormat.DayNames = new string[] { "یکشنبه", "دوشنبه", "ﺳﻪشنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" };
            DateTimeFormat.AMDesignator = "ق.ظ";
            DateTimeFormat.PMDesignator = "ب.ظ";
            DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
            DateTimeFormat.ShortTimePattern = "HH:mm";
            DateTimeFormat.LongTimePattern = "HH:mm:ss";
            DateTimeFormat.FullDateTimePattern = "yyyy/MM/dd HH:mm:ss";
        }

        public override System.Globalization.Calendar Calendar
        {
            get { return cal; }
        }

        public override System.Globalization.Calendar[] OptionalCalendars
        {
            get { return optionals; }
        }

        public static DateTime PersianToGregorianUS(DateTime faDate)
        {
            return new PersianCalendar().ToDateTime(faDate.Year, faDate.Month, faDate.Day, faDate.Hour, faDate.Minute, faDate.Second, faDate.Millisecond);
        }


        public static bool IsPersianCulture()
        {
            var service = EngineContext.Current.ResolveService<ICommonServices>();
            var lang = service?.WorkContext?.WorkingLanguage;
            if (lang == null)
            {
                return false;
            }
            return lang.LanguageCulture == "fa-IR";
        }

    }
}