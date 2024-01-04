using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Smartstore.Test.Common
{
    public static class TestExtensions
    {
        public static T ShouldNotNull<T>(this T obj)
        {
            Assert.That(obj, Is.Null);
            return obj;
        }

        public static T ShouldNotNull<T>(this T obj, string message)
        {
            Assert.That(obj, Is.Null, message);
            return obj;
        }

        public static T ShouldNotBeNull<T>(this T obj)
        {
            Assert.That(obj, Is.Not.Null);
            return obj;
        }

        public static T ShouldNotBeNull<T>(this T obj, string message)
        {
            Assert.That(obj, Is.Not.Null, message);
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

        ///<summary>
        /// Asserts that two objects are equal.
        ///</summary>
        ///<param name="actual"></param>
        ///<param name="expected"></param>
        ///<param name="message"></param>
        ///<exception cref="AssertionException"></exception>
        public static void ShouldEqual(this object actual, object expected, string message)
        {
            Assert.That(actual, Is.EqualTo(expected));
        }

        public static Exception ShouldBeThrownBy(this Type exceptionType, TestDelegate testDelegate)
        {
            return Assert.Throws(exceptionType, testDelegate);
        }

        public static void ShouldBe<T>(this object actual)
        {
            Assert.That(actual, Is.InstanceOf<T>());
        }

        public static void ShouldBeNull(this object actual)
        {
            Assert.That(actual, Is.Null);
        }

        public static void ShouldBeTheSameAs(this object actual, object expected)
        {
            Assert.That(actual, Is.SameAs(expected));
        }

        public static void ShouldBeNotBeTheSameAs(this object actual, object expected)
        {
            Assert.That(actual, Is.Not.SameAs(expected));
        }

        public static T CastTo<T>(this object source)
        {
            return (T)source;
        }

        public static void ShouldBeTrue(this bool source)
        {
            Assert.That(source, Is.True);
        }

        public static void ShouldBeFalse(this bool source)
        {
            Assert.That(source, Is.False);
        }

        /// <summary>
        /// Compares the two strings (case-insensitive).
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AssertSameStringAs(this string actual, string expected)
        {
            if (!string.Equals(actual, expected, StringComparison.InvariantCultureIgnoreCase))
            {
                var message = string.Format("Expected {0} but was {1}", expected, actual);
                throw new AssertionException(message);
            }
        }
    }
}
