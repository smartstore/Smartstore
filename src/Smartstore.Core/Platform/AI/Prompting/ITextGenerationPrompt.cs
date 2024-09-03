#nullable enable

namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Interface to be implemented by all text generation prompts.
    /// </summary>
    public interface ITextGenerationPrompt : 
        ILanguageGenerationPrompt, 
        IStructureGenerationPrompt, 
        IKeywordGenerationPrompt, 
        ITocGenerationPrompt,
        ILinkGenerationPrompt,
        IIncludeImagesGenerationPrompt
    {
        int? TargetEntityId { get; }
        string EntityName { get; }
        string TargetProperty { get; }

        int WordLimit { get; }
        int CharLimit { get; }
        bool IsSimpleText { get; }

        string Style { get; }
        string Tone { get; }
        
        string TextToOptimize { get; }
        string OptimizationCommand { get; }
        string ChangeParameter { get; }
    }
}