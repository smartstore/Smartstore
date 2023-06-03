using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class StoreExtensionsTests
    {
        [Test]
        public void Can_create_absolute_url()
        {
            var store = new Store
            {
                Url = "http://mycompany.com/shop",
                SslEnabled = true,
                //SslPort = 8080
            };

            var abs1 = store.GetAbsoluteUrl(new PathString("/shop"), "/shop/path");
            abs1.ShouldEqual("https://mycompany.com/shop/path");

            var abs2 = store.GetAbsoluteUrl("/shop/path");
            abs2.ShouldEqual("https://mycompany.com/shop/shop/path");

            store.SslPort = 8080;
            abs1 = store.GetAbsoluteUrl(new PathString("/shop"), "/shop/path");
            abs1.ShouldEqual("https://mycompany.com:8080/shop/path");
            abs2 = store.GetAbsoluteUrl("/shop/path");
            abs2.ShouldEqual("https://mycompany.com:8080/shop/shop/path");

            store.SslEnabled = false;

            var abs3 = store.GetAbsoluteUrl(new PathString("/shop"), "/shop/path/");
            abs3.ShouldEqual("http://mycompany.com/shop/path/");

            var abs4 = store.GetAbsoluteUrl(new PathString("/store"), "/shop/path/");
            abs4.ShouldEqual("http://mycompany.com/shop/shop/path/");

            var abs5 = store.GetAbsoluteUrl(null);
            abs5.ShouldEqual("http://mycompany.com/shop/");
        }

        [Test]
        public void Can_parse_host_values()
        {
            var store = new Store()
            {
                Hosts = "yourstore.com, www.yourstore.com, "
            };

            var hosts = store.ParseHostValues();
            hosts.Length.ShouldEqual(2);
            hosts[0].ShouldEqual("yourstore.com");
            hosts[1].ShouldEqual("www.yourstore.com");
        }

        [Test]
        public void Can_find_host_value()
        {
            var store = new Store()
            {
                Hosts = "yourstore.com, www.yourstore.com, "
            };

            store.ContainsHostValue(null).ShouldEqual(false);
            store.ContainsHostValue("").ShouldEqual(false);
            store.ContainsHostValue("store.com").ShouldEqual(false);
            store.ContainsHostValue("yourstore.com").ShouldEqual(true);
            store.ContainsHostValue("yoursTore.com").ShouldEqual(true);
            store.ContainsHostValue("www.yourstore.com").ShouldEqual(true);
        }
    }
}
