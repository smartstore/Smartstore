namespace Smartstore.Core.Seo.Routing;

/// <summary>
/// Disallows robots access to the route. Also used to dynamically populate the robots.txt file.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisallowRobotAttribute : Attribute
{
    public DisallowRobotAttribute()
    {
    }

    public DisallowRobotAttribute(string path)
    {
        Guard.NotEmpty(path);
        Path = path;
    }

    public string Path { get; }
}
