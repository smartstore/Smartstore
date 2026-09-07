namespace Smartstore.Packager.Cli;

/// <summary>
/// Process exit codes returned by the packager CLI.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Every requested package was created.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// A runtime, discovery or packaging error occurred.
    /// </summary>
    public const int Error = 1;

    /// <summary>
    /// The command line was invalid.
    /// </summary>
    public const int InvalidCommandLine = 2;
}
