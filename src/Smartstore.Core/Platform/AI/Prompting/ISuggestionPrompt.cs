#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Interface to be implemented by all suggestion generation prompts.
    /// </summary>
    public interface ISuggestionPrompt
    {
        string TargetProperty { get; }
        string Input { get; }
        int NumberOfSuggestions { get; }
        int CharLimit { get; }
    }
}
