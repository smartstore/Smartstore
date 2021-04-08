using System;
using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Customers
{
    public partial class CustomerRewardPointsModel : ModelBase
    {
        public List<RewardPointsHistoryModel> RewardPoints { get; set; } = new();
        public string RewardPointsBalance { get; set; }

        #region Nested classes
        public partial class RewardPointsHistoryModel : EntityModelBase
        {
            [LocalizedDisplay("RewardPoints.Fields.Points")]
            public int Points { get; set; }

            [LocalizedDisplay("RewardPoints.Fields.PointsBalance")]
            public int PointsBalance { get; set; }

            [LocalizedDisplay("RewardPoints.Fields.Message")]
            public string Message { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}
