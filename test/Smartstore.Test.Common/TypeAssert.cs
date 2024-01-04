using System;
using NUnit.Framework;

namespace Smartstore.Test.Common
{
    public static class TypeAssert
    {
        public static void AreEqual(object expected, object instance)
        {
            if (expected == null)
                Assert.That(instance, Is.Null);
            else
                Assert.That(instance, Is.Not.Null, "Instance was null");
            Assert.That(instance.GetType(), Is.EqualTo(expected.GetType()), "Expected: " + expected.GetType() + ", was: " + instance.GetType() + " was not of type " + instance.GetType());
        }

        public static void AreEqual(Type expected, object instance)
        {
            if (expected == null)
                Assert.That(instance, Is.Null);
            else
                Assert.That(instance, Is.Not.Null, "Instance was null");
            Assert.That(instance.GetType(), Is.EqualTo(expected), "Expected: " + expected.GetType() + ", was: " + instance.GetType() + " was not of type " + instance.GetType());
        }

        public static void Equals<T>(object instance)
        {
            AreEqual(typeof(T), instance);
        }

        public static void IsTypeOf<T>(object instance)
        {
            Assert.That(instance is T, Is.True);
        }
    }
}