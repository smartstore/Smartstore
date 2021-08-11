using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace DeployModule
{
    class Program
    {
        static void Main(string[] args)
        {   
            var appPath = string.Empty;
            var modulePaths = string.Empty;
            var options = args[0].Trim().Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var option in options)
            {
                var arrOption = option.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var argName = arrOption[0];
                var argValue = arrOption.Length > 1 ? arrOption[1] : string.Empty;

                switch (argName)
                {
                    case "OutputPath":
                        appPath = Path.GetFullPath(argValue);
                        break;
                    case "ModulePath":
                        modulePaths = argValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(modulePaths) || !Directory.Exists(appPath))
            {
                return;
            }

            DeployModules(appPath, modulePaths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        static void DeployModules(string appPath, string[] modulePaths)
        {
            var entryDll = new FileInfo(Path.Combine(appPath, "Smartstore.Web.dll"));
            if (!entryDll.Exists)
            {
                return;
            }

            var appContext = DependencyContext.Load(Assembly.LoadFrom(Path.Combine(appPath, "Smartstore.Web.dll")));
            var appLibs = GetLibNames(appContext);

            foreach (var path in modulePaths)
            {
                var fullModulePath = Path.GetFullPath(path).Trim('"');
                DeployModule(appLibs, fullModulePath);
                DeleteRefs(fullModulePath);
            }
        }

        static void DeployModule(string[] appLibs, string modulePath)
        {
            var assembly = LoadModuleAssembly(modulePath);

            if (assembly != null)
            {
                var moduleContext = DependencyContext.Load(assembly);
                var moduleLibs = GetLibNames(moduleContext);
                var privateLibs = moduleLibs.Except(appLibs).ToArray();

                foreach (var privateLib in privateLibs)
                {
                    var lib = moduleContext.CompileLibraries.FirstOrDefault(x => x.Name == privateLib);
                    if (lib != null)
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
                                    Console.WriteLine($"DeployModule: copied private reference {privateLib} to {targetFile.FullName}");
                                }
                            }
                        }
                    }
                }
            }
        }

        static void DeleteRefs(string modulePath)
        {
            Directory.Delete(Path.Combine(modulePath, "ref"), true);
            Directory.Delete(Path.Combine(modulePath, "refs"), true);
        }

        static string[] GetLibNames(DependencyContext context)
        {
            return context.CompileLibraries.Where(x => x.Type == "package").Select(x => x.Name).ToArray();
        }

        static Assembly LoadModuleAssembly(string path)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                return null;
            }

            var fi = di.GetFiles("*.deps.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!fi.Exists)
            {
                return null;
            }

            return Assembly.LoadFrom(fi.FullName.Replace(".deps.json", ".dll"));
        }
    }
}
