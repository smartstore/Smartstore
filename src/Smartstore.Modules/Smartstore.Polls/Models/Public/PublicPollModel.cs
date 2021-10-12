using System;
using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Polls.Models.Public
{
    public partial class PublicPollListModel : EntityModelBase
    {
        public List<PublicPollModel> Polls { get; set; } = new();

        public List<PublicPollModel> ClonedPolls { get; set; } = new();
    }

    public partial class PublicPollModel : EntityModelBase, ICloneable
    {
        public string Name { get; set; }

        public string SystemKeyword { get; set; }

        public bool AlreadyVoted { get; set; }

        public int TotalVotes { get; set; }

        public List<PublicPollAnswerModel> Answers { get; set; } = new();

        public object Clone()
        {
            //we use a shallow copy (deep clone is not required here)
            return this.MemberwiseClone();
        }
    }

    public partial class PublicPollAnswerModel : EntityModelBase
    {
        public string Name { get; set; }

        public int NumberOfVotes { get; set; }

        public double PercentOfTotalVotes { get; set; }
    }
}
