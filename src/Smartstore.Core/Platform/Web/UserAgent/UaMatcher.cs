#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Smartstore.Core.Web
{
    internal abstract class UaMatcher
    {
        public string Name { get; set; } = default!;
        public UserAgentPlatformFamily? Platform { get; set; }
        public abstract bool Match(string userAgent, [MaybeNullWhen(true)] out string? version);
    }

    internal class RegexMatcher : UaMatcher
    {
        private readonly Regex _rg;

        public RegexMatcher(Regex rg)
        {
            _rg = Guard.NotNull(rg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Match(string userAgent, [MaybeNullWhen(true)] out string? version)
        {
            version = null;
            var match = _rg.Match(userAgent);
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

    internal class ContainsMatcher : UaMatcher
    {
        private readonly string _match;

        public ContainsMatcher(string match)
        {
            Guard.NotEmpty(match);
            _match = match;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Match(string userAgent, [MaybeNullWhen(true)] out string? version)
        {
            version = null;
            return userAgent.Contains(_match, StringComparison.OrdinalIgnoreCase);
        }
    }
}
