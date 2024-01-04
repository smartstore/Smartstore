using NUnit.Framework;

namespace Smartstore.Core.Tests.Data
{
    [TestFixture]
    public class DataProviderTests : ServiceTestBase
    {
        [TestCase("test_database-5.0.0.0-20220323102033.bak", true, 5, 0, 2022, 3, 23)]
        [TestCase("test_database-5.1.0.0-20210319100510-3.bak", true, 5, 1, 2021, 3, 19)]
        [TestCase("test_database-4.2.0.0-201905111020334-3.bak", true, 4, 2, 2019, 5, 11)]
        [TestCase("test_database-5.1.0.0-20220323102033", false, 5, 1, 2022, 3, 23)]
        [TestCase("test_database-5.1.0.0-20220323102033.log", false, 5, 1, 2022, 3, 23)]
        [TestCase("test_database-20220323102033.bak", false, 5, 1, 2022, 3, 23)]
        public void Can_validate_db_backup_filename(string name, bool valid, int major, int minor, int year, int month, int day)
        {
            var result = DbContext.DataProvider.ValidateBackupFileName(name);

            Assert.That(valid, Is.EqualTo(result.IsValid));

            if (result.IsValid)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(major, Is.EqualTo(result.Version.Major));
                    Assert.That(minor, Is.EqualTo(result.Version.Minor));
                    Assert.That(year, Is.EqualTo(result.Timestamp.Year));
                    Assert.That(month, Is.EqualTo(result.Timestamp.Month));
                    Assert.That(day, Is.EqualTo(result.Timestamp.Day));
                });
            }
        }
    }
}
