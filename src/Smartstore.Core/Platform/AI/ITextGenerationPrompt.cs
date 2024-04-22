namespace Smartstore.Core.Platform.AI.Chat
{
    /// <summary>
    /// Interface to be implemented by all text generation prompts.
    /// </summary>
    public interface ITextGenerationPrompt
    {
        // TODO: (mh) Move all prompt related infrastructure (and helpers) to "Prompting" or "Prompts" (?) namespace. TBD with MC.
        // TODO: (mh) Chunk/granularize this interface into smaller parts. TBD with MC.
        int? TargetEntityId { get; set; }
        string EntityName { get; set; }
        string TargetProperty { get; set; }
        bool IncludeIntro { get; set; }
        int LanguageId { get; set; }
        int WordLimit { get; set; }
        string Style { get; set; }
        string Tone { get; set; }
        string MainHeadingTag { get; set; }
        int ParagraphCount { get; set; }
        string ParagraphHeadingTag { get; set; }
        int ParagraphWordCount { get; set; }
        bool IncludeConclusion { get; set; }
        string Keywords { get; set; }
        string KeywordsToAvoid { get; set; }
        bool MakeKeywordsBold { get; set; }
        bool IncludeImages { get; set; }
        bool AddTableOfContents { get; set; }
        string TableOfContentsTitle { get; set; }
        string TableOfContentsTitleTag { get; set; }
        string AnchorText { get; set; }
        string AnchorLink { get; set; }
        bool AddCallToAction { get; set; }
        string CallToActionText { get; set; }
        string OptimizationCommand { get; set; }
        string ChangeParameter { get; set; }
        string TextToOptimize { get; set; }
    }
}