using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class CustomerTests
    {
        [Test]
        public void New_customer_has_clear_password_type()
        {
            var customer = new Customer();
            customer.PasswordFormat.ShouldEqual(PasswordFormat.Clear);
        }

        [Test]
        public void Can_add_address()
        {
            var customer = new Customer();
            var address = new Address { Id = 1 };

            customer.Addresses.Add(address);

            customer.Addresses.Count.ShouldEqual(1);
            customer.Addresses.First().Id.ShouldEqual(1);
        }

        [Test]
        public void Can_remove_address_assigned_as_billing_address()
        {
            var customer = new Customer();
            var address = new Address { Id = 1 };

            customer.Addresses.Add(address);
            customer.BillingAddress = address;

            Assert.That(customer.BillingAddress, Is.SameAs(customer.Addresses.First()));

            customer.RemoveAddress(address);
            customer.Addresses.Count.ShouldEqual(0);
            customer.BillingAddress.ShouldBeNull();
        }

        [Test]
        public void Can_add_rewardPointsHistoryEntry()
        {
            var customer = new Customer();
            customer.AddRewardPointsHistoryEntry(1, "Points for registration");

            customer.RewardPointsHistory.Count.ShouldEqual(1);
            customer.RewardPointsHistory.First().Points.ShouldEqual(1);
        }

        [Test]
        public void Can_get_rewardPointsHistoryBalance()
        {
            var customer = new Customer();
            customer.AddRewardPointsHistoryEntry(1, "Points for registration");
            //customer.AddRewardPointsHistoryEntry(3, "Points for registration");

            customer.GetRewardPointsBalance().ShouldEqual(1);
        }
    }
}
