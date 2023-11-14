global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
global using EfState = Microsoft.EntityFrameworkCore.EntityState;
using System.Reflection;

namespace Smartstore
{
    public class HelpTopic
    {
        public readonly static HelpTopic CronExpressions = new("cron", "Managing+Scheduled+Tasks#ManagingScheduledTasks-Cron", "Geplante+Aufgaben+verwalten#GeplanteAufgabenverwalten-CronAusdruck");

        public HelpTopic(string name, string enPath, string dePath)
        {
            Guard.NotEmpty(name);
            Guard.NotEmpty(enPath);
            Guard.NotEmpty(dePath);

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
        private static readonly Version _infoVersion = new("1.0.0.0");
        private static readonly List<Version> _breakingChangesHistory = new()
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
            if (infoVersionAttr?.InformationalVersion != null)
            {
                _infoVersion = new Version(infoVersionAttr.InformationalVersion.Split('+', '-')[0]);
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

        /// <summary>
        /// Gets a value indicating whether the given min. required app version is assumed
        /// to be compatible with the current app version
        /// </summary>
        /// <remarks>
        /// An extension is generally compatible when both app version and extension's 
        /// <c>MinorAppVersion</c> are equal, OR - when app version is greater - it is 
        /// assumed to be compatible when no breaking changes occured since <c>MinorAppVersion</c>.
        /// </remarks>
        /// <param name="minAppVersion">The min. app version to check for</param>
        /// <returns><c>true</c> when the extension's version is assumed to be compatible</returns>
        public static bool IsAssumedCompatible(Version minAppVersion)
        {
            if (minAppVersion == null || Version == minAppVersion)
            {
                return true;
            }

            if (Version < minAppVersion)
            {
                return false;
            }

            bool compatible = true;

            foreach (var version in _breakingChangesHistory)
            {
                if (version > minAppVersion)
                {
                    // There was a breaking change in a version greater
                    // than plugin's MinorAppVersion.
                    compatible = false;
                    break;
                }

                if (version <= minAppVersion)
                {
                    break;
                }
            }

            return compatible;
        }

        public static string GenerateHelpUrl(string languageCode, HelpTopic topic)
        {
            Guard.NotEmpty(languageCode);
            Guard.NotNull(topic);

            var path = languageCode.EqualsNoCase("de") ? topic.DePath : topic.EnPath;
            return GenerateHelpUrl(languageCode, path);
        }

        public static string GenerateHelpUrl(string languageCode, string path)
        {
            Guard.NotEmpty(languageCode);

            return string.Concat(
                HELP_BASEURL,
                GetUserGuideSpaceKey(languageCode),
                "/",
                path.EmptyNull().Trim().TrimStart('/', '\\'));
        }

        public static string GetUserGuideSpaceKey(string languageCode)
        {
            return languageCode.EqualsNoCase("de")
                ? "SDDE50"
                : "SMNET50";
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