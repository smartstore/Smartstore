using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Platform.AI.Prompting
{
    public partial class PromptBuilder
    {
        private readonly SmartDbContext _db;
        private readonly ILinkResolver _linkResolver;

        public PromptBuilder(
            SmartDbContext db,
            ILinkResolver linkResolver,
            ILocalizationService localizationService,
            PromptResources promptResources)
        {
            _db = db;
            _linkResolver = linkResolver;
            Localization = localizationService;
            Resources = promptResources;
        }

        public PromptResources Resources { get; }
        public ILocalizationService Localization { get; }
        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Adds prompt parts with general instructions for simple text creation. 
        /// Wordlimit, Tone and Style are the only properties that are considered.
        /// </summary>
        /// <param name="model">The <see cref="ITextGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildSimpleTextPrompt(ITextGenerationPrompt model, List<string> promptParts)
        {
            // Append phrase for wordcount from model.
            if (model.WordLimit > 0)
            {
                promptParts.Add(Resources.WordLimit(model.WordLimit));
            }

            // Append phrase for tone from model.
            if (model.Tone.HasValue())
            {
                promptParts.Add(Resources.LanguageTone(model.Tone));
            }

            // Append phrase for style from model.
            if (model.Style.HasValue())
            {
                promptParts.Add(Resources.LanguageStyle(model.Style));
            }
        }

        /// <summary>
        /// Adds prompt parts with general instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="ITextGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual async Task BuildRichTextPromptAsync(ITextGenerationPrompt model, List<string> promptParts)
        {
            // TODO: (mh) (ai) Does it make sense to have own methods for every single part?
            // So it can be overwritten granularly.

            // General instructions
            promptParts.AddRange(
            [
                Resources.CreateHtml(),
                Resources.JustHtml(),
                Resources.StartWithDivTag(),
                Resources.DontCreateTitle(model.EntityName)
            ]);

            if (model.LanguageId > 0)
            {
                var language = await _db.Languages.FindByIdAsync(model.LanguageId);
                promptParts.Add(Resources.Language(language.Name.ToLower()));
            }

            // Append phrase for tone from model
            if (model.Tone.HasValue())
            {
                promptParts.Add(Resources.LanguageTone(model.Tone));
            }

            // Append phrase for style from model
            if (model.Style.HasValue())
            {
                promptParts.Add(Resources.LanguageStyle(model.Style));
            }

            BuildStructurePrompt(model, promptParts);
            BuildKeywordsPrompt(model, promptParts);
            BuildIncludeImagesPrompt(model, promptParts, model.IncludeIntro, model.IncludeConclusion);
            
            if (model.AddToc)
            {
                promptParts.Add(Resources.AddTableOfContents(model.TocTitle, model.TocTitleTag));
            }

            await BuildLinkPromptAsync(model, promptParts);
        }

        /// <summary>
        /// Adds prompt parts for creating HTML structure instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IStructureGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildStructurePrompt(IStructureGenerationPrompt model, List<string> promptParts)
        {
            if (model.IncludeIntro)
            {
                promptParts.Add(Resources.IncludeIntro());
            }

            if (model.MainHeadingTag.HasValue())
            {
                promptParts.Add(Resources.MainHeadingTag(model.MainHeadingTag));
            }

            if (model.ParagraphCount > 0)
            {
                promptParts.Add(Resources.ParagraphCount(model.ParagraphCount));

                if (model.ParagraphWordCount > 0)
                {
                    promptParts.Add(Resources.ParagraphWordCount(model.ParagraphWordCount));
                }

                promptParts.Add(Resources.WriteCompleteParagraphs());
            }

            if (model.ParagraphHeadingTag.HasValue())
            {
                promptParts.Add(Resources.ParagraphHeadingTag(model.ParagraphHeadingTag));
            }

            if (model.IncludeConclusion)
            {
                promptParts.Add(Resources.IncludeConclusion());
            }
        }

        /// <summary>
        /// Adds prompt parts for keyword generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IKeywordGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildKeywordsPrompt(IKeywordGenerationPrompt model, List<string> promptParts)
        {
            if (model.Keywords.HasValue())
            {
                promptParts.Add(Resources.UseKeywords(model.Keywords));
                if (model.MakeKeywordsBold)
                {
                    promptParts.Add(Resources.MakeKeywordsBold());
                }
            }

            if (model.KeywordsToAvoid.HasValue())
            {
                promptParts.Add(Resources.KeywordsToAvoid(model.KeywordsToAvoid));
            }
        }

        /// <summary>
        /// Adds prompt parts for image creation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IIncludeImagesGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildIncludeImagesPrompt(IIncludeImagesGenerationPrompt model, List<string> promptParts, 
            bool includeIntro, bool includeConclusion)
        {
            if (model.IncludeImages)
            {
                promptParts.Add(Resources.IncludeImages());

                if (includeIntro)
                {
                    promptParts.Add(Resources.NoIntroImage());
                }

                if (includeConclusion)
                {
                    promptParts.Add(Resources.NoConclusionImage());
                }
            }
        }

        /// <summary>
        /// Adds prompt parts for link generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="ILinkGenerationPrompt"/> model</param>
        /// <param name="promptParts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual async Task BuildLinkPromptAsync(ILinkGenerationPrompt model, List<string> promptParts)
        {
            if (model.AnchorLink.HasValue())
            {
                // Get the correct link from model.AnchorLink
                var linkResolutionResult = await _linkResolver.ResolveAsync(model.AnchorLink);
                var link = linkResolutionResult?.Link;

                if (link.HasValue())
                {
                    if (model.AnchorText.HasValue())
                    {
                        promptParts.Add(Resources.AddNamedLink(model.AnchorText, link));
                    }
                    else
                    {
                        promptParts.Add(Resources.AddLink(link));
                    }

                    if (model.AddCallToAction && model.CallToActionText.HasValue())
                    {
                        promptParts.Add(Resources.AddCallToAction(model.CallToActionText, link));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a prompt to generate pictures based on the given topic.
        /// </summary>
        /// <param name="topic">The topic for which to create the picture.</param>
        /// <returns>The generated prompt part.</returns>
        public virtual void BuildImageBasePrompt(string topic, List<string> promptParts)
        {
            promptParts.Add(Resources.CreatePicture(topic));
        }

        /// <summary>
        /// Adds prompt part with specific parameters for image creation.
        /// </summary>
        /// <param name="model">The <see cref="IImageGenerationPrompt"/> model</param>
        public virtual void BuildImagePrompt(IImageGenerationPrompt model, List<string> promptParts)
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

            promptParts.Add(prompt);
        }

        /// <summary>
        /// Creates a prompt for the meta title.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaTitlePrompt(string forPromptPart, List<string> promptParts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, promptParts);

            promptParts.Add(forPromptPart);

            // TODO: (mh) (ai) Längsten Shopnamen ermitteln und Zeichenlänge in die Anweisung einfügen.
            // INFO: Der Name des Shops wird von Smartstore automatisch dem Title zugefügt. 
            // TODO: (mh) (ai) Ausfürlich mit allen Entitäten testen.
            // Das Original mit dem auf der Produktdetailseite getestet wurde war:
            //forPromptPart += " Verwende dabei nicht den Namen des Shops. Der wird von der Webseite automatisch zugefügt. Reserviere dafür 5 Worte.";
            promptParts.Add(Resources.ReserveSpaceForShopName());

            // INFO: Smartstore automatically adds inverted commas to the title.
            promptParts.Add(Resources.DontUseQuotes());
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaDescriptionPrompt(string forPromptPart, List<string> promptParts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the AI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, promptParts);

            promptParts.Add(forPromptPart);
            promptParts.Add(Resources.DontUseQuotes());
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaKeywordsPrompt(string forPromptPart, List<string> promptParts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, promptParts);

            promptParts.Add(forPromptPart);
            promptParts.Add(Resources.SeparateListWithComma());

            // Respect Smartstore database scheme limitation. Limit is 400.
            promptParts.Add(Resources.CharLimit(400));
        }

        #region Helper methods

        /// <summary>
        /// Adds a instruction for the AI to act in a specific role.
        /// </summary>
        /// <param name="role">The <see cref="AIRole"/></param>
        /// <param name="promptParts">The list of prompt parts to add AI instruction to.</param>
        /// <param name="entityName">The name of the entity. Currently only used to fill a placeholder for the productname when the role is <see cref="AIRole.ProductExpert"/></param>
        /// <returns>AI Instruction: e.g.: Be a SEO expert.</returns>
        public virtual void BuildRolePromptPart(AIRole role, List<string> promptParts, string entityName = "")
        {
            promptParts.Add(Resources.Role(role, entityName));
        }

        /// <summary>
        /// Adds general instructions for AI suggestions.
        /// </summary>
        /// <param name="promptParts">The list of prompt parts to add AI instruction to.</param>
        public virtual void BuildInternalSuggestionPromptPart(List<string> promptParts)
        {
            promptParts.Add(Resources.DontUseQuotes());
            promptParts.Add(Resources.SeparateWithNumberSign());
            promptParts.Add(Resources.DontNumberSuggestions());
        }

        #endregion
    }
}
