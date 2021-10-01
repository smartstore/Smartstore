using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Polls.Domain;

namespace Smartstore.Polls.Migrations
{
    internal class PollsInstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;
        private readonly IWidgetService _widgetService;
        private readonly bool _deSeedData;

        public PollsInstallationDataSeeder(ModuleInstallationContext installContext, IWidgetService widgetService)
            : base(installContext.ApplicationContext, installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
            _widgetService = Guard.NotNull(widgetService, nameof(widgetService));
            _deSeedData = _installContext.Culture?.StartsWith("de", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        protected override async Task SeedCoreAsync()
        {
            if (_installContext.SeedSampleData == null || _installContext.SeedSampleData == true)
            {
                var polls = GetSamplePolls();
                await PopulateAsync("PopulatePolls", polls);
                await PopulateAsync("PopulatePollAnswers", GetSamplePollAnswers(polls));
            }

            await TryActivateWidgetAsync();
        }

        private async Task TryActivateWidgetAsync()
        {
            var hasActivePolls = await Context.Polls().Where(x => x.Published == true).AnyAsync();
            if (hasActivePolls)
            {
                // Activate the polls homepage widget.
                await _widgetService.ActivateWidgetAsync("Smartstore.Polls", true);
            }
        }

        private List<Poll> GetSamplePolls()
        {
            return new List<Poll>
            {
                new Poll
                {
                    LanguageId = 1,
                    Name = _deSeedData ? "Wie gefällt Ihnen der Shop?" : "How do you like the shop?",
                    SystemKeyword = "Blog",
                    Published = true,
                    ShowOnHomePage = true,
                    DisplayOrder = 10
                },
                new Poll
                {
                    LanguageId = 1,
                    Name = _deSeedData ? "Wie oft kaufen Sie Online ein?" : "Packaging & Shipping",
                    SystemKeyword = "MyAccountMenu",
                    Published = true,
                    ShowOnHomePage = true,
                    DisplayOrder = 20
                }
            };
        }

        private List<PollAnswer> GetSamplePollAnswers(List<Poll> groups)
        {
            var firstPoll = groups.FirstOrDefault(c => c.DisplayOrder == 10);
            var secondPoll = groups.FirstOrDefault(c => c.DisplayOrder == 20);

            return new List<PollAnswer>
            {
                // First poll
                new PollAnswer
                {
                    Poll = firstPoll,
                    Name = _deSeedData ? "Ausgezeichnet" : "Excellent",
                    DisplayOrder = 1
                },
                new PollAnswer
                {
                    Poll = firstPoll,
                    Name = _deSeedData ? "Gut" : "Good",
                    DisplayOrder = 2
                },
                new PollAnswer
                {
                    Poll = firstPoll,
                    Name = _deSeedData ? "Geht so" : "Poor",
                    DisplayOrder = 3
                },
                new PollAnswer
                {
                    Poll = firstPoll,
                    Name = _deSeedData ? "Schlecht" : "Very bad",
                    DisplayOrder = 4
                },

                // Second poll
                new PollAnswer
                {
                    Poll = secondPoll,
                    Name = _deSeedData ? "Täglich" : "Daily",
                    DisplayOrder = 1
                },
                new PollAnswer
                {
                    Poll = secondPoll,
                    Name = _deSeedData ? "Wöchentlich" : "Once a week",
                    DisplayOrder = 2
                },
                new PollAnswer
                {
                    Poll = secondPoll,
                    Name = _deSeedData ? "Alle zwei Wochen" : "Every two weeks",
                    DisplayOrder = 3
                },
                new PollAnswer
                {
                    Poll = secondPoll,
                    Name = _deSeedData ? "Einmal im Monat" : "Once a month",
                    DisplayOrder = 4
                },
            };
        }
    }
}
