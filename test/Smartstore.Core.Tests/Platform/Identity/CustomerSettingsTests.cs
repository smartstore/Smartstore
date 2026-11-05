using NUnit.Framework;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.Identity;

[TestFixture]
public class CustomerSettingsTests
{
    [TestCase(CustomerLoginType.Username, CustomerNameFormat.ShowFirstName, true)]
    [TestCase(CustomerLoginType.UsernameOrEmail, CustomerNameFormat.ShowFirstName, true)]
    [TestCase(CustomerLoginType.Email, CustomerNameFormat.ShowFirstName, false)]
    [TestCase(CustomerLoginType.Email, CustomerNameFormat.ShowUsernames, true)]
    public void Can_use_usernames(CustomerLoginType loginType, CustomerNameFormat nameFormat, bool expected)
    {
        var settings = new CustomerSettings
        {
            CustomerLoginType = loginType,
            CustomerNameFormat = nameFormat
        };

        settings.CanUseUsernames().ShouldEqual(expected);
    }
}