#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents an AI prompt.
    /// </summary>
    public interface IPromptContainer
    {
        /// <summary>
        /// The final prompt to be sent to the AI service.
        /// </summary>
        string? Prompt { get; }
    }

    /// <summary>
    /// Represents a composite prompt consisting of a string template and user input.
    /// </summary>
    public interface ICompositePrompt : IPromptContainer
    {
        /// <summary>
        /// The entity name/title that is used as the user input.
        /// </summary>
        string? EntityName { get; }
    }

    /// <summary>
    /// Represents a prompt for text generation.
    /// </summary>
    public interface ITextPromptContainer : ICompositePrompt
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
    /// Represents a prompt for generating suggestions.
    /// </summary>
    public interface ISuggestionPromptContainer : ICompositePrompt
    {
        int? NumSuggestions { get; }
    }
}
