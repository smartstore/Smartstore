#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Smartstore.Core.Web
{
    internal abstract class UaMatcher
    {
        public string Name { get; set; } = default!;
        public UserAgentPlatformFamily? PlatformFamily { get; set; }
        public abstract bool Match(ReadOnlySpan<char> userAgent, [MaybeNullWhen(true)] out string? version);
    }

    [DebuggerDisplay("Regex: {Regex}, Name: {Name}, Platform: {PlatformFamily}")]
    internal class RegexMatcher : UaMatcher
    {
        public RegexMatcher(Regex rg)
        {
            Regex = Guard.NotNull(rg);
        }

        public Regex Regex { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Match(ReadOnlySpan<char> userAgent, [MaybeNullWhen(true)] out string? version)
        {
            version = null;
            var match = Regex.Match(userAgent.ToString());
            if (match.Success)
            {
                if (match.Groups.ContainsKey("v"))
                {
                    version = match.Groups["v"].Value.NullEmpty();
                }
                else if (match.Groups.Count > 1)
                {
                    version = match.Groups[1].Value.NullEmpty();
                }

                return true;
            }

            return false;
        }
    }

    [DebuggerDisplay("Contains: {Contains}, Name: {Name}, Platform: {PlatformFamily}")]
    internal class ContainsMatcher : UaMatcher
    {
        public ContainsMatcher(string match)
        {
            Guard.NotEmpty(match);
            Contains = match;
        }

        public string Contains { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Match(ReadOnlySpan<char> userAgent, [MaybeNullWhen(true)] out string? version)
        {
            version = null;
            return userAgent.Contains(Contains, StringComparison.OrdinalIgnoreCase);
        }
    }
}
