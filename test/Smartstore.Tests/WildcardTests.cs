using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Smartstore.Utilities;

namespace Smartstore.Tests;

[TestFixture]
public class WildcardTests
{
    [Test]
    public void Can_match_number_range()
    {
        var w1 = new Wildcard("999-2450", true);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("999"), Is.True);
            Assert.That(w1.IsMatch("1500"), Is.True);
            Assert.That(w1.IsMatch("2450"), Is.True);
            Assert.That(w1.IsMatch("500"), Is.False);
            Assert.That(w1.IsMatch("2800"), Is.False);
        });

        w1 = new Wildcard("50000-59999", true);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("59192"), Is.True);
            Assert.That(w1.IsMatch("55000"), Is.True);
            Assert.That(w1.IsMatch("500"), Is.False);
            Assert.That(w1.IsMatch("80000"), Is.False);
        });

        w1 = new Wildcard("3266-3267", true);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("3266"), Is.True);
            Assert.That(w1.IsMatch("3267"), Is.True);
            Assert.That(w1.IsMatch("500"), Is.False);
            Assert.That(w1.IsMatch("4000"), Is.False);
        });

        w1 = new Wildcard("0001000-0005000", true);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("0001000"), Is.True);
            Assert.That(w1.IsMatch("0002008"), Is.True);
            Assert.That(w1.IsMatch("1000"), Is.False);
            Assert.That(w1.IsMatch("5000"), Is.False);
        });

        w1 = new Wildcard("7059-7099", true);
        Console.WriteLine(w1.Pattern);
        Assert.That(w1.IsMatch("7059"), Is.True);

        w1 = new Wildcard("0-9999", true);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("0"), Is.True);
            Assert.That(w1.IsMatch("3267"), Is.True);
            Assert.That(w1.IsMatch("9999"), Is.True);
            Assert.That(w1.IsMatch("99999"), Is.False);
            Assert.That(w1.IsMatch("2345678"), Is.False);
        });
    }

    [Test]
    public void Can_match_wildcard()
    {
        var w1 = new Wildcard("H*o ?orld", RegexOptions.IgnoreCase);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("Hello World"), Is.True);
            Assert.That(w1.IsMatch("hello WORLD"), Is.True);
            Assert.That(w1.IsMatch("world"), Is.False);
            Assert.That(w1.IsMatch("Hell word"), Is.False);
        });
    }

    [Test]
    public void Can_match_glob()
    {
        var w1 = new Wildcard("h[ae]llo", RegexOptions.IgnoreCase);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("hello"), Is.True);
            Assert.That(w1.IsMatch("hallo"), Is.True);
            Assert.That(!w1.IsMatch("hillo"), Is.True);
        });

        w1 = new Wildcard("h[^e]llo", RegexOptions.IgnoreCase);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("hallo"), Is.True);
            Assert.That(w1.IsMatch("hbllo"), Is.True);
            Assert.That(!w1.IsMatch("hello"), Is.True);
        });

        w1 = new Wildcard("h[a-d]llo", RegexOptions.IgnoreCase);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("hallo"), Is.True);
            Assert.That(w1.IsMatch("hcllo"), Is.True);
            Assert.That(w1.IsMatch("hdllo"), Is.True);
            Assert.That(!w1.IsMatch("hgllo"), Is.True);
        });

        w1 = new Wildcard("entry-[^0]*", RegexOptions.IgnoreCase);
        Assert.Multiple(() =>
        {
            //Console.WriteLine(w1.Pattern);
            Assert.That(w1.IsMatch("entry-22"), Is.True);
            Assert.That(w1.IsMatch("entry-9"), Is.True);
            Assert.That(!w1.IsMatch("entry-0"), Is.True);
        });
    }

    [Test]
    public void Range_patterns_have_no_capture_groups()
    {
        var w1 = new Wildcard("11-200.33-40.*.???", true);

        Assert.Multiple(() =>
        {
            Assert.That(w1.GetGroupNumbers(), Has.Length.EqualTo(1));
            Assert.That(w1.IsMatch("55.35.1.123"), Is.True);
            Assert.That(w1.IsMatch("10.35.1.123"), Is.False);
            Assert.That(w1.IsMatch("55.41.1.123"), Is.False);
        });
    }

    [Test]
    public void DefaultOptions_make_matching_culture_invariant()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            // Turkish 'I' lowercases to dotless 'ı', so a culture-sensitive
            // match does not consider "I*" and "index" equal.
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var withDefaults = new Wildcard("I*", RegexOptions.IgnoreCase | Wildcard.DefaultOptions);
            var cultureAware = new Wildcard("I*", RegexOptions.IgnoreCase);

            Assert.Multiple(() =>
            {
                Assert.That(withDefaults.IsMatch("index"), Is.True);

                // Opting out of the defaults is possible and stays culture-sensitive.
                Assert.That(cultureAware.IsMatch("index"), Is.False);
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Test]
    public void Default_ctor_applies_DefaultOptions()
    {
        var culture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // No options passed -> DefaultOptions apply.
            var w1 = new Wildcard("i*");
            Assert.That(w1.Options.HasFlag(RegexOptions.CultureInvariant), Is.True);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Test]
    public void Can_apply_match_timeout()
    {
        var w1 = new Wildcard("10.20.30.*", RegexOptions.None, TimeSpan.FromSeconds(5), true);

        Assert.Multiple(() =>
        {
            Assert.That(w1.MatchTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(w1.IsMatch("10.20.30.44"), Is.True);
            Assert.That(w1.IsMatch("10.20.31.44"), Is.False);
        });
    }
}