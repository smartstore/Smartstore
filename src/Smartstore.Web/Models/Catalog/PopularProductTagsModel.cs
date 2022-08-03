namespace Smartstore.Web.Models.Catalog
{
    public partial class PopularProductTagsModel : ModelBase
    {
        public int TotalTags { get; set; }

        public List<ProductTagModel> Tags { get; set; } = new();

        protected virtual int GetFontSize(double weight, double mean, double stdDev)
        {
            double factor = (weight - mean);

            if (factor != 0 && stdDev != 0) factor /= stdDev;

            return (factor > 2) ? 150 :
                (factor > 1) ? 120 :
                (factor > 0.5) ? 100 :
                (factor > -0.5) ? 90 :
                (factor > -1) ? 85 :
                (factor > -2) ? 80 :
                75;
        }

        protected virtual double Mean(IEnumerable<double> values)
        {
            double sum = 0;
            int count = 0;

            foreach (double d in values)
            {
                sum += d;
                count++;
            }

            return sum / count;
        }

        protected virtual double StdDev(IEnumerable<double> values, out double mean)
        {
            mean = Mean(values);
            double sumOfDiffSquares = 0;
            int count = 0;

            foreach (double d in values)
            {
                double diff = (d - mean);
                sumOfDiffSquares += diff * diff;
                count++;
            }

            return Math.Sqrt(sumOfDiffSquares / count);
        }

        public virtual int GetFontSize(ProductTagModel productTag)
        {
            var itemWeights = new List<double>();

            foreach (var tag in Tags)
            {
                itemWeights.Add(tag.ProductCount);
            }

            var stdDev = StdDev(itemWeights, out var mean);

            return GetFontSize(productTag.ProductCount, mean, stdDev);
        }
    }
}
