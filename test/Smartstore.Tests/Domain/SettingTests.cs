using NUnit.Framework;
using Smartstore.Core.Configuration;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class SettingTestFixture
    {
        [Test]
        public void Can_create_setting()
        {
            var setting = new Setting
            {
                Name = "Setting1",
                Value = "Value1"
            };

            setting.Name.ShouldEqual("Setting1");
            setting.Value.ShouldEqual("Value1");
        }
    }
}
