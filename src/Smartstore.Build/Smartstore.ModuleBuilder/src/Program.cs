using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyModel;

namespace Smartstore.ModuleBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var projectPath = string.Empty;
            var outPath = string.Empty;

            foreach (var arg in args)
            {
                var arrOption = arg.Trim().Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var argName = arrOption[0];
                var argValue = arrOption.Length > 1 ? arrOption[1] : string.Empty;

                switch (argName.ToLower())
                {
                    case "outpath":
                        outPath = argValue;
                        break;
                    case "projectpath":
                        projectPath = argValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(outPath))
            {
                return;
            }

            DeployModule(NormalizePath(projectPath), NormalizePath(outPath));

            static string NormalizePath(string path)
            {
                return string.IsNullOrEmpty(path) ? path : Path.GetFullPath(path.Trim('\'', '"').Replace('\\', Path.DirectorySeparatorChar));
            }
        }

        static void DeployModule(string projectPath, string outPath)
        {
            var outDir = new DirectoryInfo(outPath);
            if (!outDir.Exists)
            {
                Console.WriteLine($"---- ERR: Module output directory {outDir.FullName} does not exist.");
                return;
            }

            var projectDir = string.IsNullOrEmpty(projectPath) ? null : new DirectoryInfo(projectPath);
            if (projectDir != null && !projectDir.Exists)
            {
                Console.WriteLine($"---- ERR: Module project directory {projectDir.FullName} does not exist.");
            }

            var isDataProvider = projectDir != null && projectDir.Exists && IsDataProviderDir(projectDir);
            var descriptorDir = isDataProvider ? projectDir : outDir;

            var module = ReadModuleDescriptor(descriptorDir);
            if (module == null)
            {
                Console.WriteLine($"---- ERR: module.json does not exist in {descriptorDir.FullName}.");
                return;
            }
            else
            {
                module.OutDir = outDir;
            }

            var moduleName = module.SystemName;
            Console.WriteLine($"DeployModule: {module.SystemName}, Path: {outDir.FullName}");

            var moduleContext = ReadDependencyContext(module);
            if (moduleContext == null)
            {
                return;
            }
            else
            {
                module.DependencyContext = moduleContext;
            }

            var privateLibs = module.PrivateReferences;
            if (privateLibs == null)
            {
                return;
            }

            foreach (var privateLib in privateLibs)
            {
                var lib = moduleContext.CompileLibraries.FirstOrDefault(x => x.Name == privateLib);
                if (lib == null)
                {
                    Console.WriteLine($"---- Private reference {privateLib} does not exist.");
                }
                else
                {
                    var paths = lib.ResolveReferencePaths();
                    if (paths != null)
                    {
                        foreach (var path in paths)
                        {
                            var sourceFile = new FileInfo(path);
                            var targetFile = new FileInfo(Path.Combine(outPath, Path.GetFileName(path)));

                            if (!targetFile.Exists || sourceFile.Length != targetFile.Length || sourceFile.LastWriteTimeUtc != targetFile.LastWriteTimeUtc)
                            {
                                File.Copy(sourceFile.FullName, targetFile.FullName, true);
                                Console.WriteLine($"---- Copied private reference {privateLib} to {targetFile.FullName}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"---- ERR: Private reference {privateLib} cannot be resolved.");
                    }
                }
            }

            if (!isDataProvider)
            {
                DeleteJunk(outDir);
            }
        }

        static ModuleDescriptor ReadModuleDescriptor(DirectoryInfo moduleDir)
        {
            var descriptorFilePath = Path.Combine(moduleDir.FullName, "module.json");
            if (!File.Exists(descriptorFilePath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ModuleDescriptor>(File.ReadAllText(descriptorFilePath));
        }

        static DependencyContext ReadDependencyContext(ModuleDescriptor module)
        {
            var depsFilePath = Path.Combine(module.OutDir.FullName, $"{module.SystemName}.deps.json");
            if (!File.Exists(depsFilePath))
            {
                return null;
            }

            var reader = new DependencyContextJsonReader();
            using var file = File.OpenRead(depsFilePath);
            return reader.Read(file);
        }

        static void DeleteJunk(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                return;
            }

            var entries = dir.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
            foreach (var entry in entries)
            {
                if (entry is DirectoryInfo di && (/*entry.Name == "ref" ||*/ entry.Name == "refs"))
                {
                    di.Delete(true);
                }

                if (entry is FileInfo fi)
                {
                    if (entry.Name.StartsWith("Smartstore.Data.")
                        || entry.Name.EndsWith(".StaticWebAssets.xml", StringComparison.OrdinalIgnoreCase)
                        || entry.Name.EndsWith(".staticwebassets.runtime.json", StringComparison.OrdinalIgnoreCase))
                    {
                        fi.Delete();
                    }
                }
            }
        }

        //static string[] GetLibNames(DependencyContext context)
        //{
        //    return context.CompileLibraries.Where(x => x.Type == "package").Select(x => x.Name).ToArray();
        //}

        static bool IsDataProviderDir(DirectoryInfo dir)
        {
            return new[] { "Smartstore.Data.SqlServer", "Smartstore.Data.MySql", "Smartstore.Data.PostgreSql" }.Contains(dir.Name);
        }

        class ModuleDescriptor
        {
            public string SystemName { get; set; }
            public string[] PrivateReferences { get; set; }

            [IgnoreDataMember]
            public DirectoryInfo OutDir { get; set; }

            [IgnoreDataMember]
            public DependencyContext DependencyContext { get; set; }
        }
    }
}
