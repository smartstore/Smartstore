namespace Smartstore.Events;

/// <summary>
/// Specifies error handling behavior for a event consumer method by indicating whether errors should be logged or rethrown.
/// </summary>
/// <remarks>Apply this attribute to a consumer method to control error handling policies such as logging and exception
/// propagation. The attribute is not inherited and cannot be applied multiple times to the same method.
/// By default, all unhandled exceptions are logged and rethrown.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class HandleErrorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled. Default: true.
    /// </summary>
    public bool Log { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether exceptions should be rethrown. Default: true.
    /// </summary>
    public bool Throw { get; set; } = true;
}