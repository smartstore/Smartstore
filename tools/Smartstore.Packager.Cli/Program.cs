#nullable enable

namespace Smartstore.Packager.Cli;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    private static Task<int> Main(string[] args)
        => new PackagerCliApplication(Console.Out, Console.Error).RunAsync(args);
}
