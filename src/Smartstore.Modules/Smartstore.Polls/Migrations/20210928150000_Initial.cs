using System.Data;
using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Domain;
using Smartstore.Polls.Domain;

namespace Smartstore.Polls.Migrations
{
    [MigrationVersion("2021-09-29 15:00:00", "Polls: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string poll = "Poll";
            const string pollAnswer = "PollAnswer";
            const string pollVotingRecord = "PollVotingRecord";

            const string id = nameof(BaseEntity.Id);

            if (!Schema.Table(poll).Exists())
            {
                Create.Table(poll)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(Poll.Name)).AsString(450).NotNullable()
                        .Indexed("IX_Title")
                    .WithColumn(nameof(Poll.SystemKeyword)).AsString(200).Nullable()
                        .Indexed("IX_SystemKeyword")
                    .WithColumn(nameof(Poll.Published)).AsBoolean().NotNullable()
                    .WithColumn(nameof(Poll.ShowOnHomePage)).AsBoolean().NotNullable()
                    .WithColumn(nameof(Poll.AllowGuestsToVote)).AsBoolean().NotNullable()
                    .WithColumn(nameof(Poll.DisplayOrder)).AsInt32().NotNullable()
                    .WithColumn(nameof(Poll.StartDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(Poll.EndDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(Poll.LimitedToStores)).AsBoolean().NotNullable()
                    .WithColumn(nameof(Poll.LanguageId)).AsInt32().NotNullable()
                        .Indexed("IX_LanguageId")
                        .ForeignKey(nameof(Language), id).OnDelete(Rule.None);
            }

            if (!Schema.Table(pollAnswer).Exists())
            {
                Create.Table(pollAnswer)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(PollAnswer.Name)).AsString(450).Nullable()
                    .WithColumn(nameof(PollAnswer.NumberOfVotes)).AsInt32().NotNullable()
                    .WithColumn(nameof(PollAnswer.DisplayOrder)).AsInt32().NotNullable()
                    .WithColumn(nameof(PollAnswer.PollId)).AsInt32().NotNullable()
                        .ForeignKey(poll, id).OnDelete(Rule.Cascade);

                Create.ForeignKey()
                    .FromTable(pollAnswer).ForeignColumn(id)
                    .ToTable(nameof(CustomerContent)).PrimaryColumn(id);
            }

            if (!Schema.Table(pollVotingRecord).Exists())
            {
                Create.Table(pollVotingRecord)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(PollVotingRecord.PollAnswerId)).AsInt32().NotNullable()
                        .ForeignKey(pollAnswer, id).OnDelete(Rule.Cascade);

                Create.ForeignKey()
                    .FromTable(pollVotingRecord).ForeignColumn(id)
                    .ToTable(nameof(CustomerContent)).PrimaryColumn(id);
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave poll schema as it is or ask merchant to delete it.
        }
    }
}
