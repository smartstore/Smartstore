using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Smartstore.Test.Common
{
    public static class TestExtensions
    {
        public static T ShouldNotBeNull<T>(this T obj)
        {
            Assert.That(obj, Is.Not.Null);
            return obj;
        }

        public static T ShouldEqual<T>(this T actual, object expected)
        {
            Assert.That(actual, Is.EqualTo(expected));
            return actual;
        }

        public static IEnumerable<T> ShouldSequenceEqual<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Assert.That(actual.SequenceEqual(expected), Is.True);
            return actual;
        }

        public static void ShouldBe<T>(this object actual)
        {
            Assert.That(actual, Is.InstanceOf<T>());
        }

        public static void ShouldBeNull(this object actual)
        {
            Assert.That(actual, Is.Null);
        }

        public static void ShouldBeTrue(this bool source)
        {
            Assert.That(source, Is.True);
        }

        public static void ShouldBeFalse(this bool source)
        {
            Assert.That(source, Is.False);
        }
    }
}
