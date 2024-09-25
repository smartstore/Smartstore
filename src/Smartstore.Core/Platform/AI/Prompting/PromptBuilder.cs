using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;

namespace Smartstore.Core.Platform.AI.Prompting
{
    public partial class PromptBuilder
    {
        private readonly SmartDbContext _db;
        private readonly ILinkResolver _linkResolver;

        public PromptBuilder(
            SmartDbContext db,
            ILinkResolver linkResolver,
            PromptResources promptResources)
        {
            _db = db;
            _linkResolver = linkResolver;
            Resources = promptResources;
        }

        public PromptResources Resources { get; }

        /// <summary>
        /// Adds prompt parts with general instructions for text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        /// <param name="isRichText">A value indicating whether to build a HTML containing rich text prompt.</param>
        public virtual Task BuildTextPromptAsync(IAITextModel model, List<string> parts, bool isRichText)
        {
            return isRichText
                ? BuildRichTextPromptAsync(model, parts)
                : BuildSimpleTextPromptAsync(model, parts);
        }

        /// <summary>
        /// Adds prompt parts for creating HTML structure instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextLayoutModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildTextLayoutPrompt(IAITextLayoutModel model, List<string> parts)
        {
            if (model.IncludeIntro)
            {
                parts.Add(Resources.IncludeIntro());
            }

            if (model.MainHeadingTag.HasValue())
            {
                parts.Add(Resources.MainHeadingTag(model.MainHeadingTag));
            }

            if (model.ParagraphCount > 0)
            {
                parts.Add(Resources.ParagraphCount(model.ParagraphCount));

                if (model.ParagraphWordCount > 0)
                {
                    parts.Add(Resources.ParagraphWordCount(model.ParagraphWordCount));
                }

                parts.Add(Resources.WriteCompleteParagraphs());
            }

            if (model.ParagraphHeadingTag.HasValue())
            {
                parts.Add(Resources.ParagraphHeadingTag(model.ParagraphHeadingTag));
            }

            if (model.IncludeConclusion)
            {
                parts.Add(Resources.IncludeConclusion());
            }
        }

        /// <summary>
        /// Adds prompt parts for keyword generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIKeywordModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildKeywordsPrompt(IAIKeywordModel model, List<string> parts)
        {
            if (model.Keywords.HasValue())
            {
                parts.Add(Resources.UseKeywords(model.Keywords));
                if (model.MakeKeywordsBold)
                {
                    parts.Add(Resources.MakeKeywordsBold());
                }
            }

            if (model.KeywordsToAvoid.HasValue())
            {
                parts.Add(Resources.KeywordsToAvoid(model.KeywordsToAvoid));
            }
        }

        /// <summary>
        /// Adds prompt parts for image creation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIImageContainerModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual void BuildImageContainerPrompt(
            IAIImageContainerModel model, 
            List<string> parts, 
            bool includeIntro, 
            bool includeConclusion)
        {
            if (model.IncludeImages)
            {
                parts.Add(Resources.IncludeImages());

                if (includeIntro)
                {
                    parts.Add(Resources.NoIntroImage());
                }

                if (includeConclusion)
                {
                    parts.Add(Resources.NoConclusionImage());
                }
            }
        }

        /// <summary>
        /// Adds prompt parts for link generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAILinkModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        public virtual async Task BuildLinkPromptAsync(IAILinkModel model, List<string> parts)
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
                        parts.Add(Resources.AddNamedLink(model.AnchorText, link));
                    }
                    else
                    {
                        parts.Add(Resources.AddLink(link));
                    }

                    if (model.AddCallToAction && model.CallToActionText.HasValue())
                    {
                        parts.Add(Resources.AddCallToAction(model.CallToActionText, link));
                    }
                }
            }
        }

        /// <summary>
        /// Adds prompt part with specific parameters for image creation.
        /// </summary>
        /// <param name="model">The <see cref="IAIImageModel"/> model</param>
        public virtual void BuildImagePrompt(IAIImageModel model, List<string> parts)
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

            parts.Add(prompt);
        }

        /// <summary>
        /// Creates a prompt for the meta title.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaTitlePrompt(string forPromptPart, List<string> parts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, parts);

            parts.Add(forPromptPart);

            // TODO: (mh) (ai) Längsten Shopnamen ermitteln und Zeichenlänge in die Anweisung einfügen.
            // INFO: Der Name des Shops wird von Smartstore automatisch dem Title zugefügt. 
            // TODO: (mh) (ai) Ausfürlich mit allen Entitäten testen.
            // Das Original mit dem auf der Produktdetailseite getestet wurde war:
            //forPromptPart += " Verwende dabei nicht den Namen des Shops. Der wird von der Webseite automatisch zugefügt. Reserviere dafür 5 Worte.";
            parts.Add(Resources.ReserveSpaceForShopName());

            // INFO: Smartstore automatically adds inverted commas to the title.
            parts.Add(Resources.DontUseQuotes());
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaDescriptionPrompt(string forPromptPart, List<string> parts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the AI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, parts);

            parts.Add(forPromptPart);
            parts.Add(Resources.DontUseQuotes());
        }

        /// <summary>
        /// Creates a prompt for the meta description..
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        public virtual void BuildMetaKeywordsPrompt(string forPromptPart, List<string> parts)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            BuildRolePromptPart(AIRole.SEOExpert, parts);

            parts.Add(forPromptPart);
            parts.Add(Resources.SeparateListWithComma());
        }

        #region Helper methods

        /// <summary>
        /// Adds a instruction for the AI to act in a specific role.
        /// </summary>
        /// <param name="role">The <see cref="AIRole"/></param>
        /// <param name="parts">The list of prompt parts to add AI instruction to.</param>
        /// <param name="entityName">The name of the entity. Currently only used to fill a placeholder for the productname when the role is <see cref="AIRole.ProductExpert"/></param>
        /// <returns>AI Instruction: e.g.: Be a SEO expert.</returns>
        public virtual void BuildRolePromptPart(AIRole role, List<string> parts, string entityName = "")
        {
            parts.Add(Resources.Role(role, entityName));
        }

        /// <summary>
        /// Adds general instructions for AI suggestions.
        /// </summary>
        /// <param name="parts">The list of prompt parts to add AI instruction to.</param>
        public virtual void BuildSuggestionPromptPart(IAISuggestionModel model, List<string> parts)
        {
            parts.Add(Resources.DontUseQuotes());

            // We can assume that suggestions are only to be created for simple text.
            parts.Add(Resources.DontUseMarkdown());

            parts.Add(Resources.DontNumberSuggestions());
            parts.Add(Resources.SeparateWithNumberSign());

            if (model.CharLimit > 0)
            {
                parts.Add(Resources.CharLimitSuggestions(model.CharLimit));
            }
        }

        /// <summary>
        /// Adds prompt parts with general instructions for simple text creation, e.g. not do use markdown. 
        /// Wordlimit, Tone and Style are properties of <paramref name="model"/> that are also considered.
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        protected virtual Task BuildSimpleTextPromptAsync(IAITextModel model, List<string> parts)
        {
            parts.Add(Resources.DontUseMarkdown());

            if (model.CharLimit > 0)
            {
                parts.Add(Resources.CharLimit(model.CharLimit));
            }

            if (model.WordLimit > 0)
            {
                parts.Add(Resources.WordLimit(model.WordLimit));
            }

            return AddLanguagePartsAsync(model, parts);
        }

        /// <summary>
        /// Adds prompt parts with general instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="parts">The list of prompt parts to which the generated prompt will be added.</param>
        protected virtual async Task BuildRichTextPromptAsync(IAITextModel model, List<string> parts)
        {
            AddHtmlParts(parts);
            await AddLanguagePartsAsync(model, parts);

            parts.Add(Resources.DontCreateTitle(model.EntityName));

            BuildTextLayoutPrompt(model, parts);
            BuildKeywordsPrompt(model, parts);
            BuildImageContainerPrompt(model, parts, model.IncludeIntro, model.IncludeConclusion);

            if (model.AddToc)
            {
                parts.Add(Resources.AddTableOfContents(model.TocTitle, model.TocTitleTag));
            }

            await BuildLinkPromptAsync(model, parts);
        }

        /// <summary>
        /// Adds parts for language name, tone and style.
        /// </summary>
        protected virtual async Task AddLanguagePartsAsync(IAITextModel model, List<string> parts)
        {
            if (model.LanguageId > 0)
            {
                var languageName = await _db.Languages
                    .Where(x => x.Id == model.LanguageId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                if (languageName.HasValue())
                {
                    parts.Add(Resources.Language(languageName.ToLower()));
                }
            }

            if (model.Tone.HasValue())
            {
                parts.Add(Resources.LanguageTone(model.Tone));
            }

            if (model.Style.HasValue())
            {
                parts.Add(Resources.LanguageStyle(model.Style));
            }
        }

        /// <summary>
        /// Adds parts for HTML creation.
        /// </summary>
        protected virtual void AddHtmlParts(List<string> parts)
        {
            parts.AddRange([
                Resources.CreateHtml(),
                Resources.JustHtml(),
                Resources.StartWithDivTag()]);
        }

        #endregion
    }
}
