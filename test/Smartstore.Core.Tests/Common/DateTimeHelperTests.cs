using System;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class DateTimeHelperTests : ServiceTest
    {
        IWorkContext _workContext;
        ISettingService _settingService;   
        DateTimeSettings _dateTimeSettings;
        IDateTimeHelper _dateTimeHelper;

        Customer _customer;

        [SetUp]
        public new void SetUp()
        {
            _dateTimeSettings = new DateTimeSettings
            {
                AllowCustomersToSetTimeZone = false,
                DefaultStoreTimeZoneId = string.Empty
            };

            _customer = new Customer
            {
                Id = 1,
                TimeZoneId = "E. Europe Standard Time"
            };

            var settingServiceWrapper = new Mock<ISettingService>();
            _settingService = settingServiceWrapper.Object;

            var workContextWrapper = new Mock<IWorkContext>();
            _workContext = workContextWrapper.Object;

            _dateTimeHelper = new DateTimeHelper(DbContext, _settingService, _workContext, _dateTimeSettings);
        }

        [Test]
        public void Can_find_systemTimeZone_by_id()
        {
            var timeZones = _dateTimeHelper.FindTimeZoneById("E. Europe Standard Time");
            timeZones.ShouldNotBeNull();
            timeZones.Id.ShouldEqual("E. Europe Standard Time");
        }

        [Test]
        public void Can_get_all_systemTimeZones()
        {
            var systemTimeZones = _dateTimeHelper.GetSystemTimeZones();
            systemTimeZones.ShouldNotBeNull();
            (systemTimeZones.Count > 0).ShouldBeTrue();
        }

        [Test]
        public void Can_get_customer_timeZone_with_customTimeZones_enabled()
        {
            _dateTimeSettings.AllowCustomersToSetTimeZone = true;
            _dateTimeSettings.DefaultStoreTimeZoneId = "Central Standard Time";

            var timeZone = _dateTimeHelper.GetCustomerTimeZone(_customer);
            timeZone.ShouldNotBeNull();
            timeZone.Id.ShouldEqual("E. Europe Standard Time");
        }

        [Test]
        public void Can_get_customer_timeZone_with_customTimeZones_disabled()
        {
            _dateTimeSettings.AllowCustomersToSetTimeZone = false;
            _dateTimeSettings.DefaultStoreTimeZoneId = "Central Standard Time";

            var timeZone = _dateTimeHelper.GetCustomerTimeZone(_customer);
            timeZone.ShouldNotBeNull();
            timeZone.Id.ShouldEqual("Central Standard Time");
        }

        [Test]
        public void Can_convert_dateTime_to_userTime()
        {
            var sourceDateTime = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            sourceDateTime.ShouldNotBeNull();

            var destinationDateTime = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            destinationDateTime.ShouldNotBeNull();

            // Berlin > Istanbul
            _dateTimeHelper
                .ConvertToUserTime(new DateTime(2015, 06, 1, 0, 0, 0), sourceDateTime, destinationDateTime)
                .ShouldEqual(new DateTime(2015, 06, 1, 1, 0, 0));

            // UTC > Istanbul (summer)
            _dateTimeHelper
                .ConvertToUserTime(new DateTime(2015, 06, 1, 0, 0, 0), TimeZoneInfo.Utc, destinationDateTime)
                .ShouldEqual(new DateTime(2015, 06, 1, 3, 0, 0));

            // UTC > Istanbul (winter)
            _dateTimeHelper
                .ConvertToUserTime(new DateTime(2015, 01, 01, 0, 0, 0), TimeZoneInfo.Utc, destinationDateTime)
                .ShouldEqual(new DateTime(2015, 01, 1, 2, 0, 0));
        }

        [Test]
        public void Can_convert_dateTime_to_utc_dateTime()
        {
            var sourceDateTime = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time"); //(GMT+02:00) Minsk;
            sourceDateTime.ShouldNotBeNull();

            //summer time
            var dateTime1 = new DateTime(2010, 06, 01, 0, 0, 0);
            var convertedDateTime1 = _dateTimeHelper.ConvertToUtcTime(dateTime1, sourceDateTime);
            convertedDateTime1.ShouldEqual(new DateTime(2010, 05, 31, 21, 0, 0));

            //winter time
            var dateTime2 = new DateTime(2010, 01, 01, 0, 0, 0);
            var convertedDateTime2 = _dateTimeHelper.ConvertToUtcTime(dateTime2, sourceDateTime);
            convertedDateTime2.ShouldEqual(new DateTime(2009, 12, 31, 22, 0, 0));
        }
    }
}