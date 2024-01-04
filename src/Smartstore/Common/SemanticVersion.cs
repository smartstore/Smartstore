using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore
{
    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// </summary>
    [JsonConverter(typeof(SemanticVersionJsonConverter))]
    [TypeConverter(typeof(SemanticVersionConverter))]
    public sealed partial class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private static readonly SearchValues<char> _componentSearchValues = SearchValues.Create("-+");

        // Versions containing up to 4 digits
        private static readonly Regex _semanticVersionRegex = SemanticVersionRegex();

        // Strict SemVer 2.0.0 format, this may contain only 3 digits.
        private static readonly Regex _strictSemanticVersionRegex = StrictSemanticVersionRegex();

        private readonly string _originalString;
        private string _normalizedVersionString;

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="version">Version string.</param>
        public SemanticVersion(string version)
            : this(Parse(version))
        {
            // The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it. 
            // The original string represents the original form in which the version is represented to be used when printing.
            _originalString = version;
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="major">Major version X.y.z</param>
        /// <param name="minor">Minor version x.Y.z</param>
        /// <param name="build">Patch version x.y.Z</param>
        /// <param name="revision">Revision version x.y.z.R</param>
        public SemanticVersion(int major, int minor = 0, int build = 0, int revision = 0)
            : this(new Version(major, minor, build, revision))
        {
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="major">Major version X.y.z</param>
        /// <param name="minor">Minor version x.Y.z</param>
        /// <param name="build">Patch version x.y.Z</param>
        /// <param name="specialVersion">Release label</param>
        public SemanticVersion(int major, int minor, int build, string specialVersion)
            : this(new Version(major, minor, build), specialVersion)
        {
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="major">Major version X.y.z</param>
        /// <param name="minor">Minor version x.Y.z</param>
        /// <param name="build">Patch version x.y.Z</param>
        /// <param name="specialVersion">Release label</param>
        /// <param name="metadata">Build metadata</param>
        public SemanticVersion(int major, int minor, int build, string specialVersion, string metadata)
            : this(new Version(major, minor, build), specialVersion, metadata)
        {
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        public SemanticVersion(Version version)
            : this(version, string.Empty)
        {
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="specialVersion">Release label</param>
        public SemanticVersion(Version version, string specialVersion)
            : this(version, specialVersion, metadata: null, originalString: null)
        {
        }

        /// <summary>
        /// SemanticVersion
        /// </summary>
        /// <param name="specialVersion">Release label</param>
        /// <param name="metadata">Build metadata</param>
        public SemanticVersion(Version version, string specialVersion, string metadata)
         : this(version, specialVersion, metadata, originalString: null)
        {
        }

        private SemanticVersion(Version version, string specialVersion, string metadata, string originalString)
        {
            Guard.NotNull(version, nameof(version));

            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? string.Empty;
            Metadata = metadata;

            _originalString = string.IsNullOrEmpty(originalString) ? version.ToString()
                + (!string.IsNullOrEmpty(specialVersion) ? '-' + specialVersion : null)
                + (!string.IsNullOrEmpty(metadata) ? '+' + metadata : null)
                : originalString;
        }

        internal SemanticVersion(SemanticVersion semVer)
        {
            _originalString = semVer.ToOriginalString();
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
            Metadata = semVer.Metadata;
        }

        /// <summary>
        /// Gets the normalized version portion.
        /// </summary>
        public Version Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the optional release label. For SemVer 2.0.0 this may contain multiple '.' separated parts.
        /// </summary>
        public string SpecialVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// SemVer 2.0.0 build metadata. This is not used for comparing or sorting.
        /// </summary>
        public string Metadata
        {
            get;
            private set;
        }

        public string[] GetOriginalVersionComponents()
        {
            if (!string.IsNullOrEmpty(_originalString))
            {
                string original;

                // Search the start of the SpecialVersion part or metadata, if any
                int labelIndex = _originalString.AsSpan().IndexOfAny(_componentSearchValues);
                if (labelIndex != -1)
                {
                    // remove the SpecialVersion or metadata part
                    original = _originalString[..labelIndex];
                }
                else
                {
                    original = _originalString;
                }

                return SplitAndPadVersionString(original);
            }
            else
            {
                return SplitAndPadVersionString(Version.ToString());
            }
        }

        private static string[] SplitAndPadVersionString(string version)
        {
            string[] a = version.Split('.');
            if (a.Length == 4)
            {
                return a;
            }
            else
            {
                // if 'a' has less than 4 elements, we pad the '0' at the end 
                // to make it 4.
                var b = new string[4] { "0", "0", "0", "0" };
                Array.Copy(a, 0, b, 0, a.Length);
                return b;
            }
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static SemanticVersion Parse(string version)
        {
            Guard.NotEmpty(version, nameof(version));

            if (!TryParse(version, out var semVer))
            {
                throw new ArgumentException($"Invalid version string '{version}'.", nameof(version));
            }
            return semVer;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _semanticVersionRegex, out value);
        }

        /// <summary>
        /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
        /// </summary>
        public static bool TryParseStrict(string version, out SemanticVersion value)
        {
            return TryParseInternal(version, _strictSemanticVersionRegex, out value);
        }

        private static bool TryParseInternal(string version, Regex regex, out SemanticVersion semVer)
        {
            semVer = null;
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            var match = regex.Match(version.Trim());
            if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out var versionValue))
            {
                return false;
            }

            semVer = new SemanticVersion(
                NormalizeVersionValue(versionValue),
                RemoveLeadingChar(match.Groups["Release"].Value),
                RemoveLeadingChar(match.Groups["Metadata"].Value),
                version.Replace(" ", ""));

            return true;
        }

        // Remove the - or + from a version section.
        private static string RemoveLeadingChar(string s)
        {
            if (s != null && s.Length > 0)
            {
                return s[1..];
            }

            return s;
        }

        /// <summary>
        /// Attempts to parse the version token as a SemanticVersion.
        /// </summary>
        /// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
        public static SemanticVersion ParseOptionalVersion(string version)
        {
            _ = TryParse(version, out var semVer);
            return semVer;
        }

        private static Version NormalizeVersionValue(Version version)
        {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is not SemanticVersion other)
            {
                throw new ArgumentException("Type to compare must be an instance of SemanticVersion.", nameof(obj));

            }

            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion other)
        {
            if (other is null)
            {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0)
            {
                return result;
            }

            bool empty = string.IsNullOrEmpty(SpecialVersion);
            bool otherEmpty = string.IsNullOrEmpty(other.SpecialVersion);
            if (empty && otherEmpty)
            {
                return 0;
            }
            else if (empty)
            {
                return 1;
            }
            else if (otherEmpty)
            {
                return -1;
            }

            // Compare the release labels using SemVer 2.0.0 comparision rules.
            var releaseLabels = SpecialVersion.Split('.');
            var otherReleaseLabels = other.SpecialVersion.Split('.');

            return CompareReleaseLabels(releaseLabels, otherReleaseLabels);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2)
        {
            if (version1 is null)
            {
                return version2 is null;
            }

            return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
        {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2)
        {
            Guard.NotNull(version1, nameof(version1));
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
        {
            return version1 == version2 || version1 < version2;
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2)
        {
            Guard.NotNull(version1, nameof(version1));
            return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
        {
            return version1 == version2 || version1 > version2;
        }

        /// <summary>
        /// Returns the original version string without build metadata.
        /// </summary>
        /// <remarks>SemVer 2.0.0 versions using build metadata or multiple release labels will be normalized.
        /// SemVer 1.0.0 versions cannot be normalized in this method for backwards compatibility reasons.</remarks>
        public override string ToString()
        {
            if (IsSemVer2())
            {
                // Normalize semver2 to match Versioning
                return ToNormalizedString();
            }
            else
            {
                // Remove metadata from the original string if it exists.
                var plusIndex = _originalString.IndexOf('+');

                if (plusIndex > -1)
                {
                    return _originalString[..plusIndex];
                }
            }

            return _originalString;
        }

        /// <summary>
        /// Returns the normalized string representation of this instance of <see cref="SemanticVersion"/>.
        /// If the instance can be strictly parsed as a <see cref="SemanticVersion"/>, the normalized version
        /// string if of the format {major}.{minor}.{build}[-{special-version}]. If the instance has a non-zero
        /// value for <see cref="Version.Revision"/>, the format is {major}.{minor}.{build}.{revision}[-{special-version}].
        /// </summary>
        /// <remarks>Build metadata is not included.</remarks>
        /// <returns>The normalized string representation.</returns>
        public string ToNormalizedString()
        {
            if (_normalizedVersionString == null)
            {
                var builder = new StringBuilder();
                builder
                    .Append(Version.Major)
                    .Append('.')
                    .Append(Version.Minor)
                    .Append('.')
                    .Append(Math.Max(0, Version.Build));

                if (Version.Revision > 0)
                {
                    builder.Append('.')
                           .Append(Version.Revision);
                }

                if (!string.IsNullOrEmpty(SpecialVersion))
                {
                    builder.Append('-')
                           .Append(SpecialVersion);
                }

                _normalizedVersionString = builder.ToString();
            }

            return _normalizedVersionString;
        }

        /// <summary>
        /// Returns the full normalized string including build metadata.
        /// </summary>
        public string ToFullString()
        {
            var s = ToNormalizedString();

            if (!string.IsNullOrEmpty(Metadata))
            {
                s = string.Format(CultureInfo.InvariantCulture, "{0}+{1}", s, Metadata);
            }

            return s;
        }

        /// <summary>
        /// Returns the original string used to construct the version. This includes metadata.
        /// </summary>
        public string ToOriginalString()
        {
            return _originalString;
        }

        /// <summary>
        /// True if the version contains metadata or multiple release labels.
        /// </summary>
        public bool IsSemVer2()
        {
            return !string.IsNullOrEmpty(Metadata)
                || !string.IsNullOrEmpty(SpecialVersion) && SpecialVersion.Contains('.');
        }

        public bool Equals(SemanticVersion other)
        {
            return other is not null &&
                   Version.Equals(other.Version) &&
                   SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            var semVer = obj as SemanticVersion;
            return semVer is not null && Equals(semVer);
        }

        public override int GetHashCode()
        {
            int hashCode = Version.GetHashCode();
            if (SpecialVersion != null)
            {
                hashCode = hashCode * 4567 + SpecialVersion.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>
        /// Compares sets of release labels.
        /// </summary>
        private static int CompareReleaseLabels(IEnumerable<string> version1, IEnumerable<string> version2)
        {
            var result = 0;

            var a = version1.GetEnumerator();
            var b = version2.GetEnumerator();

            var aExists = a.MoveNext();
            var bExists = b.MoveNext();

            while (aExists || bExists)
            {
                if (!aExists && bExists)
                {
                    return -1;
                }

                if (aExists && !bExists)
                {
                    return 1;
                }

                // compare the labels
                result = CompareRelease(a.Current, b.Current);

                if (result != 0)
                {
                    return result;
                }

                aExists = a.MoveNext();
                bExists = b.MoveNext();
            }

            return result;
        }

        /// <summary>
        /// Release labels are compared as numbers if they are numeric, otherwise they will be compared
        /// as strings.
        /// </summary>
        private static int CompareRelease(string version1, string version2)
        {
            // check if the identifiers are numeric
            var v1IsNumeric = int.TryParse(version1, out int version1Num);
            var v2IsNumeric = int.TryParse(version2, out int version2Num);

            int result;
            // if both are numeric compare them as numbers
            if (v1IsNumeric && v2IsNumeric)
            {
                result = version1Num.CompareTo(version2Num);
            }
            else if (v1IsNumeric || v2IsNumeric)
            {
                // numeric labels come before alpha labels
                if (v1IsNumeric)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            else
            {
                // Ignoring 2.0.0 case sensitive compare. Everything will be compared case insensitively as 2.0.1 specifies.
                result = StringComparer.OrdinalIgnoreCase.Compare(version1, version2);
            }

            return result;
        }

        #region GeneratedRegex

        [GeneratedRegex("^(?<Version>([0-9]|[1-9][0-9]*)(\\.([0-9]|[1-9][0-9]*)){2})(?<Release>-([0]\\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+(\\.([0]\\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+)*)?(?<Metadata>\\+[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
        private static partial Regex StrictSemanticVersionRegex();

        [GeneratedRegex("^(?<Version>\\d+(\\s*\\.\\s*\\d+){0,3})(?<Release>-([0]\\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+(\\.([0]\\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+)*)?(?<Metadata>\\+[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
        private static partial Regex SemanticVersionRegex();

        #endregion
    }

    internal sealed class SemanticVersionConverter : DefaultTypeConverter
    {
        public SemanticVersionConverter() : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
            => type == typeof(string);

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string str && str.HasValue() && SemanticVersion.TryParse(str, out var semVer))
            {
                return semVer;
            }

            return null;
        }
    }

    internal sealed class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
    {
        public override bool CanRead
            => true;

        public override bool CanWrite
            => true;

        public override SemanticVersion ReadJson(JsonReader reader, Type objectType, SemanticVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            reader.Read();
            var str = reader.ReadAsString();
            if (str.HasValue() && SemanticVersion.TryParse(str, out var semVer))
            {
                return semVer;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, SemanticVersion value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }
    }
}