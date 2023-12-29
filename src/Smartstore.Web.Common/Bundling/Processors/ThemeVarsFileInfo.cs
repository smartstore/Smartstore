using System.IO.Hashing;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;
using Smartstore.Utilities;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bundling.Processors
{
    internal class ThemeVarsFileInfo : IFileInfo, IFileHashProvider
    {
        public const string FileName = "themevars.scss";
        public const string Path = "/.app/themevars.scss";

        private readonly string _theme;
        private readonly int _storeId;
        private readonly ThemeVariableRepository _repo;

        private string _content;
        private int? _contentHash;

        private ThemeVarsFileInfo(string name)
        {
            Name = name;
            PhysicalPath = PathUtility.Join("/.app/", name);
            LastModified = DateTimeOffset.UtcNow;
        }

        public ThemeVarsFileInfo(string name, string theme, int storeId, ThemeVariableRepository repo)
            : this(name)
        {
            Guard.NotNull(repo, nameof(repo));

            _theme = theme;
            _storeId = storeId;
            _repo = repo;
        }

        public ThemeVarsFileInfo(IDictionary<string, object> rawVariables, ThemeVariableRepository repo)
            : this(repo.BuildVariables(rawVariables), repo)
        {
        }

        public ThemeVarsFileInfo(IDictionary<string, string> variables, ThemeVariableRepository repo)
            : this(ThemeVariableRepository.GenerateSass(variables), repo)
        {
        }

        public ThemeVarsFileInfo(string content, ThemeVariableRepository repo)
            : this(FileName)
        {
            Guard.NotNull(content);
            Guard.NotNull(repo);

            _content = content;
            _repo = repo;
        }

        public bool Exists => true;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified { get; }

        public long Length => CreateReadStream().Length;

        public string Name { get; }

        public string PhysicalPath { get; }

        public Stream CreateReadStream()
        {
            return new MemoryStream().WriteString(GetContentAsync().Await());
        }

        public async Task<int> GetFileHashAsync()
        {
            if (_contentHash == null)
            {
                var css = await GetContentAsync();
                _contentHash = (int)XxHash32.HashToUInt32(css.GetBytes());
            }

            return _contentHash.Value;
        }

        private async Task<string> GetContentAsync()
        {
            if (_content == null)
            {
                var css = string.Empty;
                if (_theme.HasValue())
                {
                    css = await _repo.GetPreprocessorCssAsync(_theme, _storeId);
                }

                _content = css;
            }

            return _content;
        }
    }
}
