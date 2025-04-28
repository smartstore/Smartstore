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
        public static string PromptResourceRoot => "Smartstore.AI.Prompts.";

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
        /// AI instruction: Do not use Markdown formatting, no backticks (```) and no indented code sections.
        /// </returns>
        public virtual string DontUseMarkdown()
            => P("DontUseMarkdown");

        /// <summary>
        /// Prevents the AI from generating markdown.
        /// </summary>
        /// <returns>
        /// AI instruction: Only return pure HTML code - do not use Markdown formatting, no backticks (```) and no indented code sections.
        /// </returns>
        public virtual string CreatHtmlWithoutMarkdown()
            => $"{CreateHtml()} - {DontUseMarkdown()}";

        /// <summary>
        /// Necessary for the AI to create the generated text as HTML.
        /// </summary>
        /// <param name="appendPeriod">Indicates whether a period should be added at the end of the sentence.</param>
        /// <returns>
        /// AI instruction: Only return pure HTML code(.)
        /// </returns>
        public virtual string CreateHtml(bool appendPeriod = false)
            => P("CreateHtml") + (appendPeriod ? "." : "");
        
        /// <summary>
        /// Instructs the AI to skip any introduction.
        /// </summary>
        /// <returns>
        /// AI instruction: Don't start your answer with meta-comments or introductions like: 'Gladly, here's your HTML'.
        /// </returns>
        public virtual string NoFriendlyIntroductions()
            => P("NoFriendlyIntroductions");

        /// <summary>
        /// Informs the AI about the current caret position
        /// </summary>
        /// <returns>
        /// AI instruction: The placeholder [CARETPOS] marks the position where your new text should appear.
        /// </returns>
        public virtual string CaretPos()
            => P("CaretPos");

        /// <summary>
        /// Instructs where to continue text when caret position is missing.
        /// </summary>
        /// <returns>
        /// AI instruction: If the placeholder [CARETPOS] is not included in the HTML, insert the new text at the end of the document.
        /// </returns>
        public virtual string MissingCaretPosHandling()
            => P("MissingCaretPosHandling");

        /// <summary>
        /// Operational rule to generate only valid HTML.
        /// </summary>
        /// <returns>
        /// AI instruction: The generated output must be completely valid HTML that fits seamlessly into the existing HTML content.
        /// </returns>
        public virtual string ValidHtmlOutput()
            => P("ValidHtmlOutput");

        /// <summary>
        /// Operational rule to remove placeholder for caret position.
        /// </summary>
        /// <returns>
        /// AI instruction: Remove the placeholder [CARETPOS] completely. It must NEVER be included in the response - neither visibly nor as a control character.
        /// </returns>
        public virtual string RemoveCaretPosPlaceholder()
            => P("RemoveCaretPosPlaceholder");

        /// <summary>
        /// Instruction to return the complete parent tag of the current caret position for block-level elements.
        /// </summary>
        /// <returns>
        /// ALWAYS return the complete enclosing block-level parent element in which the new text was inserted or changed.
        /// ONLY return this one element - no other content before or after it.
        /// </returns>
        public virtual string ReturnCompleteParentTag()
            => $"{P("ReturnCompleteParentTag")} {P("ReturnInstructionReinforcer")}";

        /// <summary>
        /// Instruction to return the complete parent tag of the current caret position for tables.
        /// </summary>
        /// <returns>
        /// ALWAYS return the complete enclosing <c>&lt;table&gt;</c> tag in which the new text was inserted or changed.
        /// ONLY return this one element - no other content before or after it.
        /// </returns>
        public virtual string ReturnCompleteTable()
            => $"{P("ReturnCompleteTable")} {P("ReturnInstructionReinforcer")}";

        /// <summary>
        /// Instruction to return the complete parent tag of the current caret position for lists.
        /// </summary>
        /// <returns>
        /// AI instruction: ALWAYS return the complete tag of the list (<c>&lt;ul&gt;</c>, <c>&lt;ol&gt;</c> or <c>&lt;menu&gt;</c>) 
        /// in which the new text was inserted or changed.
        /// ONLY return this one element - no other content before or after it.
        /// </returns>
        public virtual string ReturnCompleteList()
            => $"{P("ReturnCompleteList")} {P("ReturnInstructionReinforcer")}";

        /// <summary>
        /// Instruction to return the complete parent tag of the current caret position for definition lists.
        /// </summary>
        /// <returns>
        /// AI instruction: ALWAYS return the complete enclosing <c>&lt;dl&gt;</c> tag in which the new text was inserted or changed.
        /// ONLY return this one element - no other content before or after it.
        /// </returns>
        public virtual string ReturnCompleteDefinitionList()
            => $"{P("ReturnCompleteDefinitionList")} {P("ReturnInstructionReinforcer")}";
        
        /// <summary>
        /// Instruction to highlight any text that is added.
        /// </summary>
        /// <returns>
        /// AI instruction: Any text deviation from the transmitted original text must be enclosed with the mark tag. 
        /// When you create a new answer, take into account the text you added previously.
        /// Enclose ANY text you have added within this chat history with the mark tag.
        /// </returns>
        public virtual string PreservePreviousHighlightTags()
            => P("PreservePreviousHighlightTags");
        
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
        /// Instruction to use last span of submitted HTML structure for continue writing.
        /// </summary>
        /// <returns>
        /// AI instruction: Use last span for continue writing.
        /// </returns>
        public virtual string PreserveOriginalText()
            => P("PreserveOriginalText");

        /// <summary>
        /// Instruction to return the generated text only.
        /// </summary>
        /// <returns>
        /// AI instruction: Only return the text you have written in your answer.
        /// </returns>
        public virtual string ReturnNewTextOnly()
            => P("ReturnNewTextOnly");

        /// <summary>
        /// Instruction to add the new text for continue writing to the last span tag.
        /// </summary>
        /// <returns>
        /// AI instruction: Be sure to append the new mark-up to the last span tag. Ignore any concerns, e.g. whether it is valid HTML.
        /// </returns>
        public virtual string AppendToLastSpan()
            => P("AppendToLastSpan");

        /// <summary>
        /// Instruction to use last span of submitted HTML structure for continue writing.
        /// </summary>
        /// <returns>
        /// AI instruction: Replace the placeholder [CARETPOS] with your continued text. Leave the rest of the text unchanged.
        /// If the placeholder is in a block-level element, only add text to complete this paragraph.
        /// If the [CARETPOS] placeholder is not found, continue at the end of the text.
        /// </returns>
        public virtual string ContinueAtPlaceholder()
            => P("ContinueAtPlaceholder");

        /// <summary>
        /// Instruction to use selected tabel as HTML generation context.
        /// </summary>
        /// <returns>
        /// AI instruction: If the user requests a table extension, use [CARETPOS] exclusively to localize the table.
        /// Expand the table logically without continuing directly at the caret position - unless the user explicitly requests that the current cell be edited.
        /// </returns>
        public virtual string ContinueTable()
            => P("ContinueTable");

        /// <summary>
        /// Instruction to wrap the new text with a mark tag.
        /// </summary>
        /// <returns>
        /// AI instruction: Any text that you generate or add must be enclosed in a real HTML <c>&lt;mark&gt;</c> tag. 
        /// Example: <c>&lt;mark&gt;additional sentence&lt;/mark&gt;</c> or <c>&lt;li&gt;&lt;mark&gt;additional list item&lt;/mark&gt;&lt;/li&gt;</c>. 
        /// The word 'mark' must never appear as visible text content.
        /// </returns>
        public virtual string WrapNewContentWithHighlightTag()
            => P("WrapNewContentWithHighlightTag");

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
        /// Instructs the AI to write text that does not exceed a defined number of characters or words.
        /// </summary>
        /// <returns>
        /// AI instruction: The text may contain no more than <paramref name="charLimit"/> characters and no more than <paramref name="wordLimit"/> words.
        /// </returns>
        public virtual string CharWordLimit(int charLimit, int wordLimit)
            => P("CharWordLimit", charLimit, wordLimit);

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
        /// Necessary so that the AI does not create an entire HTML document.
        /// </summary>
        /// <returns>AI instruction: Start with a div tag.</returns>
        public virtual string StartWithDivTag()
            => P("StartWithDivTag");

        /// <summary>
        /// The title is rendered by the respective entity itself on the page.
        /// </summary>
        /// <returns>AI instruction: Do not create a heading that contains the product name.</returns>
        public virtual string DontCreateProductTitle()
            => P("DontCreateProductTitle");

        /// <summary>
        /// The title is rendered by the respective entity itself on the page.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>AI instruction: Do not create the title: '<paramref name="entityName"/>'.</returns>
        public virtual string DontCreateTitle(string entityName)
            => P("DontCreateTitle", entityName);

        /// <summary>
        /// Instruction to use a specific placeholder for image requests.
        /// </summary>
        /// <returns>
        /// If an image is to be inserted, use a <c>&lt;div class=“mb-3”&gt;</c> with an <c>&lt;i&gt;</c> tag 
        /// containing the classes 'far fa-xl fa-file-image ai-preview-file' as a placeholder.
        /// The title attribute must correspond to the associated section heading.
        /// </returns>
        public virtual string UseImagePlaceholders()
            => P("UseImagePlaceholders");
        
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
        /// <param name="longestStoreNameLenth">The length of the logest store name.</param>
        /// <returns>
        ///     When creating text for title tags, do not use the name of the website, as this will be added later - 
        ///     Reserve <paramref name="longestStoreNameLenth"/> characters for this.
        /// </returns>
        public virtual string ReserveSpaceForShopName(int longestStoreNameLenth)
            => P("ReserveSpaceForShopName", longestStoreNameLenth.ToString());

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
        {
            return paragraphCount > 1 ? P("ParagraphCount", paragraphCount) : P("OneParagraph");
        }
        
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
            => GetResource(PromptResourceRoot + keyPart, args);

        #endregion
    }
}
