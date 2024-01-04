using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Tests.Common.Controllers
{
    [TestFixture]
    public class FormValueRequiredTests
    {
        private IFormCollection _form;

        [OneTimeSetUp]
        public void SetUp()
        {
            _form = new FormCollection(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            {
                ["Submit.First"] = "val",
                ["Submit.Second"] = "val",
                ["Submit.Third"] = "val",
                ["Cancel.First"] = "val",
                ["Cancel.Second"] = "val",
                ["Cancel.Third"] = "val",
                ["val1"] = "val",
                ["VAL2"] = "val",
                ["vAl3"] = "val"
            });
        }

        [Test]
        public void Can_Match_Required_Equals_Any()
        {
            var values = new string[] { "NOTAVAIL", "VAL1" };
            var attr = new FormValueRequiredAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAny, values);
            Assert.That(attr.IsValidForRequest(_form), Is.True);

            values = ["NOTAVAIL", "NOTAVAIL2"];
            attr = new FormValueRequiredAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAny, values);
            Assert.That(attr.IsValidForRequest(_form), Is.False);
        }

        [Test]
        public void Can_Match_Required_Equals_All()
        {
            var values = new string[] { "val2", "VAL1", "Cancel.First" };
            var attr = new FormValueRequiredAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAll, values);
            Assert.That(attr.IsValidForRequest(_form), Is.True);

            values = new string[] { "val2", "VAL1", "NOTAVAIL", "Cancel.First" };
            attr = new FormValueRequiredAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAll, values);
            Assert.That(attr.IsValidForRequest(_form), Is.False);
        }

        [Test]
        public void Can_Match_Required_StartsWith_Any()
        {
            var values = new string[] { "NOTAVAIL", "VAL1", "SuBmit." };
            var attr = new FormValueRequiredAttribute(FormValueRequirementOperator.StartsWith, FormValueRequirementMatch.MatchAny, values);
            Assert.That(attr.IsValidForRequest(_form), Is.True);

            values = new string[] { "NOTAVAIL", "NOTAVAIL2" };
            attr = new FormValueRequiredAttribute(FormValueRequirementOperator.StartsWith, FormValueRequirementMatch.MatchAny, values);
            Assert.That(attr.IsValidForRequest(_form), Is.False);
        }

        [Test]
        public void Can_Match_Required_StartsWith_All()
        {
            var values = new string[] { "SUBMIT", "Cancel.", "VAL" };
            var attr = new FormValueRequiredAttribute(FormValueRequirementOperator.StartsWith, FormValueRequirementMatch.MatchAll, values);
            Assert.That(attr.IsValidForRequest(_form), Is.True);

            values = new string[] { "SUBMIT", "Cancel.", "VAL", "notavail" };
            attr = new FormValueRequiredAttribute(FormValueRequirementOperator.StartsWith, FormValueRequirementMatch.MatchAll, values);
            Assert.That(attr.IsValidForRequest(_form), Is.False);
        }

        [Test]
        public void Can_Match_Absent()
        {
            var values = new string[] { "NOTAVAIL", "VAL1" };
            var attr = new FormValueAbsentAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAny, values);
            Assert.That(attr.IsValidForRequest(_form), Is.True);

            attr = new FormValueAbsentAttribute(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAll, values);
            Assert.That(attr.IsValidForRequest(_form), Is.False);
        }
    }
}
