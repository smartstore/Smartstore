#nullable enable

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a simple text generation model.
    /// </summary>
    public interface IAITextModel : 
        IAILanguageModel, 
        IAITextLayoutModel,
        IAIKeywordModel,
        IAITocModel,
        IAIImageContainerModel,
        IAILinkModel
    {
        int? TargetEntityId { get; }
        string EntityName { get; }
        string TargetProperty { get; }

        int? WordLimit { get; }
        int CharLimit { get; }
        bool IsSimpleText { get; }

        string Style { get; }
        string Tone { get; }
        
        string TextToOptimize { get; }
        string OptimizationCommand { get; }
        string ChangeParameter { get; }
    }
}