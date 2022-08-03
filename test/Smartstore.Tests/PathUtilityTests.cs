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

        [Test]
        public void CanCombinePaths()
        {
            foreach (var combiner in _pathCombiners)
            {
                var combined = PathUtility.Combine(combiner[0], combiner[1]);
                Assert.AreEqual(combiner[2], combined);
            }
        }
    }
}
