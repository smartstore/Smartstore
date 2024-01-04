using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.ComponentModel;

namespace Smartstore.Tests
{
    [TestFixture]
    public class MiniMapperTest
    {
        [Test]
        public void CanMap1()
        {
            var from = new MapClass1
            {
                Prop1 = "Prop1",
                Prop2 = "Prop2",
                Prop3 = "Prop3",
                Prop4 = 99,
                Prop5 = new ConsoleKey[] { ConsoleKey.Backspace, ConsoleKey.Tab, ConsoleKey.Clear }
            };
            from.Address.FirstName = "John";
            from.Address.LastName = "Doe";
            from.Address.Age = 24;

            var to = MiniMapper.Map<MapClass1, MapClass2>(from);

            Assert.That(to.Prop1, Is.EqualTo(from.Prop1));
            Assert.That(to.Prop2, Is.EqualTo(from.Prop2));
            Assert.That(to.Prop3, Is.EqualTo(from.Prop3));
            Assert.That(to.Prop4, Is.EqualTo(from.Prop4));
            Assert.That(to.Prop5.Count, Is.EqualTo(from.Prop5.Length));
            Assert.That(to.Prop5[0], Is.EqualTo((int)from.Prop5[0]));
            Assert.That(to.Prop5[1], Is.EqualTo((int)from.Prop5[1]));
            Assert.That(to.Prop5[2], Is.EqualTo((int)from.Prop5[2]));

            var dict = to.Address;
            Assert.That(dict, Has.Count.EqualTo(3));
            Assert.That(from.Address.FirstName, Is.EqualTo(dict["FirstName"]));
            Assert.That(from.Address.LastName, Is.EqualTo(dict["LastName"]));
            Assert.That(from.Address.Age, Is.EqualTo(dict["Age"]));
        }

        [Test]
        public void CanMap2()
        {
            var from = new MapClass2
            {
                Prop1 = "Prop1",
                Prop2 = "Prop2",
                Prop3 = "Prop3"
            };
            from.Address["FirstName"] = "John";
            from.Address["LastName"] = "Doe";
            from.Address["Age"] = 24;

            var to = MiniMapper.Map<MapClass2, MapClass1>(from);

            Assert.That(to.Prop1, Is.EqualTo(from.Prop1));
            Assert.That(to.Prop2, Is.EqualTo(from.Prop2));
            Assert.That(to.Prop3, Is.EqualTo(from.Prop3));

            var dict = from.Address;
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(to.Address.FirstName, Is.EqualTo(dict["FirstName"]));
            Assert.That(to.Address.LastName, Is.EqualTo(dict["LastName"]));
            Assert.That(to.Address.Age, Is.EqualTo(dict["Age"]));
        }

        [Test]
        public void CanMapAnonymousType()
        {
            var from = new
            {
                Prop1 = "Prop1",
                Prop2 = "Prop2",
                Prop3 = "Prop3",
                Prop4 = 99f,
                Address = new
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Age = 24
                }
            };

            var to = new MapClass2();
            MiniMapper.Map(from, to);

            Assert.Multiple(() =>
            {
                Assert.That(to.Prop1, Is.EqualTo(from.Prop1));
                Assert.That(to.Prop2, Is.EqualTo(from.Prop2));
                Assert.That(to.Prop3, Is.EqualTo(from.Prop3));
                Assert.That(to.Prop4, Is.EqualTo(from.Prop4));
            });

            var dict = to.Address;
            Assert.Multiple(() =>
            {
                Assert.That(dict, Has.Count.EqualTo(3));
                Assert.That(from.Address.FirstName, Is.EqualTo(dict["FirstName"]));
                Assert.That(from.Address.LastName, Is.EqualTo(dict["LastName"]));
                Assert.That(from.Address.Age, Is.EqualTo(dict["Age"]));
            });
        }
    }

    public class MapClass1
    {
        public string Prop1 { get; set; }
        public string Prop2 { get; set; }
        public string Prop3 { get; set; }
        public float? Prop4 { get; set; }
        public ConsoleKey[] Prop5 { get; set; }
        public MapNestedClass Address { get; set; } = new MapNestedClass();
    }

    public class MapClass2
    {
        public string Prop1 { get; set; }
        public string Prop2 { get; set; }
        public string Prop3 { get; set; }
        public int Prop4 { get; set; }
        public List<int> Prop5 { get; set; }
        public IDictionary<string, object> Address { get; set; } = new Dictionary<string, object>();
    }

    public class MapNestedClass
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }
}
