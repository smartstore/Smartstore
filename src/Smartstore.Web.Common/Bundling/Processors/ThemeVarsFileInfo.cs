using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;
using Smartstore.Utilities;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bundling.Processors
{
    internal class ThemeVarsFileInfo : IFileInfo, IFileHashProvider
    {
        private readonly string _theme;
        private readonly int _storeId;
        private readonly ThemeVariableRepository _repo;

        private string _content;
        private int? _contentHash;

        private ThemeVarsFileInfo(string name)
        {
            Name = name;
            PhysicalPath = name;
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
            : this(repo.GenerateSass(variables), repo)
        {
        }

        public ThemeVarsFileInfo(string content, ThemeVariableRepository repo)
            : this("themevars.scss")
        {
            Guard.NotNull(content, nameof(content));
            Guard.NotNull(repo, nameof(repo));

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
            return GenerateStreamFromString(GetContentAsync().Await());
        }

        public async Task<int> GetFileHashAsync()
        {
            if (_contentHash == null)
            {
                var css = await GetContentAsync();
                _contentHash = (int)XxHashUnsafe.ComputeHash(css);
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

        private static Stream GenerateStreamFromString(string value)
        {
            var stream = new MemoryStream();

            using (var writer = new StreamWriter(stream, Encoding.Unicode, 1024, true))
            {
                writer.Write(value);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }
}
