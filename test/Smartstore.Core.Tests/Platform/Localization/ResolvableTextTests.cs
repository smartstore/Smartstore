#nullable enable

using NUnit.Framework;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Tests.Platform.Localization;

/// <summary>
/// Verifies literal and resource-backed resolvable text.
/// </summary>
[TestFixture]
public sealed class ResolvableTextTests
{
    /// <summary>
    /// Verifies that an implicitly converted string is treated as a literal value.
    /// </summary>
    [Test]
    public void Implicitly_Converts_String_To_Literal()
    {
        ResolvableText value = "Umami";

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.TypeOf<LiteralText>());
            Assert.That(value.Resolve(NullLocalizer.Instance).Value, Is.EqualTo("Umami"));
        });
    }

    /// <summary>
    /// Verifies that resource-backed strings are resolved with the supplied localizer.
    /// </summary>
    [Test]
    public void Resolves_Resource_String()
    {
        var value = ResolvableText.Resource("Dashboard.Groups.Sales");
        Localizer localizer = (key, _) => new LocalizedString($"Resolved:{key}");

        var result = value.Resolve(localizer);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.TypeOf<ResourceText>());
            Assert.That(result.Value, Is.EqualTo("Resolved:Dashboard.Groups.Sales"));
        });
    }

    /// <summary>
    /// Verifies that equality includes both source type and value.
    /// </summary>
    [Test]
    public void Compares_Source_Type_And_Value()
    {
        var literal1 = ResolvableText.Literal("Sales");
        var literal2 = ResolvableText.Literal("Sales");
        var resource = ResolvableText.Resource("Sales");

        Assert.Multiple(() =>
        {
            Assert.That(literal1, Is.EqualTo(literal2));
            Assert.That(literal1 == literal2, Is.True);
            Assert.That(literal1, Is.Not.EqualTo(resource));
        });
    }
}
