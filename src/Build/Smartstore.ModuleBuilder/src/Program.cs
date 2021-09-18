using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyModel;

namespace Smartstore.ModuleBuilder
{
    class Program
    {
        static void Main(string[] args)
        {   
            var modulePaths = string.Empty;
            var options = args[0].Trim().Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var option in options)
            {
                var arrOption = option.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var argName = arrOption[0];
                var argValue = arrOption.Length > 1 ? arrOption[1] : string.Empty;

                switch (argName)
                {
                    case "ModulePath":
                        modulePaths = argValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(modulePaths))
            {
                return;
            }

            DeployModules(modulePaths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        static void DeployModules(string[] modulePaths)
        {
            foreach (var path in modulePaths)
            {
                var fullModulePath = Path.GetFullPath(path).Trim('"');
                var moduleName = Path.GetFileName(fullModulePath);

                Console.WriteLine($"DeployModule: {moduleName}");

                DeployModule(fullModulePath);
                DeleteJunk(fullModulePath);
            }
        }

        static void DeployModule(string modulePath)
        {
            var moduleName = Path.GetFileName(modulePath);
            var moduleContext = ReadDependencyContext(Path.Combine(modulePath, $"{moduleName}.deps.json"));
            if (moduleContext == null)
            {
                return;
            }

            var moduleDescriptor = ReadModuleDescriptor(Path.Combine(modulePath, $"module.json"));
            if (moduleDescriptor == null)
            {
                return;
            }

            var privateLibs = moduleDescriptor.PrivateReferences;
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
                            var targetFile = new FileInfo(Path.Combine(modulePath, Path.GetFileName(path)));

                            if (!targetFile.Exists || sourceFile.Length != targetFile.Length || sourceFile.LastWriteTimeUtc != targetFile.LastWriteTimeUtc)
                            {
                                File.Copy(sourceFile.FullName, targetFile.FullName);
                                Console.WriteLine($"---- Copied private reference {privateLib} to {targetFile.FullName}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"---- Private reference {privateLib} cannot be resolved.");
                    }
                }
            }
        }

        static ModuleDescriptor ReadModuleDescriptor(string manifestFilePath)
        {
            if (!File.Exists(manifestFilePath))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ModuleDescriptor>(File.ReadAllText(manifestFilePath));
        }

        static DependencyContext ReadDependencyContext(string depsFilePath)
        {
            if (!File.Exists(depsFilePath))
            {
                return null;
            }
            
            var reader = new DependencyContextJsonReader();
            using (var file = File.OpenRead(depsFilePath))
            {
                return reader.Read(file);
            }
        }

        static void DeleteJunk(string modulePath)
        {
            var dir = new DirectoryInfo(modulePath);
            if (!dir.Exists)
            {
                return;
            }

            var entries = dir.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
            foreach (var entry in entries)
            {
                if (entry is DirectoryInfo di && (entry.Name == "ref" || entry.Name == "refs"))
                {
                    di.Delete(true);
                }

                if (entry is FileInfo fi)
                {
                    if (entry.Name.StartsWith("Smartstore.Data.") || entry.Name.EndsWith(".StaticWebAssets.xml"))
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

        class ModuleDescriptor
        {
            public string SystemName { get; set; }
            public string[] PrivateReferences { get; set; }
        }
    }
}
