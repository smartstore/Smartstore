using System;
using System.Linq;
using NUnit.Framework;
using Smartstore.Collections;

namespace Smartstore.Tests
{
    [TestFixture]
    public class NaturalSortTests
    {
        [TestCase(new[] { "a", "b" }, new[] { "a", "b" })]
        [TestCase(new[] { "b", "a" }, new[] { "a", "b" })]
        [TestCase(new[] { "0", "1" }, new[] { "0", "1" })]
        [TestCase(new[] { "1", "0" }, new[] { "0", "1" })]
        [TestCase(new[] { "1", "02", "3" }, new[] { "1", "02", "3" })]
        [TestCase(new[] { "0001", "002", "001" }, new[] { "0001", "001", "002" })]
        [TestCase(new[] { "10.0401", "10.022" }, new[] { "10.022", "10.0401" })]
        [TestCase(new[] { "10.0401", "10.022", "10.042", "10.021999" }, new[] { "10.022", "10.042", "10.0401", "10.021999" })]
        [TestCase(new[] { "1.0.2", "1.0.1", "1.0.0", "1.0.9" }, new[] { "1.0.0", "1.0.1", "1.0.2", "1.0.9" })]
        [TestCase(new[] { "1.1.100", "1.1.1", "1.1.10", "1.1.54" }, new[] { "1.1.1", "1.1.10", "1.1.54", "1.1.100" })]
        [TestCase(new[] { "1.0.03", "1.0.003", "1.0.002", "1.0.0001" }, new[] { "1.0.0001", "1.0.002", "1.0.003", "1.0.03" })]
        [TestCase(new[] { "v1.100", "v1.1", "v1.10", "v1.54" }, new[] { "v1.1", "v1.10", "v1.54", "v1.100" })]
        [TestCase(new[] { "\u0044", "\u0055", "\u0054", "\u0043" }, new[] { "\u0043", "\u0044", "\u0054", "\u0055" })]
        [TestCase(
            new[] { "5D", "1A", "2D", "33A", "5E", "33K", "33D", "5S", "2C", "5C", "5F", "1D", "2M" }, 
            new[] { "1A", "1D", "2C", "2D", "2M", "5C", "5D", "5E", "5F", "5S", "33A", "33D", "33K" })]
        [TestCase(
            new[] { "1.1beta", "1.1.2alpha3", "1.0.2alpha3", "1.0.2alpha1", "1.0.1alpha4", "2.1.2", "2.1.1" },
            new[] { "1.0.1alpha4", "1.0.2alpha1", "1.0.2alpha3", "1.1.2alpha3", "1.1beta", "2.1.1", "2.1.2" })]
        [TestCase(
            new[] { "myrelease-1.1.3", "myrelease-1.2.3", "myrelease-1.1.4", "myrelease-1.1.1", "myrelease-1.0.5" },
            new[] { "myrelease-1.0.5", "myrelease-1.1.1", "myrelease-1.1.3", "myrelease-1.1.4", "myrelease-1.2.3" })]
        [TestCase(new[] { "bar.1-2", "bar.1" }, new[] { "bar.1", "bar.1-2" })]
        [TestCase(new[] { "2.2 sec", "1.9 sec", "1.53 sec" }, new[] { "1.9 sec", "1.53 sec", "2.2 sec" })]
        [TestCase(new[] { "2.2sec", "1.9sec", "1.53sec" }, new[] { "1.9sec", "1.53sec", "2.2sec" })]
        [TestCase(
            new[] { "192.168.0.100", "192.168.0.1", "192.168.1.1", "192.168.0.250", "192.168.1.123", "10.0.0.2", "10.0.0.1" },
            new[] { "10.0.0.1", "10.0.0.2", "192.168.0.1", "192.168.0.100", "192.168.0.250", "192.168.1.1", "192.168.1.123" })]
        [TestCase(new[] { "img12.png", "img10.png", "img2.png", "img1.png" }, new[] { "img1.png", "img2.png", "img10.png", "img12.png" })]
        [TestCase(
            new[] { "car.mov", "01alpha.sgi", "001alpha.sgi", "my.string_41299.tif", "organic2.0001.sgi" },
            new[] { "001alpha.sgi", "01alpha.sgi", "car.mov", "my.string_41299.tif", "organic2.0001.sgi" })]
        [TestCase(
            new[] { "1.txt", "1a.txt", "2.txt", "11.txt", "a.txt", "a1.txt", "a2.txt", "a11.txt", "aa.txt", "b.txt", "ba.txt", "bb.txt" },
            new[] { "1.txt", "1a.txt", "2.txt", "11.txt", "a.txt", "a1.txt", "a2.txt", "a11.txt", "aa.txt", "b.txt", "ba.txt", "bb.txt" })]
        [TestCase(
            new[] { "!1", "!a", "!abc", "_abc", "{abc}", "1.9 sec", "1.53 sec", "1_2", "1_3", "1_txt", "2.2 sec", "8_txt", "12", "13_3", "99!txt", "99.txt", "99_txt", "999_txt", "a_txt", "a1_txt" },
            new[] { "!1", "!a", "!abc", "_abc", "{abc}", "1.9 sec", "1.53 sec", "1_2", "1_3", "1_txt", "2.2 sec", "8_txt", "12", "13_3", "99!txt", "99.txt", "99_txt", "999_txt", "a_txt", "a1_txt" })]
        [TestCase(
            new[] {"z1.doc","z10.doc","z100.doc","z101.doc","z102.doc","z11.doc","z12.doc","z13.doc","z14.doc","z15.doc","z16.doc","z17.doc","z18.doc","z19.doc","z2.doc","z20.doc","z3.doc","z4.doc","z5.doc","z6.doc","z7.doc","z8.doc","z9.doc"},
            new[] {"z1.doc","z2.doc","z3.doc","z4.doc","z5.doc","z6.doc","z7.doc","z8.doc","z9.doc","z10.doc","z11.doc","z12.doc","z13.doc","z14.doc","z15.doc","z16.doc","z17.doc","z18.doc","z19.doc","z20.doc","z100.doc","z101.doc","z102.doc"})]
        public void Can_sort_natural(string[] actual, string[] expected)
        {
            var result = actual.OrderNaturalBy(x => x).ToArray();

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(new[] { "0001", "002", "001" }, new[] { "002", "001", "0001" })]
        [TestCase(new[] { "v1.100", "v1.1", "v1.10", "v1.54" }, new[] { "v1.100", "v1.54", "v1.10", "v1.1" })]
        public void Can_sort_natural_descending(string[] actual, string[] expected)
        {
            var result = actual.OrderNaturalByDescending(x => x).ToArray();

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("abc", "Abc", 0)]
        public void Can_compare_natural(string str1, string str2, int expected)
        {
            var result = new NaturalSorter(StringComparer.OrdinalIgnoreCase).Compare(str1, str2);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
