using FluentMigrator;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-10-01 13:00:00", "Core: Move data exchange files")]
    internal class MoveDataExchangeFiles : Migration, IDataSeeder<SmartDbContext>
    {
        private readonly IApplicationContext _appContext;
        private readonly ILogger _logger;

        public MoveDataExchangeFiles(IApplicationContext appContext, ILogger logger)
        {
            _appContext = appContext;
            _logger = logger;
        }

        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Late;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            const string dirName = "exchange";

            try
            {
                var sourceDir = await _appContext.WebRoot.GetDirectoryAsync(dirName);
                if (!sourceDir.Exists)
                {
                    return;
                }

                var targetDir = await _appContext.TenantRoot.GetDirectoryAsync(dirName);
                if (!targetDir.Exists)
                {
                    await targetDir.CreateAsync(cancelToken);
                }

                var source = _appContext.ContentRoot.AttachEntry(sourceDir);
                var target = _appContext.ContentRoot.AttachEntry(targetDir);

                await _appContext.ContentRoot.CopyDirectoryAsync(source, target);

                await _appContext.WebRoot.TryDeleteDirectoryAsync(dirName);
            }
            catch (Exception ex)
            {
                // Do not break any other data seeding.
                _logger.Error(ex);
            }
        }
    }
}
