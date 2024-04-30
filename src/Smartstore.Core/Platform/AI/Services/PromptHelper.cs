using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Platform.AI;
using Smartstore.Core.Platform.AI.Chat;

namespace Smartstore.AI.Services
{
    public partial class PromptHelper
    {
        // TODO: (mh) Don't concat strings in all the methods here. Use the "Apply" method pattern: void methods that add new prompt parts to a passed list. TBD with MC.
        private readonly SmartDbContext _db;
        private readonly ILinkResolver _linkResolver;

        public PromptHelper(SmartDbContext db, ILinkResolver linkResolver)
        {
            _db = db;
            _linkResolver = linkResolver;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        // TODO: (mh) Rename --> GenerateSimpleTextPrompt etc.
        /// <summary>
        /// Enhances prompt with general instructions for simple text creation. 
        /// Wordlimit, Tone and Style are the only properties that are considered.
        /// </summary>
        /// <param name="model">The <see cref="ITextGenerationPrompt"/> model</param>
        /// <returns>The generated prompt part.</returns>
        public string GetPromptForSimpleTextCreation(ITextGenerationPrompt model)
        {
            var prompt = string.Empty;

            // Append phrase for wordcount from model.
            if (model.WordLimit > 0)
            {
                prompt += P("WordLimit", args: [model.WordLimit]);
            }

            // Append phrase for tone from model.
            if (model.Tone.HasValue())
            {
                prompt += P("LanguageTone", args: [model.Tone]);
            }

            // Append phrase for style from model.
            if (model.Style.HasValue())
            {
                prompt += P("LanguageStyle", args: [model.Style]);
            }

            return prompt;
        }

        /// <summary>
        /// Enhances prompt with general instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="ITextGenerationPrompt"/> model</param>
        /// <returns>The generated prompt part.</returns>
        public async Task<string> GetPromptForRichTextCreationAsync(ITextGenerationPrompt model)
        {
            var prompt = string.Empty;

            #region General instructions

            // Necessary for the AI to create the generated text as HTML.
            prompt += P("CreateHtml");

            // No introductions or explanations please.
            prompt += P("JustHtml");

            // Necessary so that the AI does not create an entire HTML document.
            prompt += P("StartWithDivTag");

            // The title is rendered by the respective entity itself on the page.
            prompt += P("DontCreateTitle", args: [model.EntityName]);  

            #endregion

            #region Language and style

            // Append phrase for language from model
            if (model.LanguageId > 0)
            {
                var language = await _db.Languages.FindByIdAsync(model.LanguageId);
                prompt += P("Language", args: [language.Name.ToLower()]);
            }

            // Append phrase for tone from model
            if (model.Tone.HasValue())
            {
                prompt += P("LanguageTone", args: [model.Tone]);
            }

            // Append phrase for style from model
            if (model.Style.HasValue())
            {
                prompt += P("LanguageStyle", args: [model.Style]);
            }

            #endregion

            #region Structure

            if (model.IncludeIntro)
            {
                prompt += P("IncludeIntro");
            }

            if (model.MainHeadingTag.HasValue())
            {
                prompt += P("MainHeadingTag", args: [model.MainHeadingTag]);
            }

            if (model.ParagraphCount > 0)
            {
                prompt += P("ParagraphCount", args: [model.ParagraphCount]);

                if (model.ParagraphWordCount > 0)
                {
                    prompt += P("ParagraphWordCount", args: [model.ParagraphWordCount]);
                }

                // INFO: Necessary so that the AI does not take any shortcuts. Sometimes, for example, it simply writes TBD or ... (More text).
                prompt += P("WriteCompleteParagraphs");
            }

            if (model.ParagraphHeadingTag.HasValue())
            {
                prompt += P("ParagraphHeadingTag", args: [model.ParagraphHeadingTag]);
            }

            if (model.IncludeConclusion)
            {
                prompt += P("IncludeConclusion");
            }

            #endregion

            #region Keywords

            if (model.Keywords.HasValue())
            {
                prompt += P("UseKeywords", args: [model.KeywordsToAvoid]);
                if (model.MakeKeywordsBold)
                {
                    prompt += P("MakeKeywordsBold");
                }
            }

            if (model.KeywordsToAvoid.HasValue())
            {
                prompt += P("KeywordsToAvoid", args: [model.KeywordsToAvoid]);
            }

            #endregion

            #region Images

            if (model.IncludeImages)
            {
                prompt += P("IncludeImages");

                if (model.IncludeIntro)
                {
                    prompt += P("NoIntroImage");
                }

                if ( model.IncludeConclusion)
                {
                    prompt += P("NoConclusionImage");
                }
            }

            #endregion

            #region Additional content
            
            if (model.AddTableOfContents)
            {
                prompt += P("AddToc", args: [model.TableOfContentsTitle, model.TableOfContentsTitleTag]);
            }

            #endregion

            #region Links

            if (model.AnchorLink.HasValue())
            {
                // Get the correct link from model.AnchorLink
                var linkResolutionResult = await _linkResolver.ResolveAsync(model.AnchorLink);
                var link = linkResolutionResult?.Link;

                if (link.HasValue())
                {
                    if (model.AnchorText.HasValue())
                    {
                        prompt += P("AddNamedLink", args: [model.AnchorText, link]);
                    }
                    else
                    {
                        prompt += P("AddLink", args: [link]);
                    }

                    if (model.AddCallToAction && model.CallToActionText.HasValue())
                    {
                        prompt += P("AddCallToAction", args: [model.CallToActionText, link]);
                    }
                }
            }

            #endregion

            return prompt;
        }


        /// <summary>
        /// Creates a prompt to generate pictures based on the given topic.
        /// </summary>
        /// <param name="topic">The topic for which to create the picture.</param>
        /// <returns>The generated prompt part.</returns>
        public string GetGenericCreatePicturePrompt(string topic)
        {
            var prompt = string.Empty;

            prompt += P("CreatePicture", args:[topic]);

            return prompt;
        }

        /// <summary>
        /// Enhances prompt with general instructions for image creation.
        /// </summary>
        /// <param name="model">The <see cref="IImageGenerationPrompt"/> model</param>
        /// <returns>The generated prompt part.</returns>
        public static string GetPromptForImageCreation(IImageGenerationPrompt model)
        {
            var prompt = string.Empty;

            if (model.Medium.HasValue())
            {
                prompt += model.Medium + ", ";
            }

            if (model.Environment.HasValue())
            {
                prompt += model.Environment + ", ";
            }

            if (model.Lighting.HasValue())
            {
                prompt += model.Lighting + ", ";
            }

            if (model.Color.HasValue())
            {
                prompt += model.Color + ", ";
            }

            if (model.Mood.HasValue())
            {
                prompt += model.Mood + ", ";
            }

            if (model.Composition.HasValue())
            {
                prompt += model.Composition;
            }

            return prompt;
        }

        /// <summary>
        /// Creates a prompt for the meta title.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public string GenerateMetaTitle(string forPromptPart)
        {
            var prompt = string.Empty;

            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            prompt += GetRolePromptPart(AIRole.SEOExpert);

            prompt += forPromptPart;

            // TODO: Längsten Shopnamen ermitteln und Zeichenlänge in die Anweisung einfügen.
            // INFO: Der Name des Shops wird von Smartstore automatisch dem Title zugefügt. 
            // TODO: Ausfürlich mit allen Entitäten testen.
            // Das Original mit dem auf der Produktdetailseite getestet wurde war:
            //forPromptPart += " Verwende dabei nicht den Namen des Shops. Der wird von der Webseite automatisch zugefügt. Reserviere dafür 5 Worte.";
            prompt += P("ReserveSpaceForShopName");

            // INFO: Smartstore automatically adds inverted commas to the title.
            prompt += GetDontUseQuotesPromptPart();

            return prompt;
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public string GenerateMetaDescription(string forPromptPart)
        {
            var prompt = string.Empty;

            // INFO: No need for word limit in SEO properties. Because we advised the AI to be a SEO expert, it already knows the correct limits.
            prompt += GetRolePromptPart(AIRole.SEOExpert);
            prompt += forPromptPart;
            prompt += GetDontUseQuotesPromptPart();

            return prompt;
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public string GenerateMetaKeywords(string forPromptPart)
        {
            var prompt = string.Empty;

            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            prompt += GetRolePromptPart(AIRole.SEOExpert);
            prompt += forPromptPart;
            prompt += P("SeparateListWithComma");

            // Respect Smartstore database scheme limitation. Limit is 400.
            prompt += P("CharLimit", args: [400]);

            return prompt;
        }

        #region Helper methods

        public string GetRolePromptPart(AIRole role, string entityName = "")
        {
            var prompt = P("RolePrefix"); 

            switch (role)
            {   
                case AIRole.Translator:
                    prompt += P("Role.Translator");
                    break;
                case AIRole.Copywriter:
                    prompt += P("Role.Copywriter");
                    break;
                case AIRole.Marketer:
                    prompt += P("Role.Marketer");
                    break;
                case AIRole.SEOExpert:
                    prompt += P("Role.SEOExpert");
                    break;
                case AIRole.Blogger:
                    prompt += P("Role.Blogger");
                    break;
                case AIRole.Journalist:
                    prompt += P("Role.Journalist");
                    break;
                case AIRole.SalesPerson:
                    prompt += P("Role.SalesPerson");
                    break;
                case AIRole.ProductExpert:
                    prompt += P("Role.ProductExpert", args: [entityName]);
                    break;
                default:
                    break;
            }

            return prompt;
        }

        public string GetInternalSuggestionPromptPart()
        {
            var prompt = string.Empty;

            prompt += GetDontUseQuotesPromptPart();
            prompt += P("SeparateWithNumberSign");
            prompt += P("DontNumberSuggestions");

            return prompt;
        }

        public string GetDontUseQuotesPromptPart()
        {
            var prompt = string.Empty;

            prompt += P("DontUseQuotes");

            return prompt;
        }

        /// <summary>
        /// Shortcut to get a resource string without NewLine breaks.
        /// </summary>
        /// <param name="resourceId">The id of the resource.</param>
        /// <returns>The resource without NewLine breaks.</returns>
        public string P(string resourceId, bool enhanceResourceId = true, params object[] args)
        {
            if (enhanceResourceId)
            {
                // TODO: (mh) Make const for "Smartstore.AI.Prompts."
                resourceId = "Smartstore.AI.Prompts." + resourceId;
            }

            // TODO: (mh) Be careful. Removing NewLine could collapse text! Don't do this.
            return T(resourceId, args).ToString().Replace(Environment.NewLine, string.Empty);
        }

        #endregion
    }
}
