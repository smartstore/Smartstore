namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An action result that wraps another result.
    /// </summary>
    public interface IActionResultContainer : IActionResult
    {
        /// <summary>
        /// The wrapped action result.
        /// </summary>
        IActionResult InnerResult { get; }
    }
}
