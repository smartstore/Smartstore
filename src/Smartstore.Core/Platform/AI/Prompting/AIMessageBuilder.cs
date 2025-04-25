using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.AI.Prompting
{
    public partial class AIMessageBuilder
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ILinkResolver _linkResolver;

        public AIMessageBuilder(
            SmartDbContext db,
            IStoreContext storeContext,
            ILinkResolver linkResolver,
            AIMessageResources promptResources)
        {
            _db = db;
            _storeContext = storeContext;
            _linkResolver = linkResolver;
            Resources = promptResources;
        }

        public AIMessageResources Resources { get; }

        public virtual string GetDefaultMessage(AIChatTopic topic, params object[] args)
        {
            switch (topic)
            {
                case AIChatTopic.Text:
                    return Resources.GetResource("Admin.AI.TextCreation.DefaultPrompt", args);
                case AIChatTopic.Suggestion:
                    return Resources.GetResource("Admin.AI.Suggestions.DefaultPrompt", args);
                case AIChatTopic.Image:
                    return Resources.GetResource("Admin.AI.ImageCreation.DefaultPrompt", args);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual Task<AIChat> AddTextMessagesAsync(IAITextModel model, AIChat chat)
        {
            return chat.Topic == AIChatTopic.RichText
                ? AddRichTextMessagesAsync(model, chat)
                : AddSimpleTextMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for creating HTML structure instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextLayoutModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual AIChat AddTextLayoutMessages(IAITextLayoutModel model, AIChat chat)
        {
            if (model.IncludeIntro)
            {
                chat.User(Resources.IncludeIntro()).SetMetaData(model.IncludeIntro);
            }

            if (model.MainHeadingTag.HasValue())
            {
                chat.User(Resources.MainHeadingTag(model.MainHeadingTag)).SetMetaData(model.MainHeadingTag);
            }

            if (model.ParagraphCount > 0)
            {
                chat.User(Resources.ParagraphCount(model.ParagraphCount)).SetMetaData(model.ParagraphCount);

                if (model.ParagraphWordCount > 0)
                {
                    chat.User(Resources.ParagraphWordCount(model.ParagraphWordCount)).SetMetaData(model.ParagraphWordCount);
                }
            }

            if (model.ParagraphHeadingTag.HasValue())
            {
                chat.User(Resources.ParagraphHeadingTag(model.ParagraphHeadingTag)).SetMetaData(model.ParagraphHeadingTag);
            }

            if (model.IncludeConclusion)
            {
                chat.User(Resources.IncludeConclusion()).SetMetaData(model.IncludeConclusion);
            }

            return chat;
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for keyword generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIKeywordModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual AIChat AddKeywordsMessages(IAIKeywordModel model, AIChat chat)
        {
            if (model.Keywords.HasValue())
            {
                chat.User(Resources.UseKeywords(model.Keywords)).SetMetaData(model.Keywords);

                if (model.MakeKeywordsBold)
                {
                    chat.User(Resources.MakeKeywordsBold()).SetMetaData(model.MakeKeywordsBold);
                }
            }

            if (model.KeywordsToAvoid.HasValue())
            {
                chat.User(Resources.KeywordsToAvoid(model.KeywordsToAvoid)).SetMetaData(model.KeywordsToAvoid);
            }

            return chat;
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for image creation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAIImageContainerModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual AIChat AddImageContainerMessages(
            IAIImageContainerModel model,
            AIChat chat,
            bool includeIntro,
            bool includeConclusion)
        {
            if (model.IncludeImages)
            {
                chat.User(Resources.IncludeImages()).SetMetaData(model.IncludeImages);

                if (includeIntro)
                {
                    chat.User(Resources.NoIntroImage()).SetMetaData("IncludeIntro", includeIntro);
                }

                if (includeConclusion)
                {
                    chat.User(Resources.NoConclusionImage()).SetMetaData("IncludeConclusion", includeConclusion);
                }
            }

            return chat;
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for link generation instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAILinkModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual async Task<AIChat> AddLinkMessagesAsync(IAILinkModel model, AIChat chat)
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
                        chat.User(Resources.AddNamedLink(model.AnchorText, link)).SetMetaData(model.AnchorText);
                    }
                    else
                    {
                        chat.User(Resources.AddLink(link)).SetMetaData(model.AnchorLink);
                    }

                    if (model.AddCallToAction && model.CallToActionText.HasValue())
                    {
                        chat.User(Resources.AddCallToAction(model.CallToActionText, link)).SetMetaData(model.AddCallToAction);
                    }
                }
            }

            return chat;
        }

        /// <summary>
        /// Adds <see cref="AIChatMessage"/> with specific parameters for image creation.
        /// </summary>
        /// <param name="model">The <see cref="IAIImageModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual AIChat BuildImagePrompt(IAIImageModel model, AIChat chat)
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

            return chat.User(message);
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta title generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual AIChat AddMetaTitleMessages(string forPromptPart, AIChat chat)
        {
            int longestStoreNameLength = _storeContext.GetCachedStores().Stores.Values.Max(s => s.Name.Length);
            var operativeRoleInstructions = new List<string>
            {
                Resources.ReserveSpaceForShopName(longestStoreNameLength + 1)
            };

            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            return AddRoleMessage(AIRole.SEOExpert, chat, operativeRoleInstructions)
                .UserTopic(forPromptPart);
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta description generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual AIChat AddMetaDescriptionMessages(string forPromptPart, AIChat chat)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the AI to be a SEO expert, it already knows the correct limits.
            return AddRoleMessage(AIRole.SEOExpert, chat)
                .UserTopic(forPromptPart);
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for the meta keywords generation.
        /// </summary>
        /// <param name="forPromptPart">The part where we tell the AI what to generate.</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        public virtual AIChat AddMetaKeywordsMessages(string forPromptPart, AIChat chat)
        {
            // INFO: No need for word limit in SEO properties. Because we advised the KI to be a SEO expert, it already knows the correct limits.
            return AddRoleMessage(AIRole.SEOExpert, chat)
                .UserTopic(forPromptPart)
                .System(Resources.SeparateListWithComma());
        }

        #region Helper methods

        /// <summary>
        /// Adds a <see cref="AIChatMessage"/> containing instructions for the AI to act in a specific role.
        /// </summary>
        /// <param name="role">The <see cref="AIRole"/></param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated message will be added.</param>
        /// <param name="roleInstructions">
        /// A list of explicit behavioral instructions that define how the AI should interpret and act within its system role. 
        /// These rules form the operational core of the system prompt and guide the model's response style, structure, and constraints. 
        /// Each entry in the list should represent a clear, standalone directive.
        /// </param>
        /// <param name="entityName">The name of the entity. Currently only used to fill a placeholder for the productname when the role is <see cref="AIRole.ProductExpert"/></param>
        /// <returns>AI Instruction: e.g.: Be a SEO expert.</returns>
        public virtual AIChat AddRoleMessage(AIRole role, AIChat chat, List<string> roleInstructions = null, string entityName = "")
        {
            var message = Resources.Role(role, entityName);
            roleInstructions ??= [];

            // Lets add some generic operational instructions for explizit roles.
            var resRoot = AIMessageResources.PromptResourceRoot;

            // INFO: ProductExpert will be used exclusivly by RichTextDialog of Product.LongDescription
            if (role == AIRole.ProductExpert)
            {
                // Add an empty line between the first role and the second role to make clear that these are two different dimensions of the role.
                message += "\n\n" + Resources.Role(AIRole.HtmlEditor);

                roleInstructions.AddRange(
                    Resources.GetResource(resRoot + "Product.NoAssumptions"),
                    Resources.DontCreateProductTitle()
                );
            }
            else if (role == AIRole.ImageAnalyzer)
            {
                message += "\n\n" + Resources.Role(AIRole.SEOExpert);

                var imageAnalyzerResRoot = resRoot + "ImageAnalyzer.";

                // INFO: This instruction must be built differently to accomplish sub lists
                var objectDefinition = Resources.GetResource(imageAnalyzerResRoot + "ObjectDefinition");
                objectDefinition += "\n  - " + Resources.GetResource(imageAnalyzerResRoot + "ObjectDefinition.Title");
                objectDefinition += "\n  - " + Resources.GetResource(imageAnalyzerResRoot + "ObjectDefinition.Alt");
                objectDefinition += "\n  - " + Resources.GetResource(imageAnalyzerResRoot + "ObjectDefinition.Tags");

                roleInstructions.AddRange(
                    objectDefinition,
                    Resources.GetResource(imageAnalyzerResRoot + "NoContent"),
                    Resources.GetResource(resRoot+ "CreateJson"),
                    Resources.DontUseMarkdown()
                );
            }
            else if (role == AIRole.Translator)
            {
                var translatorResRoot = resRoot + "Translator.";

                roleInstructions.AddRange(
                    Resources.GetResource(translatorResRoot + "TranslateTextContentOnly"),
                    Resources.GetResource(translatorResRoot + "PreserveHtmlStructure"),
                    Resources.GetResource(translatorResRoot + "IgnoreTechnicalAttributes"),
                    Resources.GetResource(translatorResRoot + "KeepHtmlEntitiesIntact"),
                    Resources.GetResource(translatorResRoot + "TranslateWithContext"),
                    Resources.GetResource(translatorResRoot + "TranslateDescriptiveAttributes"),
                    Resources.GetResource(translatorResRoot + "PreserveToneAndStyle"),
                    // TODO: (mh) (ai) Dangerous!!! Senseless token eater.
                    Resources.GetResource(translatorResRoot + "SkipAlreadyTranslated"),
                    Resources.DontUseQuotes(),
                    Resources.GetResource(translatorResRoot + "NoMetaComments"),
                    Resources.DontUseMarkdown()
                );
            }

            if (chat.Topic == AIChatTopic.RichText)
            {
                roleInstructions.AddRange(
                    Resources.WriteCompleteParagraphs(), 
                    Resources.UseImagePlaceholders(),
                    Resources.CreatHtmlWithoutMarkdown(),
                    Resources.NoFriendlyIntroductions(),
                    Resources.StartWithDivTag()
                );
            }
            else if (chat.Topic == AIChatTopic.Suggestion)
            {
                var suggestionResRoot = resRoot + "Suggestions.";

                roleInstructions.AddRange(
                    Resources.GetResource(suggestionResRoot + "Separation"),
                    Resources.GetResource(suggestionResRoot + "NoNumbering"),
                    Resources.GetResource(suggestionResRoot + "NoRepitions"),
                    Resources.DontUseMarkdown(),
                    Resources.DontUseQuotes(),
                    Resources.DontUseLineBreaks()
                );
            }

            if (roleInstructions != null && roleInstructions.Count > 0)
            {
                // INFO: Structuring role instructions as a clear list helps the AI parse and follow them more reliably, reducing the risk of missed rules.
                message += "\n\n" + Resources.GetResource(resRoot + "Role.Rules") + "\n\n- ";
                message += string.Join("\n- ", roleInstructions);
            }

            return chat.System(message);
        }

        /// <summary>
        /// Adds messages of type <see cref="AIChatMessage"/> to a <see cref="AIChat" /> for general instructions for AI suggestions.
        /// </summary>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        public virtual AIChat AddSuggestionMessages(IAISuggestionModel model, AIChat chat)
        {
            if (model.CharLimit > 0)
            {
                chat.System(Resources.GetResource("Smartstore.AI.Prompts.Suggestions.CharLimit", model.CharLimit))
                    .SetMetaData(model.CharLimit);
            }

            return chat;
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for simple text creation, e.g. not do use markdown. 
        /// Wordlimit, Tone and Style are properties of <paramref name="model"/> which are also considered.
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        protected virtual Task<AIChat> AddSimpleTextMessagesAsync(IAITextModel model, AIChat chat)
        {
            chat.System(Resources.DontUseMarkdown())
                .System(Resources.DontUseQuotes());

            if (model.CharLimit > 0 && model.WordLimit > 0)
            {
                // INFO: WordLimit should be a user message and CharLimit a system message.
                // But as this case is probably very rare, we just use the system message for both.
                chat.System(Resources.CharWordLimit(model.CharLimit, model.WordLimit.Value))
                    .SetMetaData(model.CharLimit)
                    .SetMetaData(model.WordLimit);
            }
            else if (model.CharLimit > 0)
            {
                chat.System(Resources.CharLimit(model.CharLimit))
                    .SetMetaData(model.CharLimit);
            }
            else if (model.WordLimit > 0)
            {
                chat.User(Resources.WordLimit((int)model.WordLimit))
                    .SetMetaData(model.WordLimit);
            }

            AddKeywordsMessages(model, chat);

            return AddLanguageMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> with general instructions for rich text creation. 
        /// </summary>
        /// <param name="model">The <see cref="IAITextModel"/> model</param>
        /// <param name="chat">The <see cref="AIChat" /> containing a <see cref="List{AIChatMessage}"/> to which the generated messages will be added.</param>
        protected virtual async Task<AIChat> AddRichTextMessagesAsync(IAITextModel model, AIChat chat)
        {
            // INFO: For products we use a slightly different version
            if (model.TargetProperty != "FullDescription" && model.Type != "Product")
            {
                chat.System(Resources.DontCreateTitle(model.EntityName));
            }

            await AddLanguageMessagesAsync(model, chat);
            AddTextLayoutMessages(model, chat);
            AddKeywordsMessages(model, chat);
            AddImageContainerMessages(model, chat, model.IncludeIntro, model.IncludeConclusion);

            if (model.AddToc)
            {
                chat.User(Resources.AddTableOfContents(model.TocTitle, model.TocTitleTag)).SetMetaData(model.AddToc);
            }

            return await AddLinkMessagesAsync(model, chat);
        }

        /// <summary>
        /// Adds <see cref="List{AIChatMessage}"/> for language name, tone and style.
        /// </summary>
        protected virtual async Task<AIChat> AddLanguageMessagesAsync(IAITextModel model, AIChat chat)
        {
            if (model.LanguageId > 0)
            {
                var languageName = await _db.Languages
                    .Where(x => x.Id == model.LanguageId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                if (languageName.HasValue())
                {
                    chat.User(Resources.Language(languageName.ToLower()))
                        .SetMetaData(model.LanguageId)
                        .SetMetaData("LanguageName", languageName); 
                }
            }

            if (model.Tone.HasValue())
            {
                chat.User(Resources.LanguageTone(model.Tone));
                chat.SetMetaData(model.Tone);
            }

            if (model.Style.HasValue())
            {
                chat.User(Resources.LanguageStyle(model.Style));
                chat.SetMetaData(model.Style);
            }

            return chat;
        }

        #endregion
    }
}
