using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Net.Mail;
using Smartstore.Test.Common;

namespace Smartstore.Tests
{
    [TestFixture]
    public class ConversionTests
    {
        [Test]
        public void CanConvertNullables()
        {
            var r1 = ((double)3).Convert<double?>();
            r1.ShouldBe<double?>();
            Assert.That(r1.Value, Is.EqualTo(3));

            var r2 = ((double?)3).Convert<double>();
            r2.ShouldBe<double>();
            Assert.That(r2, Is.EqualTo(3));

            var r3 = (true).Convert<bool?>();
            r3.ShouldBe<bool?>();
            Assert.That(r3.Value, Is.EqualTo(true));

            var r4 = ("1000").Convert<double?>();
            r4.ShouldBe<double?>();
            Assert.That(r4.Value, Is.EqualTo(1000));

            var r5 = ((int?)5).Convert<long>();
            r5.ShouldBe<long>();
            Assert.That(r5, Is.EqualTo(5));

            var r6 = ((short)5).Convert(typeof(int));
            r6.ShouldBe<int>();
            Assert.That(r6, Is.EqualTo(5));
        }

        [Test]
        public void CanConvertEnums()
        {
            var e1 = "CreateInstance".Convert<BindingFlags>();
            e1.ShouldBe<BindingFlags>();
            Assert.That(e1, Is.EqualTo(BindingFlags.CreateInstance));

            var e2 = ("CreateInstance").Convert<BindingFlags?>();
            e2.ShouldBe<BindingFlags?>();
            Assert.That(e2.Value, Is.EqualTo(BindingFlags.CreateInstance));

            BindingFlags flags = BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.IgnoreCase;

            var e3 = flags.Convert<string>();
            e3.ShouldBe<string>();
            Assert.That(e3, Is.EqualTo("IgnoreCase, CreateInstance, GetProperty"));

            var e5 = e3.Convert<BindingFlags?>();
            e5.ShouldBe<BindingFlags?>();
            Assert.That(flags, Is.EqualTo(e5.Value));

            var e4 = flags.Convert<int>();
            e4.ShouldBe<int>();
            Assert.That(e4, Is.EqualTo(4609));

            var enu = AttributeControlType.FileUpload;
            Assert.That(enu.Convert<int>(), Is.EqualTo((int)enu));
            Assert.That(enu.Convert<string>(), Is.EqualTo("FileUpload"));
        }

        [Test]
        public void CanConvertBoolean()
        {
            var b = "yes".Convert<bool>();
            Assert.That(b, Is.True);

            b = "off".Convert<bool>();
            Assert.That(b, Is.False);

            b = 1.Convert<bool>();
            Assert.That(b, Is.True);

            b = 0.Convert<bool>();
            Assert.That(b, Is.False);

            var s = true.Convert<string>();
            Assert.That(s, Is.EqualTo("True"));

            var bn = "true".Convert<bool?>();
            Assert.That(bn.Value, Is.True);

            bn = "wahr".Convert<bool?>();
            Assert.That(bn.Value, Is.True);

            bn = "".Convert<bool?>();
            Assert.That(bn.HasValue, Is.False);
        }

        [Test]
        public void CanConvertNumerics()
        {
            "1000".Convert<int>().ShouldBe<int>();
            "1000".Convert<int>().ShouldEqual(1000);

            var i = ((short?)null).Convert<int?>();
            Assert.That(i.HasValue, Is.False);

            var sh = ((decimal?)10).Convert<short>();
            sh.ShouldBe<short>();
            Assert.That(sh, Is.EqualTo(10));

            var dec = ((double)10).Convert<decimal?>();
            dec.ShouldBe<decimal?>();
            Assert.That(dec.Value, Is.EqualTo(10));

            var dbl = ((decimal)10).Convert<double?>();
            dbl.ShouldBe<double?>();
            Assert.That(dbl.Value, Is.EqualTo(10));

            var f = 20f.Convert<int?>();
            f.ShouldBe<int?>();
            Assert.That(f.Value, Is.EqualTo(20));

            var f2 = ((float?)20f).Convert<int>();
            f2.ShouldBe<int>();
            Assert.That(f2, Is.EqualTo(20));

            var culture = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");

            "123567896,54".Convert<decimal>(culture).ShouldBe<decimal>();
        }

        [Test]
        public void CanConvertDateTime()
        {
            var dt = ((double)40248.3926).Convert<DateTime>();
            dt.ShouldBe<DateTime>();
            dt.Year.ShouldEqual(2010);
            dt.Month.ShouldEqual(3);
            dt.Day.ShouldEqual(11);
        }

        [Test]
        public void CanConvertEnumerables()
        {
            var list = "1,2,3,4,5".Convert<IList<int>>();
            list.ShouldBe<List<int>>();
            Assert.That(list, Has.Count.EqualTo(5));
            Assert.That(list[2], Is.EqualTo(3));

            var list2 = "1,0,off,wahr,false,y,n".Convert<ICollection<bool>>();
            list2.ShouldBe<List<bool>>();
            Assert.That(list2, Has.Count.EqualTo(7));
            Assert.That(list2.ElementAt(3), Is.EqualTo(true));

            "1,2,3,4,5".Convert<IReadOnlyCollection<int>>().ShouldBe<ReadOnlyCollection<int>>();
            "1,2,3,4,5".Convert<IReadOnlyList<int>>().ShouldBe<ReadOnlyCollection<int>>();
            "1,2,3,4,5".Convert<HashSet<double>>().ShouldBe<HashSet<double>>();
            "1,2,3,4,5".Convert<Stack<int>>().ShouldBe<Stack<int>>();
            "1,2,3,4,5".Convert<ISet<int>>().ShouldBe<HashSet<int>>();
            "1,2,3,4,5".Convert<Queue<int>>().ShouldBe<Queue<int>>();
            "1,2,3,4,5".Convert<LinkedList<string>>().ShouldBe<LinkedList<string>>();
            "1,2,3,4,5".Convert<ConcurrentBag<int>>().ShouldBe<ConcurrentBag<int>>();
            "1,2,3,4,5".Convert<ArraySegment<int>>().ShouldBe<ArraySegment<int>>();

            var list3 = new List<int>(new int[] { 1, 2, 3, 4, 5 });
            var str = list3.Convert<string>();
            Assert.That(str, Is.EqualTo("1,2,3,4,5"));

            var converter = TypeConverterFactory.GetConverter<double[]>();
            converter.ShouldBe<EnumerableConverter<double>>();

            var arr3 = list3.Convert<int[]>();
            arr3.ShouldBe<int[]>();
            Assert.That(list3, Has.Count.EqualTo(5));
            Assert.That(list3[2], Is.EqualTo(3));

            var list4 = ((double)5).Convert<List<int>>();
            list4.ShouldBe<List<int>>();
            Assert.That(list4, Has.Count.EqualTo(1));
            Assert.That(list4[0], Is.EqualTo(5));

            var list5 = new List<string>(new string[] { "1", "2", "3", "4", "5" });
            var arr4 = list5.Convert<float[]>();
            arr4.ShouldBe<float[]>();
            Assert.That(list5, Has.Count.EqualTo(5));
            Assert.That(list5[3], Is.EqualTo("4"));
        }

        [Test]
        public void CanConvertShippingOptions()
        {
            TypeConverterFactory.Providers.Insert(0, new ShippingOptionConverterProvider());

            var shippingOption = new ShippingOption
            {
                ShippingMethodId = 2,
                Name = "Name",
                Description = "Desc",
                Rate = 1,
                ShippingRateComputationMethodSystemName = "SystemName"
            };
            var soStr = shippingOption.Convert<string>();
            Assert.That(soStr, Is.Not.Empty);

            var arr = (new[] { shippingOption.Convert<string>() }).Convert<ShippingOption[]>();
            arr.ShouldBe<ShippingOption[]>();
            Assert.That(arr, Has.Length.EqualTo(1));
            Assert.That(arr[0].Name, Is.EqualTo("Name"));

            shippingOption = soStr.Convert<ShippingOption>();
            Assert.That(shippingOption, Is.Not.Null);
            Assert.That(shippingOption.ShippingMethodId, Is.EqualTo(2));
            Assert.That(shippingOption.Name, Is.EqualTo("Name"));
            Assert.That(shippingOption.Description, Is.EqualTo("Desc"));
            Assert.That(shippingOption.Rate, Is.EqualTo(1));
            Assert.That(shippingOption.ShippingRateComputationMethodSystemName, Is.EqualTo("SystemName"));

            var shippingOptions = new List<ShippingOption>
            {
                new() { ShippingMethodId = 1, Name = "Name1", Description = "Desc1" },
                new() { ShippingMethodId = 2, Name = "Name2", Description = "Desc2" }
            };
            soStr = shippingOptions.Convert<string>();
            Assert.That(soStr, Is.Not.Empty);

            shippingOptions = soStr.Convert<List<ShippingOption>>();
            Assert.That(shippingOptions, Has.Count.EqualTo(2));
            Assert.That(shippingOptions[1].ShippingMethodId, Is.EqualTo(2));
            Assert.That(shippingOptions[1].Description, Is.EqualTo("Desc2"));

            var shippingOptions2 = soStr.Convert<IList<ShippingOption>>();
            Assert.That(shippingOptions2, Has.Count.EqualTo(2));
            Assert.That(shippingOptions[1].ShippingMethodId, Is.EqualTo(2));
            Assert.That(shippingOptions2.First().Description, Is.EqualTo("Desc1"));
        }

        [Test]
        public void CanConvertEmailAddress()
        {
            var list = (new[] { new MailAddress("test@domain.com") }).Convert<IList<string>>();
            list.ShouldBe<IList<string>>();
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0], Is.EqualTo("test@domain.com"));

            var list2 = (new[] { "test@domain.com", "test2@domain.com" }).Convert<HashSet<MailAddress>>();
            list2.ShouldBe<HashSet<MailAddress>>();
            Assert.That(list2, Has.Count.EqualTo(2));
            Assert.That(list2.ElementAt(1).Address, Is.EqualTo("test2@domain.com"));
        }
    }
}
