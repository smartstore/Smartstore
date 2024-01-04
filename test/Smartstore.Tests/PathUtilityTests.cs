using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.IO;

namespace Smartstore.Tests
{
    [TestFixture]
    public class PathUtilityTests
    {
        private static readonly List<string[]> _pathCombiners = new()
        {
            new[] { "/some/path/left", "../right/path/", "/some/path/right/path/" },
            new[] { "some/path/left", "../../right/path/", "some/right/path/" },
            new[] { "/some/path/left", "right/path/", "/some/path/left/right/path/" },
            new[] { "/some/path/left", "/right/path/", "/right/path/" },
            new[] { "/some/path/left/", "../../right/../path", "/some/path" },
            new[] { "/some/path/left/", "right/../path", "/some/path/left/path" },
        };

        private static readonly List<string[]> _pathJoiners = new()
        {
            new[] { "/some/path/left", "/right/path/", "/some/path/left/right/path/" },
            new[] { "some/path/left/", "right\\path/", "some/path/left/right/path/" },
            new[] { "\\some/path/left/", "/right/path/", "/some/path/left/right/path/" }
        };

        [Test]
        public void CanCombinePaths()
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
        public void CanJoinPaths()
        {
            foreach (var combiner in _pathJoiners)
            {
                var path1 = combiner[0].AsSpan();
                var path2 = combiner[1].AsSpan();

                var combined = PathUtility.Join(path1, path2);
                Assert.That(combined, Is.EqualTo(combiner[2]));
            }
        }
    }
}
