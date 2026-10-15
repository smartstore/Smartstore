using System.IO;
using NUnit.Framework;
using Smartstore.IconKit;

namespace Smartstore.Core.Tests.Content.Media.Icons;

[TestFixture]
public class IconPresentationTests
{
    [TestCase("")]
    [TestCase("?")]
    [TestCase("?&&")]
    public void Empty_Presentation_Has_No_Question_Mark(string value)
    {
        Assert.That(IconPresentation.TryParse(value, out var presentation), Is.True);
        Assert.That(presentation, Is.EqualTo(default(IconPresentation)));
        Assert.That(presentation.ToString(), Is.Empty);
    }

    [TestCase("?size=lg&color=#1459ad&rotate=90")]
    [TestCase("size=lg&color=#1459ad&rotate=90")]
    [TestCase("?flip=none&rotate=0&fixed-width=false")]
    [TestCase("?color=color-mix(in%20oklch,%20red%2050%25,%20blue)")]
    public void Presentation_RoundTrips_Independently_Of_An_Address(string value)
    {
        var presentation = IconPresentation.Parse(value);
        var serialized = presentation.ToString();
        Assert.That(serialized, Does.StartWith("?").And.Not.StartWith("??"));
        Assert.That(IconPresentation.TryParse(serialized, out var restored), Is.True);
        Assert.That(restored, Is.EqualTo(presentation));
        var expression = new IconExpression("cart", presentation);
        Assert.That(expression.ToString(), Is.EqualTo("cart" + serialized));
        Assert.That(IconExpression.Parse(expression.ToString()), Is.EqualTo(expression));
    }

    [Test]
    public void Last_Valid_Value_Wins_And_Invalid_Known_Values_Are_Ignored()
    {
        var presentation = IconPresentation.Parse("?rotate=90&rotate=invalid&size=&rotate=0");
        Assert.That(presentation.Rotate, Is.Zero);
        Assert.That(presentation.Size, Is.Null);
    }

    [TestCase("fw")]
    [TestCase("inverse")]
    [TestCase("anim-rev")]
    [TestCase("flip-h")]
    [TestCase("flip-v")]
    [TestCase("mirror-rtl")]
    public void Boolean_Flags_Accept_Bare_Text_And_Numeric_Values(string key)
    {
        var enabled = IconPresentation.Parse("?" + key);
        Assert.That(enabled, Is.Not.EqualTo(default(IconPresentation)));
        Assert.That(IconPresentation.Parse("?" + key + "=true"), Is.EqualTo(enabled));
        Assert.That(IconPresentation.Parse("?" + key + "=1"), Is.EqualTo(enabled));
        Assert.That(enabled.ToString(), Is.EqualTo("?" + key));
        var disabled = IconPresentation.Parse("?" + key + "=0");
        Assert.That(disabled, Is.Not.EqualTo(default(IconPresentation)));
        Assert.That(disabled, Is.EqualTo(IconPresentation.Parse("?" + key + "=false")));
        Assert.That(disabled.ToString(), Is.EqualTo("?" + key + "=false"));
        Assert.That(IconPresentation.Parse("?" + key + "=0&" + key), Is.EqualTo(enabled));
        Assert.That(IconPresentation.Parse("?" + key + "&" + key + "=0"), Is.EqualTo(disabled));
        Assert.That(IconPresentation.Parse("?" + key + "="), Is.EqualTo(default(IconPresentation)));
    }

    [Test]
    public void Short_Tokens_Are_Canonical_And_Long_Tokens_Remain_Readable()
    {
        var presentation = IconPresentation.Parse("?fixed-width=true&animation=spin&animation-duration=800ms&animation-reverse=true");
        Assert.That(presentation.ToString(), Is.EqualTo("?fw&anim=spin&anim-dur=800ms&anim-rev"));
        Assert.That(IconPresentation.Parse(presentation.ToString()), Is.EqualTo(presentation));
    }

    [TestCase("?unknown=1")]
    [TestCase("?color=red%3Bdisplay:none")]
    public void Invalid_Input_Does_Not_Return_Partial_Results(string value)
    {
        Assert.That(IconPresentation.TryParse(value, out var presentation), Is.False);
        Assert.That(presentation, Is.EqualTo(default(IconPresentation)));
        Assert.Throws<InvalidDataException>(() => IconPresentation.Parse(value));
    }
}
