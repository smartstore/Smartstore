using Smartstore.Admin.Models.Orders;

namespace Smartstore.Admin.Components
{
    public abstract class DashboardViewComponentBase : SmartViewComponent
    {
        private DateTime? _now;
        private DateTime? _userTime;
        private DateTime? _createdFrom;

        public abstract Task<IViewComponentResult> InvokeAsync();

        /// <summary>
        /// Current date in UTC.
        /// </summary>
        protected DateTime Now
        {
            get => _now ??= DateTime.UtcNow;
        }

        /// <summary>
        /// Current user date.
        /// </summary>
        protected DateTime UserTime
        {
            get => _userTime ??= Services.DateTimeHelper.ConvertToUserTime(Now, DateTimeKind.Utc);
        }

        /// <summary>
        /// Beginning of the year (in UTC).
        /// Returns the date from 4 weeks ago if the year is younger.
        /// </summary>
        protected DateTime CreatedFrom
        {
            get
            {
                if (!_createdFrom.HasValue)
                {
                    var beginningOfYear = new DateTime(Now.Year, 1, 1);
                    _createdFrom = (Now.Date - beginningOfYear).Days < 28 ? Now.AddDays(-27).Date : beginningOfYear;
                }

                return _createdFrom.Value;
            }
        }

        /// <summary>
        /// Applies a short description for the percentage change compared to the comparison period.
        /// </summary>
        protected void ApplyComparedToDescription(DashboardChartReportModel model)
        {
            if (model.ComparedFrom != DateTime.MinValue && model.PercentageDelta != 0)
            {
                var percentageStr = (model.PercentageDelta > 0 ? '+' : '-') + Math.Abs(model.PercentageDelta).ToString() + '%';
                var fromStr = Services.DateTimeHelper.ConvertToUserTime(model.ComparedFrom, DateTimeKind.Utc).ToShortDateString();
                var toStr = Services.DateTimeHelper.ConvertToUserTime(model.ComparedTo, DateTimeKind.Utc).ToShortDateString();

                model.PercentageDescription = T("Admin.Report.ChangeComparedTo", percentageStr, fromStr, toStr);
            }
            else
            {
                model.PercentageDescription = string.Empty;
            }
        }
    }
}
