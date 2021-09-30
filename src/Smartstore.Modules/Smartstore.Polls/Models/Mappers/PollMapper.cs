using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Polls.Domain;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Models.Public;

namespace Smartstore.Polls.Models.Mappers
{
    public static partial class PollMappingExtensions
    {
        public static async Task<PublicPollModel> MapAsync(this Poll entity, dynamic parameters = null)
        {
            var to = new PublicPollModel();
            await MapAsync(entity, to, parameters);

            return to;
        }

        public static async Task MapAsync(this Poll entity, PublicPollModel to, dynamic parameters = null)
        {
            await MapperFactory.MapAsync(entity, to, parameters);
        }
    }

    public class PollMapper : Mapper<Poll, PublicPollModel>
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        
        public PollMapper(SmartDbContext db, IWorkContext workContext)
        {
            _db = db;
            _workContext = workContext;
        }

        protected override void Map(Poll from, PublicPollModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(Poll from, PublicPollModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var setAlreadyVotedProperty = parameters?.SetAlreadyVotedProperty == true;

            to.Id = from.Id;
            to.AlreadyVoted = setAlreadyVotedProperty && (await _db.PollAnswers().GetAlreadyVotedAsync(from.Id, _workContext.CurrentCustomer.Id));
            to.Name = from.Name;

            var answers = from.PollAnswers.OrderBy(x => x.DisplayOrder);

            foreach (var answer in answers)
            {
                to.TotalVotes += answer.NumberOfVotes;
            }

            foreach (var pa in answers)
            {
                to.Answers.Add(new PublicPollAnswerModel
                {
                    Id = pa.Id,
                    Name = pa.Name,
                    NumberOfVotes = pa.NumberOfVotes,
                    PercentOfTotalVotes = to.TotalVotes > 0 ? ((Convert.ToDouble(pa.NumberOfVotes) / Convert.ToDouble(to.TotalVotes)) * Convert.ToDouble(100)) : 0
                });
            }
        }
    }
}
