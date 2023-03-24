using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;
using Smartstore.Core.Common;
using Smartstore.Test.Common;

namespace Smartstore.Tests
{
    [TestFixture]
    public class DictionaryConversionTests
    {
        public class TestClass
        {
            public string PropString { get; set; }
            public bool PropBool { get; set; }
            public DateTime? PropDate { get; set; }
            public int PropInt { get; set; }
            public Money? PropMoney { get; set; }
            public Dictionary<string, object> PropDictionary { get; set; } = new();
            public TestClass PropNested { get; set; }
            public TestClass PropNested2 { get; set; }
            public IList<TestClass> PropList { get; set; }
            public TestClass[] PropArray { get; set; }
        }
        
        private IDictionary<string, object> _dict;
        
        [SetUp]
        public virtual void SetUp()
        {
            CreateTestDictionary();
        }

        private void CreateTestDictionary()
        {
            _dict = new Dictionary<string, object>
            {
                ["PropString"] = "A string",
                ["PropBool"] = true,
                ["PropDate"] = DateTime.Now,
                ["PropInt"] = 1234,
                ["PropMoney"] = null,
                ["PropDictionary"] = new Dictionary<string, object>
                {
                    ["PropString"] = "A string",
                    ["PropBool"] = true,
                    ["PropDate"] = DateTime.Now,
                    ["PropInt"] = 1234,
                    ["PropMoney"] = new Money(),
                },
                ["PropNested"] = new Dictionary<string, object>
                {
                    ["PropString"] = "A string",
                    ["PropBool"] = true,
                    ["PropDate"] = DateTime.Now,
                    ["PropInt"] = 1234,
                    ["PropMoney"] = new Money(),
                },
                ["PropList[0].PropString"] = "A string",
                ["PropList[1].PropBool"] = true,
                ["PropList[2].PropDate"] = DateTime.Now,
                ["PropList[3].PropInt"] = 1234,
                ["PropList[4].PropMoney"] = null,
                ["PropArray[0].PropString"] = "A string",
                ["PropArray[1].PropBool"] = true,
                ["PropArray[2].PropDate"] = DateTime.Now,
                ["PropArray[3].PropInt"] = 1234,
            };
        }

        [Test]
        public void CanConvertDictionary()
        {
            var converter = TypeConverterFactory.GetConverter<Dictionary<string, object>>();

            converter.ShouldBe<DictionaryTypeConverter<IDictionary<string, object>>>();
            converter.CanConvertTo(typeof(TestClass)).ShouldBeTrue();
            converter.CanConvertTo(typeof(Money)).ShouldBeFalse();
            converter.CanConvertTo(typeof(List<string>)).ShouldBeFalse();

            var result = (TestClass)converter.ConvertTo(_dict, typeof(TestClass));

            Assert.AreEqual(result.PropString, _dict["PropString"]);
            Assert.AreEqual(result.PropBool, _dict["PropBool"]);
            Assert.AreEqual(result.PropDate, _dict["PropDate"]);
            Assert.AreEqual(result.PropInt, _dict["PropInt"]);
            Assert.AreEqual(result.PropMoney, _dict["PropMoney"]);
            Assert.AreEqual(result.PropDictionary, _dict["PropDictionary"]);

            CheckNested(result.PropNested, _dict["PropNested"] as Dictionary<string, object>);

            Assert.AreEqual(result.PropList.Count, 5);
            Assert.AreEqual(result.PropArray.Length, 4);

            static void CheckNested(TestClass nested, IDictionary<string, object> nestedDict)
            {
                Assert.AreEqual(nested.PropString, nestedDict["PropString"]);
                Assert.AreEqual(nested.PropBool, nestedDict["PropBool"]);
                Assert.AreEqual(nested.PropDate, nestedDict["PropDate"]);
                Assert.AreEqual(nested.PropInt, nestedDict["PropInt"]);
                Assert.AreEqual(nested.PropMoney, nestedDict["PropMoney"]);
            }
        }
    }
}
