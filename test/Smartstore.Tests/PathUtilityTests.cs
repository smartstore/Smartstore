using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.IO;

namespace Smartstore.Tests;

[TestFixture]
public class PathUtilityTests
{
    private static readonly List<string[]> _pathCombiners =
    [
        ["/some/path/left", "../right/path/", "/some/path/right/path/"],
        ["some/path/left", "../../right/path/", "some/right/path/"],
        ["/some/path/left", "right/path/", "/some/path/left/right/path/"],
        ["/some/path/left", "/right/path/", "/right/path/"],
        ["/some/path/left/", "../../right/../path", "/some/path"],
        ["/some/path/left/", "right/../path", "/some/path/left/path"],
    ];

    private static readonly List<string[]> _pathJoiners =
    [
        ["/some/path/left", "/right/path/", "/some/path/left/right/path/"],
        ["some/path/left/", "right\\path/", "some/path/left/right/path/"],
        ["\\some/path/left/", "/right/path/", "/some/path/left/right/path/"]
    ];

    [Test]
    public void Can_combine_paths()
    {
        foreach (var combiner in _pathCombiners)
        {
            var path1 = combiner[0];
            var path2 = combiner[1];

            var combined = PathUtility.Combine(path1, path2);
            Assert.That(combined, Is.EqualTo(combiner[2]));
        }
    }

    [Test]
    public void Can_join_paths()
    {
        foreach (var combiner in _pathJoiners)
        {
            var path1 = combiner[0].AsSpan();
            var path2 = combiner[1].AsSpan();

            var combined = PathUtility.Join(path1, path2);
            Assert.That(combined, Is.EqualTo(combiner[2]));
        }
    }

    [TestCase("file.csv", true)]
    [TestCase("file name.csv", true)]
    [TestCase("../file.csv", false)]
    [TestCase("..\\file.csv", false)]
    [TestCase("folder/file.csv", false)]
    [TestCase("folder\\file.csv", false)]
    [TestCase(".", false)]
    [TestCase("..", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Can_validate_file_name(string value, bool expected)
    {
        Assert.That(PathUtility.IsFileName(value), Is.EqualTo(expected));
    }
}