using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Smartstore.Core
{
    public class HelpTopic
    {
        public readonly static HelpTopic CronExpressions = new HelpTopic("cron", "Managing+Scheduled+Tasks#ManagingScheduledTasks-Cron", "Geplante+Aufgaben+verwalten#GeplanteAufgabenverwalten-CronAusdruck");

        public HelpTopic(string name, string enPath, string dePath)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(enPath, nameof(enPath));
            Guard.NotEmpty(dePath, nameof(dePath));

            Name = name;
            EnPath = enPath;
            DePath = dePath;
        }

        public string Name { get; private set; }
        public string EnPath { get; private set; }
        public string DePath { get; private set; }
    }

    public static class SmartstoreVersion
    {
        private static readonly Version _infoVersion = new Version("1.0.0.0");
        private static readonly List<Version> _breakingChangesHistory = new List<Version>
        { 
            // IMPORTANT: Add app versions from low to high
            // NOTE: do not specify build & revision unless you have good reasons for it.
            //       A release with breaking changes should definitely have at least
            //       a greater minor version.
        };

        private const string HELP_BASEURL = "https://docs.smartstore.com/display/";

        static SmartstoreVersion()
        {
            _breakingChangesHistory.Reverse();

            // Get informational version
            var infoVersionAttr = Assembly.GetExecutingAssembly().GetAttribute<AssemblyInformationalVersionAttribute>(false);
            if (infoVersionAttr != null)
            {
                _infoVersion = new Version(infoVersionAttr.InformationalVersion);
            }
        }

        /// <summary>
        /// Gets the app version
        /// </summary>
        public static string CurrentVersion 
            => "{0}.{1}".FormatInvariant(_infoVersion.Major, _infoVersion.Minor);

        /// <summary>
        /// Gets the app full version
        /// </summary>
        public static string CurrentFullVersion 
            => _infoVersion.ToString();

        public static Version Version 
            => _infoVersion;

        public static string GenerateHelpUrl(string languageCode, HelpTopic topic)
        {
            Guard.NotEmpty(languageCode, nameof(languageCode));
            Guard.NotNull(topic, nameof(topic));

            var path = languageCode.EqualsNoCase("de") ? topic.DePath : topic.EnPath;
            return GenerateHelpUrl(languageCode, path);
        }

        public static string GenerateHelpUrl(string languageCode, string path)
        {
            Guard.NotEmpty(languageCode, nameof(languageCode));

            return String.Concat(
                HELP_BASEURL,
                GetUserGuideSpaceKey(languageCode),
                "/",
                path.EmptyNull().Trim().TrimStart('/', '\\'));
        }

        public static string GetUserGuideSpaceKey(string languageCode)
        {
            return languageCode.EqualsNoCase("de")
                ? "SDDE40"
                : "SMNET40";
        }

        /// <summary>
        /// Gets a list of Smartstore versions in which breaking changes occured,
        /// which could lead to ´module malfunctioning.
        /// </summary>
        /// <remarks>
        /// A module's <c>MinAppVersion</c> is checked against this list to assume
        /// its compatibility with the current app version.
        /// </remarks>
        internal static IEnumerable<Version> BreakingChangesHistory 
            => _breakingChangesHistory.AsEnumerable();
    }
}