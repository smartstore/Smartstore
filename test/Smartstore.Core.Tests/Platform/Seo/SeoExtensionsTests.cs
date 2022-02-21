using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Seo;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Seo
{
    [TestFixture]
    public class SeoExtensionsTests
    {
        [Test]
        public void Should_return_lowercase()
        {
            SeoHelper.BuildSlug("tEsT", false, false).ShouldEqual("test");
        }

        [Test]
        public void Should_allow_all_latin_chars()
        {
            SeoHelper.BuildSlug("abcdefghijklmnopqrstuvwxyz1234567890", false, false).ShouldEqual("abcdefghijklmnopqrstuvwxyz1234567890");
        }

        [Test]
        public void Should_remove_illegal_chars()
        {
            SeoHelper.BuildSlug("test!@#$%^&*()+<>?", false, false).ShouldEqual("test");
        }

        [Test]
        public void Should_replace_space_with_dash()
        {
            SeoHelper.BuildSlug("test test", false, false).ShouldEqual("test-test");
            SeoHelper.BuildSlug("test     test", false, false).ShouldEqual("test-test");
        }

        [Test]
        public void Can_convert_non_western_chars()
        {
            //german letters with diacritics
            SeoHelper.BuildSlug("testäöü", true, false).ShouldEqual("testaou");
            SeoHelper.BuildSlug("testäöü", false, false).ShouldEqual("test");

            var charConversions = string.Join(Environment.NewLine, new string[] { "ä;ae", "ö;oe", "ü;ue" });

            SeoHelper.BuildSlug("testäöü", false, false, charConversions).ShouldEqual("testaeoeue");

            SeoHelper.ResetUserSeoCharacterTable();
        }

        [Test]
        public void Can_allow_unicode_chars()
        {
            //russian letters
            SeoHelper.BuildSlug("testтест", false, true).ShouldEqual("testтест");
            SeoHelper.BuildSlug("testтест", false, false).ShouldEqual("test");
        }
    }
}
