using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Tests.Rules.Filters
{
    [TestFixture]
    public class FilterTests : FilterTestsBase
    {
        [Test]
        public void OperatorIsNull()
        {
            var op = RuleOperator.IsNull;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(null, null), Is.EqualTo(true));
                Assert.That(op.Match("no", null), Is.EqualTo(false));
                Assert.That(op.Match((int?)null, null), Is.EqualTo(true));
            });

            var expectedResult = Customers.Where(x => x.Username == null).ToList();
            var result = ExecuteQuery(op, x => x.Username, null);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorIsNotNull()
        {
            var op = RuleOperator.IsNotNull;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(null, null), Is.EqualTo(false));
                Assert.That(op.Match("no", null), Is.EqualTo(true));
                Assert.That(op.Match((int?)null, null), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => x.Username != null).ToList();
            var result = ExecuteQuery(op, x => x.Username, null);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorIsEmpty()
        {
            var op = RuleOperator.IsEmpty;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match((string)null, null), Is.EqualTo(true));
                Assert.That(op.Match(string.Empty, null), Is.EqualTo(true));
                Assert.That(op.Match(" ab", null), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => string.IsNullOrWhiteSpace(x.Username)).ToList();
            var result = ExecuteQuery(op, x => x.Username, null);
            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorIsNotEmpty()
        {
            var op = RuleOperator.IsNotEmpty;

            Assert.That(op.Match((string)null, null), Is.EqualTo(false));
            Assert.That(op.Match(string.Empty, null), Is.EqualTo(false));
            Assert.That(op.Match(" ab", null), Is.EqualTo(true));

            var expectedResult = Customers.Where(x => !string.IsNullOrEmpty(x.Username)).ToList();
            var result = ExecuteQuery(op, x => x.Username, null);
            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorEqualTo()
        {
            var op = RuleOperator.IsEqualTo;

            var d1 = (DateTime?)DateTime.Now.Date;
            var d2 = DateTime.Now.Date;
            var d3 = (DateTime?)null;
            var e1 = DateTimeKind.Utc;
            var e2 = (DateTimeKind?)DateTimeKind.Utc;
            var e3 = (DateTimeKind?)null;

            Assert.That(op.Match(null, null), Is.True);
            Assert.That(op.Match(string.Empty, string.Empty), Is.True);
            Assert.That(op.Match("abc", "abc"), Is.True);
            Assert.That(op.Match(d1, d2), Is.True);
            Assert.That(op.Match(d2, d1), Is.True);
            Assert.That(op.Match(d3, d1), Is.False);
            Assert.That(op.Match(d2, d3), Is.False);
            Assert.That(op.Match(e1, e2), Is.True);
            Assert.That(op.Match(e2, e1), Is.True);
            Assert.That(op.Match(e3, e1), Is.False);
            Assert.That(op.Match(e2, e3), Is.False);

            var expectedResult = Customers.Where(x => x.IsTaxExempt == true).ToList();
            var result = ExecuteQuery(op, x => x.IsTaxExempt, true);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorNotEqualTo()
        {
            var op = RuleOperator.IsNotEqualTo;

            var d1 = (DateTime?)DateTime.Now.Date;
            var d2 = DateTime.Now.Date;
            var d3 = (DateTime?)null;
            var e1 = DateTimeKind.Utc;
            var e2 = (DateTimeKind?)DateTimeKind.Utc;
            var e3 = (DateTimeKind?)null;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(null, null), Is.EqualTo(false));
                Assert.That(op.Match(string.Empty, string.Empty), Is.EqualTo(false));
                Assert.That(op.Match("abc", "abc"), Is.EqualTo(false));
                Assert.That(op.Match(d1, d2), Is.EqualTo(false));
                Assert.That(op.Match(d2, d1), Is.EqualTo(false));
                Assert.That(op.Match(d3, d1), Is.EqualTo(true));
                Assert.That(op.Match(d2, d3), Is.EqualTo(true));
                Assert.That(op.Match(e1, e2), Is.EqualTo(false));
                Assert.That(op.Match(e2, e1), Is.EqualTo(false));
                Assert.That(op.Match(e3, e1), Is.EqualTo(true));
                Assert.That(op.Match(e2, e3), Is.EqualTo(true));
            });

            var expectedResult = Customers.Where(x => x.IsTaxExempt == false).ToList();
            var result = ExecuteQuery(op, x => x.IsTaxExempt, true);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorStartsWith()
        {
            var op = RuleOperator.StartsWith;

            Assert.That(op.Match("hello", "he"), Is.EqualTo(true));

            var expectedResult = Customers.Where(x => x.Username.EmptyNull().StartsWith('s')).ToList();
            var result = ExecuteQuery(op, x => x.Username, "s");

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorEndsWith()
        {
            var op = RuleOperator.EndsWith;

            Assert.That(op.Match("hello", "lo"), Is.EqualTo(true));

            var expectedResult = Customers.Where(x => x.Username.EmptyNull().EndsWith('y')).ToList();
            var result = ExecuteQuery(op, x => x.Username, "y");

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorContains()
        {
            var op = RuleOperator.Contains;

            Assert.That(op.Match("hello", "el"), Is.EqualTo(true));

            var expectedResult = Customers.Where(x => x.Username.EmptyNull().Contains("now")).ToList();
            var result = ExecuteQuery(op, x => x.Username, "now");

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorNotContains()
        {
            var op = RuleOperator.NotContains;

            Assert.That(op.Match("hello", "al"), Is.EqualTo(true));

            var expectedResult = Customers.Where(x => !x.Username.EmptyNull().Contains('a')).ToList();
            var result = ExecuteQuery(op, x => x.Username, "a");

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorGreatherThan()
        {
            var op = RuleOperator.GreaterThan;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(10, 5), Is.EqualTo(true));
                Assert.That(op.Match(5, 10), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => x.BirthDate.HasValue && x.BirthDate > DateTime.Now.AddYears(-30)).ToList();
            var result = ExecuteQuery(op, x => x.BirthDate, DateTime.Now.AddYears(-30));

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorGreatherThanOrEqualTo()
        {
            var op = RuleOperator.GreaterThanOrEqualTo;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(5, 5), Is.EqualTo(true));
                Assert.That(op.Match(4, 5), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => x.Id >= 2).ToList();
            var result = ExecuteQuery(op, x => x.Id, 2);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorLessThan()
        {
            var op = RuleOperator.LessThan;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(5, 10), Is.EqualTo(true));
                Assert.That(op.Match(10, 5), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => x.BirthDate.HasValue && x.BirthDate < DateTime.Now.AddYears(-10)).ToList();
            var result = ExecuteQuery(op, x => x.BirthDate, DateTime.Now.AddYears(-10));

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorLessThanOrEqualTo()
        {
            var op = RuleOperator.LessThanOrEqualTo;

            Assert.Multiple(() =>
            {
                Assert.That(op.Match(5, 5), Is.EqualTo(true));
                Assert.That(op.Match(5, 4), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => x.Id <= 4).ToList();
            var result = ExecuteQuery(op, x => x.Id, 4);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorIn()
        {
            var op = RuleOperator.In;

            var orderIds = new List<int> { 1, 2, 5, 8 };
            Assert.Multiple(() =>
            {
                Assert.That(op.Match(2, orderIds), Is.EqualTo(true));
                Assert.That(op.Match(3, orderIds), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => orderIds.Contains(x.Id)).ToList();
            var result = ExecuteQuery(op, x => x.Id, orderIds);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void OperatorNotIn()
        {
            var op = RuleOperator.NotIn;

            var orderIds = new List<int> { 1, 2, 3, 5 };
            Assert.Multiple(() =>
            {
                Assert.That(op.Match(4, orderIds), Is.EqualTo(true));
                Assert.That(op.Match(2, orderIds), Is.EqualTo(false));
            });

            var expectedResult = Customers.Where(x => !orderIds.Contains(x.Id)).ToList();
            var result = ExecuteQuery(op, x => x.Id, orderIds);

            AssertEquality(expectedResult, result);
        }


        [Test]
        public void SimpleMemberFiltersMatchAnd()
        {
            var taxExemptFilter = new FilterExpression
            {
                Descriptor = FilterDescriptors.TaxExempt,
                Operator = RuleOperator.IsEqualTo,
                Value = true
            };

            var countryFilter = new FilterExpression
            {
                Descriptor = FilterDescriptors.BillingCountry,
                Operator = RuleOperator.IsEqualTo,
                Value = 2
            };

            var expectedResult = Customers
                .Where(x => x.IsTaxExempt && x.BillingAddress != null && x.BillingAddress.CountryId == 2)
                .ToList();
            var result = ExecuteQuery(LogicalRuleOperator.And, taxExemptFilter, countryFilter);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void SimpleMemberFiltersMatchOr()
        {
            var taxExemptFilter = new FilterExpression
            {
                Descriptor = FilterDescriptors.TaxExempt,
                Operator = RuleOperator.IsEqualTo,
                Value = true
            };

            var countryFilter = new FilterExpression
            {
                Descriptor = FilterDescriptors.BillingCountry,
                Operator = RuleOperator.IsEqualTo,
                Value = 2
            };

            var shippingCountryFilter = new FilterExpression
            {
                Descriptor = FilterDescriptors.ShippingCountry,
                Operator = RuleOperator.IsEqualTo,
                Value = 2
            };

            var expectedResult = Customers
                .Where(x => x.IsTaxExempt || x.BillingAddress?.CountryId == 2 || x.ShippingAddress?.CountryId == 2)
                .ToList();
            var result = ExecuteQuery(LogicalRuleOperator.Or, taxExemptFilter, countryFilter, shippingCountryFilter);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void CompletedOrderCount()
        {
            var filter = new FilterExpression
            {
                Descriptor = FilterDescriptors.CompletedOrderCount,
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = 1
            };

            var expectedResult = Customers.Where(x => x.Orders.Count(y => y.OrderStatusId == 30) >= 1).ToList();
            var result = ExecuteQuery(LogicalRuleOperator.And, filter);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void HasAnyCustomerRole()
        {
            var filter = new FilterExpression
            {
                Descriptor = FilterDescriptors.IsInAnyRole,
                Operator = RuleOperator.In,
                Value = new List<int> { 2, 3 }
            };

            var roleIdsToCheck = filter.Value as List<int>;

            var expectedResult = Customers
                .Where(x => x.CustomerRoleMappings.Any(rm => rm.CustomerRole.Active && roleIdsToCheck.Contains(rm.CustomerRole.Id)))
                .ToList();

            var result = ExecuteQuery(LogicalRuleOperator.And, filter);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void HasAllCustomerRoles()
        {
            var filter = new FilterExpression
            {
                Descriptor = FilterDescriptors.HasAllRoles,
                Operator = RuleOperator.In,
                Value = new List<int> { 2, 3 }
            };

            var roleIdsToCheck = filter.Value as List<int>;

            var expectedResult = Customers
                .Where(x => x.CustomerRoleMappings.Where(rm => rm.CustomerRole.Active).All(rm => roleIdsToCheck.Contains(rm.CustomerRole.Id)))
                .ToList();

            var result = ExecuteQuery(LogicalRuleOperator.And, filter);

            AssertEquality(expectedResult, result);
        }

        [Test]
        public void HasPurchasedProduct()
        {
            var filter = new FilterExpression
            {
                Descriptor = FilterDescriptors.HasPurchasedProduct,
                Operator = RuleOperator.In,
                Value = new List<int> { 7, 8, 9, 10 }
            };

            var productIdsToCheck = filter.Value as List<int>;

            var expectedResult = Customers.Where(x => x.Orders.SelectMany(o => o.OrderItems).Any(p => productIdsToCheck.Contains(p.ProductId))).ToList();
            var result = ExecuteQuery(LogicalRuleOperator.And, filter);

            AssertEquality(expectedResult, result);
        }
    }
}
