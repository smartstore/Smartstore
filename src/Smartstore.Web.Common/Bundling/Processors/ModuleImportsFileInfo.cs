using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Web.Bundling.Processors
{
    internal class ModuleImportsFileInfo : IFileInfo, IFileHashProvider
    {
        public const string FileName = "moduleimports.scss";
        public const string Path = "/.app/moduleimports.scss";

        private string _content;

        #region Static

        private static readonly HashSet<ModuleImport> _adminImports = new();
        private static readonly HashSet<ModuleImport> _publicImports = new();

        static ModuleImportsFileInfo()
        {
            if (DataSettings.DatabaseIsInstalled())
            {
                CollectModuleImports();
            }
        }

        private static void CollectModuleImports()
        {
            var moduleCatalog = EngineContext.Current.Application.ModuleCatalog;
            var installedModules = moduleCatalog.GetInstalledModules();

            foreach (var module in installedModules)
            {
                TryAddImport(module, _publicImports, "public.scss");
                TryAddImport(module, _adminImports, "admin.scss");
            }

            static void TryAddImport(IModuleDescriptor module, HashSet<ModuleImport> imports, string name)
            {
                var file = module.WebRoot.GetFileInfo(name);
                if (file.Exists)
                {
                    imports.Add(new ModuleImport
                    {
                        PhysicalPath = file.PhysicalPath,
                        Path = PathUtility.Combine(module.Path, name),
                        ModuleDescriptor = module
                    });
                }
            }
        }

        #endregion

        public ModuleImportsFileInfo(bool isAdmin)
        {
            IsAdmin = isAdmin;
            Name = FileName;
            PhysicalPath = Path;
            LastModified = DateTimeOffset.UtcNow;
        }

        public bool IsAdmin { get; }

        public bool Exists => true;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified { get; }

        public long Length => GetContent().Length;

        public string Name { get; }

        public string PhysicalPath { get; }

        public Stream CreateReadStream()
        {
            var content = GetContent();

            if (content.HasValue())
            {
                return new MemoryStream().WriteString(content);
            }
            else
            {
                return new MemoryStream();
            }
        }

        private string GetContent()
        {
            if (_content == null)
            {
                var imports = IsAdmin ? _adminImports : _publicImports;
                if (imports.Count == 0)
                {
                    _content = string.Empty;
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var imp in imports)
                    {
                        sb.AppendLine($"@import '{imp.Path}';");
                    }

                    _content = sb.ToString();
                }
            }

            return _content;
        }

        public Task<int> GetFileHashAsync()
        {
            return Task.FromResult((int)XxHash32.HashToUInt32(GetContent().GetBytes()));
        }

        class ModuleImport
        {
            public string Path { get; init; }
            public string PhysicalPath { get; init; }
            public IModuleDescriptor ModuleDescriptor { get; init; }

            public override string ToString()
                => Path;

            public override int GetHashCode()
                => Path.GetHashCode();

            public override bool Equals(object obj)
            {
                if (obj is not ModuleImport other)
                    return false;

                return string.Equals(this.Path, other.Path, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
