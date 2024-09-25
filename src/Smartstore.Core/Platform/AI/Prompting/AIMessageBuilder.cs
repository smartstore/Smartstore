using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;

namespace Smartstore.Core.Platform.AI.Prompting
{
    public partial class AIMessageBuilder
    {
        private readonly SmartDbContext _db;
        private readonly ILinkResolver _linkResolver;

        public AIMessageBuilder(
            SmartDbContext db,
            ILinkResolver linkResolver,
            AIMessageResources promptResources)
        {
            _db = db;
            _linkResolver = linkResolver;
            Resources = promptResources;
        }

        public AIMessageResources Resources { get; }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        /// <param name="isRichText">A value indicating whether to build a HTML containing rich text prompt.</param>
        public virtual Task AddTextMessagesAsync(IAITextModel model, AIChat chat, bool isRichText)
        {
            return isRichText
                ? AddRichTextMessagesAsync(model, chat)
                : AddSimpleTextMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for creating HTML structure instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextLayoutModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual void AddTextLayoutMessages(IAITextLayoutModel model, AIChat chat)
        {
            if (model.IncludeIntro)
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.IncludeIntro()));
            }

            if (model.MainHeadingTag.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.MainHeadingTag(model.MainHeadingTag)));
            }

            if (model.ParagraphCount > 0)
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.ParagraphCount(model.ParagraphCount)));

                if (model.ParagraphWordCount > 0)
                {
                    chat.AddMessages(AIChatMessage.FromUser(Resources.ParagraphWordCount(model.ParagraphWordCount)));
                }

                chat.AddMessages(AIChatMessage.FromSystem(Resources.WriteCompleteParagraphs()));
            }

            if (model.ParagraphHeadingTag.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.ParagraphHeadingTag(model.ParagraphHeadingTag)));
            }

            if (model.IncludeConclusion)
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.IncludeConclusion()));
            }
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for keyword generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIKeywordModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual void AddKeywordsMessages(IAIKeywordModel model, AIChat chat)
        {
            if (model.Keywords.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.UseKeywords(model.Keywords)));
                if (model.MakeKeywordsBold)
                {
                    chat.AddMessages(AIChatMessage.FromUser(Resources.MakeKeywordsBold()));
                }
            }

            if (model.KeywordsToAvoid.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.KeywordsToAvoid(model.KeywordsToAvoid)));
            }
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for image creation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIImageContainerModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual void AddImageContainerMessages(
            IAIImageContainerModel model,
            AIChat chat,
            bool includeIntro,
            bool includeConclusion)
        {
            if (model.IncludeImages)
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.IncludeImages()));

                if (includeIntro)
                {
                    chat.AddMessages(AIChatMessage.FromUser(Resources.NoIntroImage()));
                }

                if (includeConclusion)
                {
                    chat.AddMessages(AIChatMessage.FromUser(Resources.NoConclusionImage()));
                }
            }
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for link generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAILinkModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual async Task AddLinkMessagesAsync(IAILinkModel model, AIChat chat)
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
                        chat.AddMessages(AIChatMessage.FromUser(Resources.AddNamedLink(model.AnchorText, link)));
                    }
                    else
                    {
                        chat.AddMessages(AIChatMessage.FromUser(Resources.AddLink(link)));
                    }

                    if (model.AddCallToAction && model.CallToActionText.HasValue())
                    {
                        chat.AddMessages(AIChatMessage.FromUser(Resources.AddCallToAction(model.CallToActionText, link)));
                    }
                }
            }
        }

        /// <summary>
        /// Adds <see cref="AIChatMessage"/> with specific parameters for image creation.
        /// </summary>
        /// <param name="model">The <see cref="IAIImageModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual void BuildImagePrompt(IAIImageModel model, AIChat chat)
        {
            var message = string.Empty;

            if (model.Medium.HasValue())
            {
                message += model.Medium + ", ";
            }

            if (model.Environment.HasValue())
            {
                message += model.Environment + ", ";
            }

            if (model.Lighting.HasValue())
            {
                message += model.Lighting + ", ";
            }

            if (model.Color.HasValue())
            {
                message += model.Color + ", ";
            }

            if (model.Mood.HasValue())
            {
                message += model.Mood + ", ";
            }

            if (model.Composition.HasValue())
            {
                message += model.Composition;
            }

            chat.AddMessages(AIChatMessage.FromUser(message));
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta title generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual void AddMetaTitleMessages(string forPromptPart, AIChat chat)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            AddRoleMessage(AIRole.SEOExpert, chat);

            chat.AddMessages(AIChatMessage.FromUser(forPromptPart));

            // TODO: (mh) (ai) Längsten Shopnamen ermitteln und Zeichenlänge in die Anweisung einfügen.
            // INFO: Der Name des Shops wird von Smartstore automatisch dem Title zugefügt. 
            // TODO: (mh) (ai) Ausfürlich mit allen Entitäten testen.
            // Das Original mit dem auf der Produktdetailseite getestet wurde war:
            //forPromptPart += " Verwende dabei nicht den Namen des Shops. Der wird von der Webseite automatisch zugefügt. Reserviere dafür 5 Worte.";
            chat.AddMessages(AIChatMessage.FromSystem(Resources.ReserveSpaceForShopName()));

            // INFO: Smartstore automatically adds inverted commas to the title.
            chat.AddMessages(AIChatMessage.FromSystem(Resources.DontUseQuotes()));
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta description generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual void AddMetaDescriptionMessages(string forPromptPart, AIChat chat)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the AI to be a SEO expert, it already knows the correct limits.
            AddRoleMessage(AIRole.SEOExpert, chat);

            chat.AddMessages(AIChatMessage.FromUser(forPromptPart));
            chat.AddMessages(AIChatMessage.FromSystem(Resources.DontUseQuotes()));
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta keywords generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual void AddMetaKeywordsMessages(string forPromptPart, AIChat chat)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            AddRoleMessage(AIRole.SEOExpert, chat);

            chat.AddMessages(AIChatMessage.FromUser(forPromptPart));
            chat.AddMessages(AIChatMessage.FromSystem(Resources.SeparateListWithComma()));
        }

        #region Helper methods

        /// <summary>
        /// Adds a <see cref="AIChatMessage"/> containing an instruction for the AI to act in a specific role.
        /// </summary>
        /// <param name="role">The <see cref="AIRole"/></param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        /// <param name="entityName">The name of the entity. Currently only used to fill a placeholder for the productname when the role is <see cref="AIRole.ProductExpert"/></param>
        /// <returns>AI Instruction: e.g.: Be a SEO expert.</returns>
        public virtual void AddRoleMessage(AIRole role, AIChat chat, string entityName = "")
        {
            chat.AddMessages(AIChatMessage.FromSystem(Resources.Role(role, entityName)));
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for general instructions for AI suggestions.
        /// </summary>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual void AddSuggestionMessages(IAISuggestionModel model, AIChat chat)
        {
            chat.AddMessages(
                AIChatMessage.FromSystem(Resources.DontUseMarkdown()),
                AIChatMessage.FromSystem(Resources.DontUseQuotes()),
                AIChatMessage.FromSystem(Resources.DontNumberSuggestions()),
                AIChatMessage.FromSystem(Resources.SeparateWithNumberSign())
            );

            if (model.CharLimit > 0)
            {
                chat.AddMessages(AIChatMessage.FromSystem(Resources.CharLimitSuggestions(model.CharLimit)));
            }
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for simple text creation, e.g. not do use markdown. 
        /// Wordlimit, Tone and Style are properties of <paramref name="model"/> which are also considered.
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        protected virtual Task AddSimpleTextMessagesAsync(IAITextModel model, AIChat chat)
        {
            chat.AddMessages(AIChatMessage.FromSystem(Resources.DontUseMarkdown()));

            if (model.CharLimit > 0)
            {
                chat.AddMessages(AIChatMessage.FromSystem(Resources.CharLimit(model.CharLimit)));
            }

            if (model.WordLimit > 0)
            {
                chat.AddMessages(AIChatMessage.FromSystem(Resources.WordLimit(model.WordLimit)));
            }

            return AddLanguageMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        protected virtual async Task AddRichTextMessagesAsync(IAITextModel model, AIChat chat)
        {
            AddHtmlMessages(chat);
            await AddLanguageMessagesAsync(model, chat);

            chat.AddMessages(AIChatMessage.FromSystem(Resources.DontCreateTitle(model.EntityName)));

            AddTextLayoutMessages(model, chat);
            AddKeywordsMessages(model, chat);
            AddImageContainerMessages(model, chat, model.IncludeIntro, model.IncludeConclusion);

            if (model.AddToc)
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.AddTableOfContents(model.TocTitle, model.TocTitleTag)));
            }

            await AddLinkMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for language name, tone and style.
        /// </summary>
        protected virtual async Task AddLanguageMessagesAsync(IAITextModel model, AIChat chat)
        {
            if (model.LanguageId > 0)
            {
                var languageName = await _db.Languages
                    .Where(x => x.Id == model.LanguageId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                if (languageName.HasValue())
                {
                    chat.AddMessages(AIChatMessage.FromUser(Resources.Language(languageName.ToLower())));
                }
            }

            if (model.Tone.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.LanguageTone(model.Tone)));
            }

            if (model.Style.HasValue())
            {
                chat.AddMessages(AIChatMessage.FromUser(Resources.LanguageStyle(model.Style)));
            }
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for HTML creation.
        /// </summary>
        protected virtual void AddHtmlMessages(AIChat chat)
        {
            chat.AddMessages(
                AIChatMessage.FromSystem(Resources.CreateHtml()),
                AIChatMessage.FromSystem(Resources.JustHtml()),
                AIChatMessage.FromSystem(Resources.StartWithDivTag())
            );
        }

        #endregion
    }
}
