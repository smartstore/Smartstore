using NUnit.Framework;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.Identity
{
    [TestFixture]
    public class CustomerExtensionTests
    {
        private Customer _customer;

        private CustomerRoleMapping _crmRegistered;
        private CustomerRoleMapping _crmGuests;
        private CustomerRoleMapping _crmAdmin;
        private CustomerRoleMapping _crmForumAdmin;

        [SetUp]
        public virtual void Setup()
        {
            _customer = new Customer();
            _crmRegistered = new() { CustomerRole = new CustomerRole { Active = true, Name = "Registered", SystemName = SystemCustomerRoleNames.Registered } };
            _crmGuests = new() { CustomerRole = new CustomerRole { Active = true, Name = "Guests", SystemName = SystemCustomerRoleNames.Guests } };
            _crmAdmin = new() { CustomerRole = new CustomerRole { Active = true, Name = "Administrators", SystemName = SystemCustomerRoleNames.Administrators } };
            _crmForumAdmin = new() { CustomerRole = new CustomerRole { Active = true, Name = "ForumModerators", SystemName = SystemCustomerRoleNames.ForumModerators } };
        }

        [Test]
        public void Can_check_IsInCustomerRole()
        {
            _customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerRole = new CustomerRole
                {
                    Active = true,
                    Name = "Test name 1",
                    SystemName = "Test system name 1",
                }
            });
            _customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerRole = new CustomerRole
                {
                    Active = false,
                    Name = "Test name 2",
                    SystemName = "Test system name 2",
                }
            });

            _customer.IsInRole("Test system name 1", false).ShouldBeTrue();
            _customer.IsInRole("Test system name 1", true).ShouldBeTrue();

            _customer.IsInRole("Test system name 2", false).ShouldBeTrue();
            _customer.IsInRole("Test system name 2", true).ShouldBeFalse();

            _customer.IsInRole("Test system name 3", false).ShouldBeFalse();
            _customer.IsInRole("Test system name 3", true).ShouldBeFalse();
        }

        [Test]
        public void Can_check_whether_customer_is_admin()
        {
            _customer.CustomerRoleMappings.Add(_crmRegistered);
            _customer.CustomerRoleMappings.Add(_crmGuests);
            _customer.IsAdmin().ShouldBeFalse();

            _customer.CustomerRoleMappings.Add(_crmAdmin);
            _customer.IsAdmin().ShouldBeTrue();
        }

        [Test]
        public void Can_check_whether_customer_is_forum_moderator()
        {
            _customer.CustomerRoleMappings.Add(_crmRegistered);
            _customer.CustomerRoleMappings.Add(_crmGuests);
            _customer.IsInRole("ForumModerators").ShouldBeFalse();

            _customer.CustomerRoleMappings.Add(_crmForumAdmin);
            _customer.IsInRole("ForumModerators").ShouldBeTrue();
        }

        [Test]
        public void Can_check_whether_customer_is_guest()
        {
            _customer.CustomerRoleMappings.Add(_crmRegistered);
            _customer.CustomerRoleMappings.Add(_crmAdmin);
            _customer.IsGuest().ShouldBeFalse();

            _customer.CustomerRoleMappings.Add(_crmGuests);
            _customer.IsGuest().ShouldBeTrue();
        }

        [Test]
        public void Can_check_whether_customer_is_registered()
        {
            _customer.CustomerRoleMappings.Add(_crmAdmin);
            _customer.CustomerRoleMappings.Add(_crmGuests);
            _customer.IsRegistered().ShouldBeFalse();

            _customer.CustomerRoleMappings.Add(_crmRegistered);
            _customer.IsRegistered().ShouldBeTrue();
        }
    }
}