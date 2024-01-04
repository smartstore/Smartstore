using NUnit.Framework;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Common
{
    [TestFixture]
    public class SemanticVersionTests
    {
        [TestCase("1.2.0.0", 1, 2, 0, 0)]
        [TestCase("4.3.2.1", 4, 3, 2, 1)]
        public void Can_parse_4digit_version(string version, int major, int minor, int build, int revision)
        {
            SemanticVersion.TryParse(version, out var value).ShouldBeTrue();

            var other = new SemanticVersion(major, minor, build, revision);
            other.ShouldEqual(value);
        }

        [TestCase("1.2.0", 1, 2, 0)]
        [TestCase("1.4", 1, 4)]
        [TestCase("1.0.0-beta", 1, 0, 0, "beta")]
        [TestCase("2.0.0-rc.1 ", 2, 0, 0, "rc.1")]
        [TestCase("2.0.0-alpha+exp.sha.5114f85", 2, 0, 0, "alpha", "exp.sha.5114f85")]
        public void Can_parse_semantic_version(string version, int major, int minor = 0, int build = 0, string specialVersion = null, string metadata = null)
        {
            SemanticVersion.TryParse(version, out var value).ShouldBeTrue();

            var other = new SemanticVersion(major, minor, build, specialVersion, metadata);
            other.ShouldEqual(value);
        }

        [TestCase("2.0.0-alpha+exp.sha.5114f85", 2, 0, 0, "alpha", "exp.sha.5114f85")]
        public void Can_parse_strict_semantic_version(string version, int major, int minor = 0, int build = 0, string specialVersion = null, string metadata = null)
        {
            SemanticVersion.TryParseStrict(version, out var value).ShouldBeTrue();

            var other = new SemanticVersion(major, minor, build, specialVersion, metadata);
            other.ShouldEqual(value);
        }

        [TestCase("1.wrong.0")]
        public void Cannot_parse_faulty_version(string version)
        {
            SemanticVersion.TryParse(version, out var _).ShouldBeFalse();
        }

        [TestCase("2.0.0.0", "1.2.0.0")]
        [TestCase("2.0.0-rc.1", "1.0.0-beta")]
        public void First_version_is_greater_than_second(string version1, string version2)
        {
            SemanticVersion.TryParse(version1, out var value1).ShouldBeTrue();
            SemanticVersion.TryParse(version2, out var value2).ShouldBeTrue();

            Assert.That(value1, Is.GreaterThan(value2));
        }

        [TestCase("1.2.0.0", "1.2.0.0 ")]
        [TestCase("2.0.0-rc.1", "2.0.0-RC.1 ")]
        public void Two_versions_are_equal(string version1, string version2)
        {
            SemanticVersion.TryParse(version1, out var value1).ShouldBeTrue();
            SemanticVersion.TryParse(version2, out var value2).ShouldBeTrue();

            Assert.That(value2, Is.EqualTo(value1));
        }

        [TestCase("1.2.0.0", "2.0.0.0")]
        [TestCase("1.2.0.0", "1.2.0.1")]
        [TestCase("1.0.0-alpha", "1.0.0-beta")]
        public void Two_versions_are_not_equal(string version1, string version2)
        {
            SemanticVersion.TryParse(version1, out var value1).ShouldBeTrue();
            SemanticVersion.TryParse(version2, out var value2).ShouldBeTrue();

            Assert.That(value2, Is.Not.EqualTo(value1));
        }
    }
}
