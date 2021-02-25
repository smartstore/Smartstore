using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Common;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderReportService : IOrderReportService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly Currency _reportCurrency;

        public OrderReportService(SmartDbContext db, IWorkContext workContext)
        {
            _db = db;
            _workContext = workContext;

            // Actually the primary store currency but order reporting only takes place in admin area 
            // and in admin area the working currency is always the primary store currency.
            _reportCurrency = _workContext.WorkingCurrency;
        }
    }
}
