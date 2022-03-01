using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class MeasureServiceTests : ServiceTest
    {
        MeasureSettings _measureSettings;
        IMeasureService _measureService;

        MeasureDimension measureDimension1, measureDimension2, measureDimension3, measureDimension4;
        MeasureWeight measureWeight1, measureWeight2, measureWeight3, measureWeight4;

        [OneTimeSetUp]
        public new void SetUp()
        {
            measureDimension1 = new MeasureDimension()
            {
                Id = 1,
                Name = "inch(es)",
                SystemKeyword = "inch",
                Ratio = 1M,
                DisplayOrder = 1,
            };
            measureDimension2 = new MeasureDimension()
            {
                Id = 2,
                Name = "feet",
                SystemKeyword = "ft",
                Ratio = 0.08333333M,
                DisplayOrder = 2,
            };
            measureDimension3 = new MeasureDimension()
            {
                Id = 3,
                Name = "meter(s)",
                SystemKeyword = "m",
                Ratio = 0.0254M,
                DisplayOrder = 3,
            };
            measureDimension4 = new MeasureDimension()
            {
                Id = 4,
                Name = "millimetre(s)",
                SystemKeyword = "mm",
                Ratio = 25.4M,
                DisplayOrder = 4,
            };

            measureWeight1 = new MeasureWeight()
            {
                Id = 1,
                Name = "ounce(s)",
                SystemKeyword = "oz",
                Ratio = 16M,
                DisplayOrder = 1,
            };
            measureWeight2 = new MeasureWeight()
            {
                Id = 2,
                Name = "lb(s)",
                SystemKeyword = "lb",
                Ratio = 1M,
                DisplayOrder = 2,
            };
            measureWeight3 = new MeasureWeight()
            {
                Id = 3,
                Name = "kg(s)",
                SystemKeyword = "kg",
                Ratio = 0.45359237M,
                DisplayOrder = 3,
            };
            measureWeight4 = new MeasureWeight()
            {
                Id = 4,
                Name = "gram(s)",
                SystemKeyword = "g",
                Ratio = 453.59237M,
                DisplayOrder = 4,
            };

            DbContext.MeasureDimensions.AddRange(new[] { measureDimension1, measureDimension2, measureDimension3, measureDimension4 });
            DbContext.SaveChanges();

            DbContext.MeasureWeights.AddRange(new[] { measureWeight1, measureWeight2, measureWeight3, measureWeight4 });
            DbContext.SaveChanges();

            _measureSettings = new MeasureSettings
            {
                BaseDimensionId = measureDimension1.Id, //inch(es)
                BaseWeightId = measureWeight2.Id        //lb(s)
            };

            _measureService = new MeasureService(DbContext, _measureSettings);
        }

        [Test]
        public async Task Can_convert_dimension()
        {
            //from meter(s) to feet
            (await _measureService.ConvertDimensionAsync(10, measureDimension3, measureDimension2, true)).ShouldEqual(32.81);
            //from inch(es) to meter(s)
            (await _measureService.ConvertDimensionAsync(10, measureDimension1, measureDimension3, true)).ShouldEqual(0.25);
            //from meter(s) to meter(s)
            (await _measureService.ConvertDimensionAsync(13.333M, measureDimension3, measureDimension3, true)).ShouldEqual(13.33);
            //from meter(s) to millimeter(s)
            (await _measureService.ConvertDimensionAsync(10, measureDimension3, measureDimension4, true)).ShouldEqual(10000);
            //from millimeter(s) to meter(s)
            (await _measureService.ConvertDimensionAsync(10000, measureDimension4, measureDimension3, true)).ShouldEqual(10);
        }

        [Test]
        public async Task Can_convert_weight()
        {
            //from ounce(s) to lb(s)
            (await _measureService.ConvertWeightAsync(11, measureWeight1, measureWeight2, true)).ShouldEqual(0.69);
            //from lb(s) to ounce(s)
            (await _measureService.ConvertWeightAsync(11, measureWeight2, measureWeight1, true)).ShouldEqual(176);
            //from ounce(s) to  ounce(s)
            (await _measureService.ConvertWeightAsync(13.333M, measureWeight1, measureWeight1, true)).ShouldEqual(13.33);
            //from kg(s) to ounce(s)
            (await _measureService.ConvertWeightAsync(11, measureWeight3, measureWeight1, true)).ShouldEqual(388.01);
            //from kg(s) to gram(s)
            (await _measureService.ConvertWeightAsync(10, measureWeight3, measureWeight4, true)).ShouldEqual(10000);
        }
    }
}