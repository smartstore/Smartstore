#nullable enable

using NUnit.Framework;
using Smartstore.Core.AI;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.AI;

[TestFixture]
public class AIImageQualityTests
{
    [TestCase("xhigh")]
    [TestCase("max")]
    public void Can_convert_extended_quality_from_and_to_string(string value)
    {
        AIImageQuality quality = AIImageQuality.FromString(value)
            ?? throw new AssertionException($"Could not convert '{value}' to an image quality.");

        ((string)quality).ShouldEqual(value);
        Assert.That(AIImageQuality.All, Does.Contain(quality));
    }
}
