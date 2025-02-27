#nullable enable

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a text suggestion model.
    /// </summary>
    public interface IAISuggestionModel
    {
        string EntityName { get; }
        string TargetProperty { get; }
        int? NumSuggestions { get; }
        int CharLimit { get; }
    }
}
