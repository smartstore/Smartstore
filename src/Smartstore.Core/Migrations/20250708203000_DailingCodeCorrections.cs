using FluentMigrator;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-07-08 20:30:00", "Core: Dailing code corrections")]
    internal class DailingCodeCorrections : Migration, IDataSeeder<SmartDbContext>
    {
        private readonly ILogger _logger;

        public DailingCodeCorrections(ILogger logger)
        {
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

        // Must be updated (dial code and/or modernized name)
        private static readonly (string Iso2, int DialCode, string NewName)[] ToUpdate =
        [
            ("CU", 53,  null),                       // Cuba
            ("AW", 297, null),                       // Aruba
            ("AS", 1,   "Amerikanisch-Samoa"),       // American Samoa
            ("MP", 1,   "Nördliche Marianen"),       // Northern Mariana Islands
            ("CG", 243, "DR Kongo"),                 // DR Congo
            ("BY", 375, "Belarus"),                  // Belarus (not "Weissrussland")
            ("MM", 95,  "Myanmar"),                  // Myanmar (not "Birma")
            ("MK", 389, "Nordmazedonien"),           // North Macedonia
            ("CZ", 420, "Tschechien"),               // Czechia
            ("RU", 7,   "Russland"),                 // Russia (not "Rußland")
            ("GB", 44,  "Vereinigtes Königreich"),   // United Kingdom (not "Großbritannien")
            ("MZ", 258, "Mosambik"),                 // Mozambique (not "Mocambique")
            // Optional sanity if you keep WS (Samoa): ("WS", 685, "Samoa"),
        ];

        // Must set DiallingCode to null (territories without own country calling code or obsolete)
        private static readonly string[] ToNull =
        [
            "BV", // Bouvet Island
            "HM", // Heard & McDonald Islands
            "TF", // French Southern Territories
            "AQ", // Antarctica
            "UM", // U.S. Minor Outlying Islands
            "AN", // Netherlands Antilles (obsolete)
        ];

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            try
            {
                // Collect ISO2 keys we'll touch (updates + nullings only)
                var keys = new HashSet<string>(
                    ToUpdate.Select(x => x.Iso2).Concat(ToNull)
                );

                // Load existing rows
                var rows = await context.Countries
                    .Where(c => keys.Contains(c.TwoLetterIsoCode))
                    .ToListAsync(cancelToken);

                var byIso = rows.ToDictionary(c => c.TwoLetterIsoCode, c => c);

                // Apply updates
                foreach (var (iso2, dial, newName) in ToUpdate)
                {
                    if (!byIso.TryGetValue(iso2, out var c)) continue;

                    // Update dialing code
                    if (c.DiallingCode != dial)
                        c.DiallingCode = dial;

                    // Update name if provided (remove if you don't store names)
                    if (!string.IsNullOrWhiteSpace(newName) && c.Name != newName)
                        c.Name = newName;
                }

                // Set to null (no deletions)
                foreach (var iso2 in ToNull)
                {
                    if (!byIso.TryGetValue(iso2, out var c)) 
                        continue;

                    if (c.DiallingCode != null)
                        c.DiallingCode = null;
                }

                await context.SaveChangesAsync(cancelToken);
            }
            catch (Exception ex)
            {
                // Do not break any other data seeding.
                _logger.Error(ex);
            }
        }
    }
}