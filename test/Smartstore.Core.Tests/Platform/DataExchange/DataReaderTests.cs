using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Excel;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.DataExchange
{
    [TestFixture]
    public class DataReaderTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Test]
        public void CanReadExcel()
        {
            var stream = GetFileStream("testdata.xlsx");
            using var reader = new ExcelReader(stream);

            var table = LightweightDataTable.FromDataReader(reader);
            VerifyDataTable(table);
        }

        [Test]
        public void CanReadCsv()
        {
            var stream = GetFileStream("testdata.csv");
            using var reader = new CsvDataReader(new StreamReader(stream, Encoding.UTF8));

            var table = LightweightDataTable.FromDataReader(reader);
            VerifyDataTable(table);
        }

        private Stream GetFileStream(string fileName)
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream($"Smartstore.Core.Tests.Platform.DataExchange.Files.{fileName}");
        }

        private static void VerifyDataTable(IDataTable table)
        {
            Assert.Multiple(() =>
            {
                Assert.That(table.Columns, Has.Count.EqualTo(12), "Columns Count");
                Assert.That(table.Rows, Has.Count.EqualTo(10), "Rows Count");
            });

            var cols = table.Columns;

            Assert.Multiple(() =>
            {
                Assert.That(cols[0].Name, Is.EqualTo("Id"));
                Assert.That(cols[1].Name, Is.EqualTo("Sku"));
                Assert.That(cols[2].Name, Is.EqualTo("Name"));
                Assert.That(cols[3].Name, Is.EqualTo("Description"));
                Assert.That(cols[4].Name, Is.EqualTo("Bool"));
                Assert.That(cols[5].Name, Is.EqualTo("Date"));
                Assert.That(cols[6].Name, Is.EqualTo("OADate"));
                Assert.That(cols[7].Name, Is.EqualTo("UnixDate"));
                Assert.That(cols[8].Name, Is.EqualTo("Int"));
                Assert.That(cols[9].Name, Is.EqualTo("Double"));
                Assert.That(cols[10].Name, Is.EqualTo("Guid"));
                Assert.That(cols[11].Name, Is.EqualTo("IntList"));
            });

            var rows = table.Rows;

            rows[3]["Sku"].ShouldEqual("SKU 4");
            rows[1]["Name"].ShouldEqual("äöü");
            rows[7]["Description"].ShouldEqual("Description 8");
            rows[0]["Bool"].Convert<bool>().ShouldBeTrue();
            rows[5]["Bool"].Convert<bool>().ShouldBeTrue();
            rows[6]["Bool"].Convert<bool>().ShouldBeFalse();
            rows[3]["Double"].Convert<double>().ShouldEqual(9999.765);
            rows[0]["OADate"].Convert<DateTime>().ShouldEqual(DateTime.FromOADate(rows[0]["OADate"].Convert<double>()));
            rows[3]["Guid"].Convert<Guid>().ShouldEqual(Guid.Parse("77866957-eec3-4b35-950f-10d1699ac46d"));

            rows[0]["IntList"].Convert<List<int>>().ShouldSequenceEqual(new List<int> { 1, 2, 3, 4 });
            rows[1]["IntList"].Convert<List<short>>().ShouldSequenceEqual(new List<short> { 1, 2, 3, 4 });
            rows[5]["IntList"].Convert<List<double>>().ShouldSequenceEqual(new List<double> { 1, 2, 3, 4 });
        }
    }
}
