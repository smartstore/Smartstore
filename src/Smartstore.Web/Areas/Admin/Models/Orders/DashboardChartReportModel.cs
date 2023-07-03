namespace Smartstore.Admin.Models.Orders
{
    public class DashboardChartReportModel
    {
        public DashboardChartReportModel(int numberOfDatasets, int numberOfDataPoints, DateTime comparedFrom, DateTime comparedTo)
        {
            DataSets = new ChartDataPointReport[numberOfDatasets];
            Labels = new string[numberOfDataPoints];
            ComparedFrom = comparedFrom;
            ComparedTo = comparedTo;

            for (var i = 0; i < DataSets.Length; i++)
            {
                DataSets[i] = new(numberOfDataPoints);
            }
        }

        public static List<DashboardChartReportModel> Create(int numberOfDatasets)
        {
            var now = DateTime.UtcNow;
            var beginningOfYear = new DateTime(now.Year, 1, 1);

            return new()
            {
                // Index 0: today.
                new(numberOfDatasets, 24, DateTime.MinValue, DateTime.MinValue),
                // Index 1: yesterday.
                new(numberOfDatasets, 24, now.Date.AddDays(-2), now.Date.AddDays(-1)),
                // Index 2: last 7 days.
                new(numberOfDatasets, 7, now.Date.AddDays(-14), now.Date.AddDays(-7)),
                // Index 3: last 28 days.
                new(numberOfDatasets, 4, now.Date.AddDays(-56), now.Date.AddDays(-28)),
                // Index 4: this year.
                new(numberOfDatasets, 12, beginningOfYear.AddYears(-1), now.AddYears(-1))
            };
        }

        public ChartDataPointReport[] DataSets { get; set; }

        public string TotalAmountFormatted { get; set; }
        public decimal TotalAmount { get; set; }
        public string[] Labels { get; set; }

        public DateTime ComparedFrom { get; init; } = DateTime.MinValue;
        public DateTime ComparedTo { get; init; } = DateTime.MinValue;
        public decimal ComparedTotalAmount { get; set; }

        public int PercentageDelta
            => TotalAmount != 0 && ComparedTotalAmount != 0 ? (int)Math.Round(TotalAmount / ComparedTotalAmount * 100 - 100) : 0;

        public string PercentageDescription { get; set; } = string.Empty;
    }

    public class ChartDataPointReport
    {
        public ChartDataPointReport(int numberOfDataPoints)
        {
            Quantity = new int[numberOfDataPoints];
            Amount = new decimal[numberOfDataPoints];
            AmountFormatted = new string[numberOfDataPoints];
            QuantityFormatted = new string[numberOfDataPoints];
        }

        public decimal TotalAmount { get; set; }
        public string TotalAmountFormatted { get; set; }
        public int[] Quantity { get; set; }
        public string[] QuantityFormatted { get; set; }
        public decimal[] Amount { get; set; }
        public string[] AmountFormatted { get; set; }
    }
}
