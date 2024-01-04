using System;
using NUnit.Framework;
using Smartstore.Collections;

namespace Smartstore.Tests
{
    [TestFixture]
    public class TopologicSortTests
    {
        private class SortableItem : ITopologicSortable<string>
        {
            public string Key { get; set; }
            public string[] DependsOn { get; set; }
        }

        [Test]
        public void Can_sort_topological()
        {
            /*
					A
					|
				----------
				|        |
				B        C
						 |
					-----------
					|         |
					D         E
			 		|		  |
			 		H		  F
			 		|		  |
			 		I		  G
			*/

            var a = new SortableItem { Key = "A" };
            var b = new SortableItem { Key = "B", DependsOn = new string[] { "A" } };
            var c = new SortableItem { Key = "C", DependsOn = new string[] { "a" } };
            var d = new SortableItem { Key = "D", DependsOn = new string[] { "C" } };
            var e = new SortableItem { Key = "E", DependsOn = new string[] { "c" } };
            var f = new SortableItem { Key = "F", DependsOn = new string[] { "e" } };
            var g = new SortableItem { Key = "G", DependsOn = new string[] { "f" } };
            var h = new SortableItem { Key = "H", DependsOn = new string[] { "D" } };
            var i = new SortableItem { Key = "I", DependsOn = new string[] { "H" } };

            var items = new SortableItem[] { c, e, b, d, a, i, g, f, h };

            var sortedItems = items.SortTopological(StringComparer.OrdinalIgnoreCase);
            Assert.Multiple(() =>
            {
                //Console.WriteLine(String.Join(", ", sortedItems.Select(x => x.Key).ToArray()));

                Assert.That(sortedItems, Has.Length.EqualTo(items.Length));
                Assert.That(Array.IndexOf(sortedItems, a), Is.LessThan(Array.IndexOf(sortedItems, b)));
                Assert.That(Array.IndexOf(sortedItems, a), Is.LessThan(Array.IndexOf(sortedItems, c)));
                Assert.That(Array.IndexOf(sortedItems, a), Is.LessThan(Array.IndexOf(sortedItems, d)));
                Assert.That(Array.IndexOf(sortedItems, a), Is.LessThan(Array.IndexOf(sortedItems, e)));
                Assert.That(Array.IndexOf(sortedItems, c), Is.LessThan(Array.IndexOf(sortedItems, d)));
                Assert.That(Array.IndexOf(sortedItems, c), Is.LessThan(Array.IndexOf(sortedItems, e)));
                Assert.That(Array.IndexOf(sortedItems, d), Is.LessThan(Array.IndexOf(sortedItems, h)));
                Assert.That(Array.IndexOf(sortedItems, h), Is.LessThan(Array.IndexOf(sortedItems, i)));
                Assert.That(Array.IndexOf(sortedItems, e), Is.LessThan(Array.IndexOf(sortedItems, f)));
                Assert.That(Array.IndexOf(sortedItems, f), Is.LessThan(Array.IndexOf(sortedItems, g)));
            });
        }

        [Test]
        public void Can_detect_cycles()
        {
            /*
					A
					|
				----------
				|        |
				B        C
						 |
					-----------
					|         
					D         E<-
			 		|		  |  |
			 		G		  F--
			*/

            var a = new SortableItem { Key = "A" };
            var b = new SortableItem { Key = "B", DependsOn = new string[] { "A" } };
            var c = new SortableItem { Key = "C", DependsOn = new string[] { "a" } };
            var d = new SortableItem { Key = "D", DependsOn = new string[] { "C" } };
            var e = new SortableItem { Key = "E", DependsOn = new string[] { "f" } };
            var f = new SortableItem { Key = "F", DependsOn = new string[] { "e" } };
            var g = new SortableItem { Key = "G", DependsOn = new string[] { "D" } };

            var items = new SortableItem[] { c, e, b, d, a, g, f };

            Assert.Throws<CyclicDependencyException>(() => items.SortTopological(StringComparer.OrdinalIgnoreCase));
        }
    }
}