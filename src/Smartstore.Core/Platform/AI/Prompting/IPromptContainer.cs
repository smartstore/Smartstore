#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Model interface for prompt UI.
    /// </summary>
    public interface IPromptContainer
    {
        string? EntityName { get; }
        string? Prompt { get; }
    }

    /// <summary>
    /// Model interface for the text generation prompt UI.
    /// </summary>
    public interface ITextPromptContainer : IPromptContainer
    {
        int? WordLimit { get; }
        string? Style { get; }
        string? Tone { get; }

        bool DisplayWordLimit { get; set; }
        bool DisplayStyle { get; set; }
        bool DisplayTone { get; set; }

        SelectList? AvailableStyles { get; }
        SelectList? AvailableTones { get; }
    }

    /// <summary>
    /// Model interface for the suggestion generation prompt UI.
    /// </summary>
    public interface ISuggestionPromptContainer : IPromptContainer
    {
        int? NumSuggestions { get; }
    }
}
