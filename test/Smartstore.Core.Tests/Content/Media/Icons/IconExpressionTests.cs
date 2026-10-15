using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Smartstore.IconKit;

namespace Smartstore.Core.Tests.Content.Media.Icons;

[TestFixture]
public class IconExpressionTests
{
    [TestCase("cart")]
    [TestCase("hi:cart!/sr?flip=none&rotate=0&stroke-scale=1&size=lg&color=#1459ad&animation=spin")]
    [TestCase("cart?flip-h=false&fixed-width=false&inverse=false&mirror-rtl=false&animation-reverse=false")]
    [TestCase("cart?font-scale=1.25&scale=0.8&shift-x=-2&shift-y=3.5&animation-duration=800ms")]
    [TestCase("cart?color=color-mix(in%20oklch,%20red%2050%25,%20blue)")]
    public void Persistent_Strings_RoundTrip_Without_Resolving_Concepts(string value)
    {
        var expression = IconExpression.Parse(value);
        Assert.That(IconExpression.TryParse(expression.ToString(), out var parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(expression));
        Assert.That(parsed.Address, Is.EqualTo(expression.Address));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("cart?color=red%3Bdisplay:none")]
    [TestCase("cart?unknown=1")]
    public void Invalid_Input_Returns_False_And_Empty_Result(string value)
    {
        Assert.That(IconExpression.TryParse(value, out var result), Is.False);
        Assert.That(result, Is.EqualTo(default(IconExpression)));
    }

    [TestCase("cart?rotate=NaN&stroke-scale=0&size=bogus&&color=", "cart")]
    [TestCase("cart?rotate=1&rotate=2&rotate=invalid&rotate=", "cart?rotate=2")]
    [TestCase("cart?flip=x&flip-h=false", "cart?flip=none")]
    [TestCase("cart?flip-h=false&flip=xy", "cart?flip=xy")]
    [TestCase("cart?color=red&color=blue", "cart?color=blue")]
    [TestCase("cart?size=lg&size=&animation=spin&animation=unknown", "cart?size=lg&anim=spin")]
    [TestCase("cart?&rotate&", "cart")]
    public void Known_Modifiers_Are_Tolerant_And_Last_Valid_Value_Wins(string input, string expected)
    {
        Assert.That(IconExpression.TryParse(input, out var actual), Is.True);
        Assert.That(actual, Is.EqualTo(IconExpression.Parse(expected)));
        Assert.That(IconExpression.Parse(input), Is.EqualTo(actual));
    }

    [Test]
    public void Explicit_Resets_And_Omitted_Axes_Remain_Distinct()
    {
        var expression = IconExpression.Parse("cart?flip-h=false&rotate=0");
        var copy = expression with { Presentation = expression.Presentation with { Color = "#fff" } };
        Assert.That(expression.Presentation.Color, Is.Null);
        Assert.That(copy.Presentation.FlipHorizontal, Is.False);
        Assert.That(copy.Presentation.FlipVertical, Is.Null);
        Assert.That(copy.Presentation.Rotate, Is.Zero);
        Assert.That(IconExpression.Parse(copy.ToString()), Is.EqualTo(copy));
    }

    [Test]
    public void Presentation_Overrides_Preserve_Explicit_Resets()
    {
        var defaults = new IconPresentation { Rotate = 90, FlipHorizontal = true, Size = "lg" };
        var overrides = new IconPresentation { Rotate = 0, FlipHorizontal = false };
        var merged = overrides.WithDefaults(defaults);
        Assert.That(merged.Rotate, Is.Zero);
        Assert.That(merged.FlipHorizontal, Is.False);
        Assert.That(merged.Size, Is.EqualTo("lg"));
        Assert.That(defaults.Rotate, Is.EqualTo(90));
    }

    [Test]
    public void Numeric_Formatting_Is_Culture_Independent()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var expression = new IconExpression("cart", new IconPresentation { Rotate = 12.5, FontScale = 1.25f });
            Assert.That(expression.ToString(), Does.Contain("rotate=12.5").And.Contain("font-scale=1.25"));
            Assert.That(IconExpression.Parse(expression.ToString()), Is.EqualTo(expression));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Formatting_Rejects_Invalid_Programmatic_Values()
    {
        var expression = new IconExpression("cart", new IconPresentation { Rotate = double.NaN });
        Assert.Throws<InvalidDataException>(() => expression.ToString());
        Assert.Throws<ArgumentException>(() => new IconExpression(default(IconAddress), new IconPresentation { Size = "lg" }));
    }
}
