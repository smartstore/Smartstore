﻿namespace Smartstore.Core.Platform.AI.Prompting
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
        int? TargetEntityId { get; set; }
        string EntityName { get; set; }
        string TargetProperty { get; set; }

        string Style { get; set; }
        string Tone { get; set; }
        
        int WordLimit { get; set; }
        string OptimizationCommand { get; set; }
        string ChangeParameter { get; set; }
        string TextToOptimize { get; set; }
    }
}