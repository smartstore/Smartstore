using System.Diagnostics;
using System.Runtime.CompilerServices;
using Smartstore.Core.Localization;

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Contains methods to obtain string resources for AI prompts.
    /// </summary>
    public partial class AIMessageResources(ILocalizationService localizationService)
    {
        private readonly ILocalizationService _localizationService = localizationService;

        /// <summary>
        /// Prevents the AI from generating quotation marks.
        /// </summary>
        /// <returns>
        /// AI instruction: Do not enclose the text in quotation marks.
        /// </returns>
        public virtual string DontUseQuotes()
            => P("DontUseQuotes");

        /// <summary>
        /// Prevents the AI from generating markdown.
        /// </summary>
        /// <returns>
        /// AI instruction: Do not use markdown formatting.
        /// </returns>
        public virtual string DontUseMarkdown()
            => P("DontUseMarkdown");

        /// <summary>
        /// Prevents the AI from generating line breaks.
        /// </summary>
        /// <returns>
        /// AI instruction: Do not use line breaks.
        /// </returns>
        public virtual string DontUseLineBreaks()
            => P("DontUseLineBreaks");

        /// <summary>
        /// Instruction to preserver HTML structure.
        /// </summary>
        /// <returns>
        /// AI instruction: Be sure to preserve the HTML structure.
        /// </returns>
        public virtual string PreserveHtmlStructure()
            => P("PreserveHtmlStructure");

        /// <summary>
        /// Instruction to process HTML elements individually.
        /// </summary>
        /// <returns>
        /// AI instruction: Process each HTML element individually.
        /// </returns>
        public virtual string ProcessHtmlElementsIndividually()
            => P("ProcessHtmlElementsIndividually");

        /// <summary>
        /// Instructs the AI to write text that does not exceed a defined number of words.
        /// </summary>
        /// <returns>
        /// AI instruction: The text may contain a maximum of <paramref name="wordLimit"/> words.
        /// </returns>
        public virtual string WordLimit(int wordLimit)
            => P("WordLimit", wordLimit);

        /// <summary>
        /// Instructs the AI to write text that does not exceed a defined number of characters.
        /// </summary>
        /// <returns>
        /// AI instruction: Limit your answer to <paramref name="charLimit"/> characters!
        /// </returns>
        public virtual string CharLimit(int charLimit)
            => P("CharLimit", charLimit);

        /// <summary>
        /// Instructs the AI to write in a specific tone.
        /// </summary>
        /// <returns>
        /// AI instruction: The tone should be <paramref name="tone"/>.
        /// </returns>
        public virtual string LanguageTone(string tone)
            => P("LanguageTone", tone);

        /// <summary>
        /// Instructs the AI to write in a specific style.
        /// </summary>
        /// <returns>
        /// AI instruction: The language style should be <paramref name="style"/>.
        /// </returns>
        public virtual string LanguageStyle(string style)
            => P("LanguageStyle", style);

        /// <summary>
        /// Necessary for the AI to create the generated text as HTML.
        /// </summary>
        /// <returns>
        /// AI instruction: Create HTML text.
        /// </returns>
        public virtual string CreateHtml()
            => P("CreateHtml");

        /// <summary>
        /// No introductions or explanations please.
        /// </summary>
        /// <returns>
        /// AI instruction: Just return the HTML you have created so that it can be integrated directly into a website. 
        /// Don't give explanations about what you have created or introductions like: 'Gladly, here is your HTML'. 
        /// Do not include the generated HTML in any delimiters like: '```html' 
        /// </returns>
        public virtual string JustHtml()
            => P("JustHtml");

        /// <summary>
        /// Necessary so that the AI does not create an entire HTML document.
        /// </summary>
        /// <returns>AI instruction: Start with a div tag.</returns>
        public virtual string StartWithDivTag()
            => P("StartWithDivTag");

        /// <summary>
        /// The title is rendered by the respective entity itself on the page.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>AI instruction: Do not create the title: '<paramref name="entityName"/>'.</returns>
        public virtual string DontCreateTitle(string entityName)
            => P("DontCreateTitle", entityName);

        /// <summary>
        /// Instruction to write in a specific language.
        /// </summary>
        /// <param name="languageName">The name of the language.</param>
        /// <returns>AI instruction: Write in <paramref name="languageName"/>.</returns>
        public virtual string Language(string languageName)
            => P("Language", languageName);

        /// <summary>
        /// Instruction to separate the generated list with commas.
        /// </summary>
        /// <returns>AI instruction: The list should be comma-separated so that it can be inserted directly as a meta tag.</returns>
        public virtual string SeparateListWithComma()
            => P("SeparateListWithComma");

        /// <summary>
        /// Instruction to reserve space for the name of the website.
        /// </summary>
        /// <returns>AI instruction: Do not use the name of the website as this will be added later. Reserve 5 words for this..</returns>
        public virtual string ReserveSpaceForShopName()
            => P("ReserveSpaceForShopName");

        #region HTML Structure instructions

        /// <summary>
        /// Instruction to include an intro for the HTML about to be generated.
        /// </summary>
        /// <returns>AI instruction: Start with an introduction.</returns>
        public virtual string IncludeIntro()
            => P("IncludeIntro");

        /// <summary>
        /// Instruction to use a specific tag for the main heading.
        /// </summary>
        /// <param name="tag">The type of the HTML tag.</param>
        /// <returns>AI instruction: The main heading is given a <paramref name="tag"/> tag.</returns>
        public virtual string MainHeadingTag(string tag)
            => P("MainHeadingTag", tag);

        /// <summary>
        /// Defines the number of paragraphs the text should be divided into.
        /// </summary>
        /// <param name="paragraphCount">The count of paragraphs.</param>
        /// <returns>AI instruction: The text should be divided into <paramref name="paragraphCount"/> paragraphs, which are enclosed with p tags.</returns>
        public virtual string ParagraphCount(int paragraphCount)
            => P("ParagraphCount", paragraphCount);

        /// <summary>
        /// Defines the number of words for each paragraph.
        /// </summary>
        /// <param name="paragraphWordCount">The count of words per paragraph.</param>
        /// <returns>AI instruction: Each section should contain a maximum of <paramref name="paragraphWordCount"/> words.</returns>
        public virtual string ParagraphWordCount(int paragraphWordCount)
            => P("ParagraphWordCount", paragraphWordCount);

        /// <summary>
        /// Necessary so that the AI does not take any shortcuts. Sometimes, for example, it simply writes TBD or ... (More text).
        /// </summary>
        /// <returns>AI instruction: Write complete texts for each section.</returns>
        public virtual string WriteCompleteParagraphs()
            => P("WriteCompleteParagraphs");

        /// <summary>
        /// Instruction to use a specific tag for the paragraph headings.
        /// </summary>
        /// <param name="tag">The type of the HTML tag.</param>
        /// <returns>AI instruction: The headings of the individual sections are given <paramref name="tag"/> tags.</returns>
        public virtual string ParagraphHeadingTag(string tag)
            => P("ParagraphHeadingTag", tag);

        /// <summary>
        /// Instruction to include an conclusion for the HTML about to be generated.
        /// </summary>
        /// <returns>AI instruction: End the text with a conclusion.</returns>
        public virtual string IncludeConclusion()
            => P("IncludeConclusion");

        #endregion

        #region Keywords

        /// <summary>
        /// Instruction to include specific keywords in the text.
        /// </summary>
        /// <param name="keywords">The keywords to use.</param>
        /// <returns>AI instruction: Use the following keywords: '<paramref name="keywords"/>'.</returns>
        public virtual string UseKeywords(string keywords)
            => P("UseKeywords", keywords);

        /// <summary>
        /// Instruction to make keywords bold in the text.
        /// </summary>
        /// <returns>AI instruction: Include the keywords in b-tags.</returns>
        public virtual string MakeKeywordsBold()
            => P("MakeKeywordsBold");

        /// <summary>
        /// Instruction to avoid specific keywords in the text.
        /// </summary>
        /// <param name="keywordsToAvoid">The keywords to avoid.</param>
        /// <returns>AI instruction: Do not use the following keywords under any circumstances: '<paramref name="keywordsToAvoid"/>'.</returns>
        public virtual string KeywordsToAvoid(string keywordsToAvoid)
            => P("KeywordsToAvoid", keywordsToAvoid);

        #endregion

        #region Images

        /// <summary>
        /// Instruction to include image placeholders in the text.
        /// </summary>
        /// <returns>AI instruction: After each paragraph, add another div-tag with the CSS class 'mb-3', 
        /// which contains an i-tag with the classes 'far fa-xl fa-file-image ai-preview-file'.
        /// The title attribute of the i-tag should be the heading of the respective paragraph.</returns>
        public virtual string IncludeImages()
            => P("IncludeImages");

        /// <summary>
        /// Instruction to include no image for the intro text.
        /// </summary>
        /// <returns>AI instruction: The intro does not receive a picture.</returns>
        public virtual string NoIntroImage()
            => P("NoIntroImage");

        /// <summary>
        /// Instruction to include no image for the conclusion text.
        /// </summary>
        /// <returns>AI instruction: The conclusion does not receive a picture.</returns>
        public virtual string NoConclusionImage()
            => P("NoConclusionImage");

        /// <summary>
        /// Should prevent the AI from using text when creating images. (Doesn't not work with dall-e-3. But it must work sooner or later)
        /// </summary>
        /// <returns>
        /// AI instruction: Do not use any text or characters in the images to be created. The image should be purely visual, without any writing or labelling.
        /// </returns>
        public virtual string DontUseTextInImages()
            => P("DontUseTextInImages");

        #endregion

        /// <summary>
        /// Instruction to add a table of contents to the text.
        /// </summary>
        /// <param name="tableOfContentsTitle">The title of the table of contents.</param>
        /// <param name="tableOfContentsTitleTag">The tag of title of the table of contents.</param>
        /// <returns>AI instruction: Insert a table of contents with the title '<paramref name="tableOfContentsTitle"/>'.
        /// The title receives a <paramref name="tableOfContentsTitleTag"/> tag.
        /// Link the individual points of the table of contents to the respective headings of the paragraphs.</returns>
        public virtual string AddTableOfContents(string tableOfContentsTitle, string tableOfContentsTitleTag)
            => P("AddToc", tableOfContentsTitle, tableOfContentsTitleTag);

        #region Links

        /// <summary>
        /// Instruction to include a link to the text.
        /// </summary>
        /// <param name="anchorText">The title of link.</param>
        /// <param name="link">The URL.</param>
        /// <returns>AI instruction: Insert a link with the text '<paramref name="anchorText"/>' that refers to '<paramref name="link"/>'.</returns>
        public virtual string AddNamedLink(string anchorText, string link)
            => P("AddNamedLink", anchorText, link);

        /// <summary>
        /// Instruction to include a link to the text.
        /// </summary>
        /// <param name="link">The URL.</param>
        /// <returns>AI instruction: Insert a link that refers to '<paramref name="link"/>'.</returns>
        public virtual string AddLink(string link)
            => P("AddLink", link);

        /// <summary>
        /// Instruction to include a call to action button after the text.
        /// </summary>
        /// <param name="callToActionText">The title of link (or button).</param>
        /// <param name="link">The URL.</param>
        /// <returns>AI instruction: Insert a link with the text '<paramref name="callToActionText"/>' that refers to '<paramref name="link"/>'.</returns>
        public virtual string AddCallToAction(string callToActionText, string link)
            => P("AddCallToAction", callToActionText, link);

        #endregion

        /// <summary>
        /// Adds an instruction for the AI to act in a specific role.
        /// </summary>
        /// <param name="role">The <see cref="AIRole"/></param>
        /// <param name="entityName">
        /// The name of the entity. Currently only used to fill a placeholder for the product name 
        /// when the role is <see cref="AIRole.ProductExpert"/>.</param>
        /// <returns>AI Instruction: e.g.: Be a SEO expert.</returns>
        public virtual string Role(AIRole role, string entityName = "")
            => P($"Role.{Enum.GetName(typeof(AIRole), role)}", entityName);

        #region Utilities

        /// <summary>
        /// Gets a resource string value for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="args">The arguments to format the resource string.</param>
        public virtual string GetResource(string key, params object[] args)
        {
            var localizedValue = _localizationService.GetResource(key, returnEmptyIfNotFound: true)
                .EmptyNull()
                .Trim();

            if (!args.IsNullOrEmpty())
            {
                return localizedValue.FormatInvariant(args);
            }

            return localizedValue;
        }

        /// <summary>
        /// Shortcut to get a resource string value of a prompt.
        /// The key will be "Smartstore.AI.Prompts.&lt;<paramref name="keyPart"/>&gt;".
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string P(string keyPart, params object[] args)
            => GetResource("Smartstore.AI.Prompts." + keyPart, args);

        #endregion
    }
}
