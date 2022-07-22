using System;
using NUnit.Framework;
using Smartstore.Core.Seo;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Seo
{
    [TestFixture]
    public class SlugUtilityTests
    {
        [Test]
        public void Can_slugify_with_defaults()
        {
            SlugUtility.Slugify("a ambição cerra o coração", new SlugifyOptions()).ShouldEqual("a-ambicao-cerra-o-coracao");
        }

        [Test]
        public void Should_return_lowercase()
        {
            SlugUtility.Slugify("tEsT", false, false).ShouldEqual("test");
        }

        [Test]
        public void Should_keep_casing()
        {
            SlugUtility.Slugify("tEsT", new SlugifyOptions { ForceLowerCase = false }).ShouldEqual("tEsT");
        }

        [Test]
        public void Should_allow_all_latin_chars()
        {
            SlugUtility.Slugify("abcdefghijklmnopqrstuvwxyz1234567890", false, false).ShouldEqual("abcdefghijklmnopqrstuvwxyz1234567890");
        }

        [Test]
        public void Should_remove_illegal_chars()
        {
            SlugUtility.Slugify("test!@#$%^&*()+<>?", false, false).ShouldEqual("test");
        }

        [Test]
        public void Should_replace_space_with_dash()
        {
            SlugUtility.Slugify("test test", false, false).ShouldEqual("test-test");
            SlugUtility.Slugify("test     test", false, false).ShouldEqual("test-test");
        }

        [Test]
        public void Can_convert_non_western_chars()
        {
            // German letters with diacritics
            SlugUtility.Slugify("testäöü", true, false).ShouldEqual("testaou");
            SlugUtility.Slugify("testäöü", false, false).ShouldEqual("test");

            var charConversions = string.Join(Environment.NewLine, new string[] { "ä;ae", "ö;oe", "ü;ue" });
            var seoSettings = new SeoSettings { SeoNameCharConversion = charConversions };

            SlugUtility.Slugify("testäöü", seoSettings).ShouldEqual("testaeoeue");
        }

        [Test]
        public void Can_allow_unicode_chars()
        {
            // Russian letters
            SlugUtility.Slugify("testтест", false, true).ShouldEqual("testтест");
            SlugUtility.Slugify("testтест", false, false).ShouldEqual("test");
        }

        [Test]
        public void Can_collapse_whitespace()
        {
            SlugUtility.Slugify("a  b   c     d", new SlugifyOptions { AllowSpace = true, CollapseWhiteSpace = true }).ShouldEqual("a b c d");
            SlugUtility.Slugify("a  b   c     d", new SlugifyOptions { AllowSpace = false }).ShouldEqual("a-b-c-d");
        }
    }
}
