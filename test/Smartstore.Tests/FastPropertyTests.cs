using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Test.Common;

namespace Smartstore.Tests
{
    [TestFixture]
    public class FastPropertyTests
    {
        private readonly static string[] _props = new[]
        {
            nameof(Product.Name),
            nameof(Product.ShortDescription),
            nameof(Product.FullDescription),
            nameof(Product.Gtin),
            nameof(Product.Sku),
            nameof(Product.SystemName)
        };
        private readonly int _times = 10000;

        [Test]
        public void GetPropertiesReflectionPerfTest()
        {
            for (var i = 1; i <= _times; i++)
            {
                var props = typeof(ProductBundleItem).GetTypeInfo().GetRuntimeProperties().ToDictionary(x => x.Name);
            }
        }

        [Test]
        public void GetPropertiesFastPerfTest()
        {
            for (var i = 1; i <= _times; i++)
            {
                var props = FastProperty.GetProperties(typeof(ProductBundleItem));
            }
        }

        [Test]
        public void PropertyFastPerfTest()
        {
            var product = new Product();
            foreach (var propName in _props)
            {
                var prop = FastProperty.GetProperties(typeof(Product))[propName];
                for (var i = 1; i <= _times; i++)
                {
                    var value = prop.GetValue(product);
                    prop.SetValue(product, "Some Text");
                }
            }
        }

        [Test]
        public void PropertyReflectionPerfTest()
        {
            var product = new Product();
            foreach (var propName in _props)
            {
                var prop = typeof(Product).GetTypeInfo().GetRuntimeProperty(propName);
                for (var i = 1; i <= _times; i++)
                {
                    var value = prop.GetValue(product);
                    prop.SetValue(product, "Some Text");
                }
            }
        }

        [Test]
        public void PropertyExpressionPerfTest()
        {
            var product = new Product();
            foreach (var propName in _props)
            {
                var prop = typeof(Product).GetTypeInfo().GetRuntimeProperty(propName);
                var provider = new ExpressionValueProvider(prop);

                for (var i = 1; i <= _times; i++)
                {
                    var value = provider.GetValue(product);
                    provider.SetValue(product, "Some Text");
                }
            }
        }

        [Test]
        public void InvokerReflectionPerfTest()
        {
            var testClass = new TestClass();
            var mi = typeof(TestClass).GetMethod("InvokeMe", new[] { typeof(string), typeof(bool), typeof(int) });

            for (var i = 1; i <= _times; i++)
            {
                var value = mi.Invoke(testClass, new object[] { "Yo", true, 10 });
            }
        }

        [Test]
        public void ActivatorReflectionPerfTest()
        {
            for (var i = 1; i <= _times; i++)
            {
                var value = Activator.CreateInstance(typeof(TestClass), new object[] { Array.Empty<Product>(), 3, "yoman" });
            }
        }


        //[Test]
        //public void CanCreateFastPropertyByLambda()
        //{
        //    var fastProp = FastProperty.GetProperty<Product>(x => x.Name, PropertyCachingStrategy.Cached);
        //    fastProp.ShouldNotBeNull();

        //    Assert.AreEqual(fastProp.Property.Name, "Name");

        //    // from cache
        //    var fastProp2 = FastProperty.GetProperty<Product>(x => x.Name);
        //    Assert.AreSame(fastProp, fastProp2);
        //}

        //[Test]
        //public void CanCreateFastPropertyByPropInfo()
        //{
        //    var pi = typeof(Product).GetProperty("Name");

        //    var fastProp = FastProperty.GetProperty(pi, PropertyCachingStrategy.Cached);
        //    fastProp.ShouldNotBeNull();

        //    Assert.AreEqual(fastProp.Property.Name, "Name");

        //    // from cache
        //    var fastProp2 = FastProperty.GetProperty<Product>(x => x.Name);
        //    Assert.AreSame(fastProp, fastProp2);
        //    Assert.AreSame(fastProp.Property, pi);
        //}

        //[Test]
        //public void CanCreateFastPropertyByName()
        //{
        //    var fastProp = FastProperty.GetProperty(typeof(Product), "Name", PropertyCachingStrategy.Cached);
        //    fastProp.ShouldNotBeNull();

        //    Assert.AreEqual(fastProp.Property.Name, "Name");

        //    // from cache
        //    var fastProp2 = FastProperty.GetProperty<Product>(x => x.Name);
        //    Assert.AreSame(fastProp, fastProp2);

        //    var product = new Product { Name = "MyName" };
        //    var name = fastProp.GetValue(product);

        //    Assert.AreEqual("MyName", name);
        //}
    }

    public class TestClass
    {
        public TestClass()
        {
        }
        public TestClass(IEnumerable<Product> param1)
        {
        }
        public TestClass(int param1)
        {
        }
        public TestClass(IEnumerable<Product> param1, int param2)
        {
        }
        public TestClass(IEnumerable<Product> param1, int param2, string param3)
        {
        }
        public TestClass(DateTime param1)
        {
        }
        public TestClass(double param1)
        {
        }
        public TestClass(decimal param1)
        {
        }
        public TestClass(long param1)
        {
        }

        public string InvokeMe() => string.Empty;
        public string InvokeMe(string p1, bool p2, int p3) => string.Empty;
    }
}