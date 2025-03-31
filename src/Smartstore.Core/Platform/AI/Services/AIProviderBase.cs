using Smartstore.Core.AI.Prompting;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Utilities;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// A base class to implement <see cref="IAIProvider"/>.
    /// </summary>
    public abstract class AIProviderBase : IAIProvider
    {
        /// <summary>
        /// The character used to split streamed AI responses.
        /// </summary>
        protected const char AnswerSeparator = '¶';

        /// <summary>
        /// The finish reason when the AI response has been fully transmitted.
        /// </summary>
        protected const string CompletedReason = "stop";

        /// <summary>
        /// The finish reason if the token limit is reached and the AI's response is incomplete (chunked).
        /// Further "go on" completion request(s) are needed.
        /// </summary>
        protected const string ContinueReason = "length";

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public abstract bool IsActive();

        public abstract bool Supports(AIProviderFeatures feature);

        public bool SupportsTextCreation
            => Supports(AIProviderFeatures.TextCreation);

        public bool SupportsTextTranslation
            => Supports(AIProviderFeatures.TextTranslation);

        public bool SupportsImageCreation
            => Supports(AIProviderFeatures.ImageCreation);

        public bool SupportsImageAnalysis
            => Supports(AIProviderFeatures.ImageAnalysis);

        public bool SuportsThemeVarCreation
            => Supports(AIProviderFeatures.ThemeVarCreation);

        public bool SupportsAssistence
            => Supports(AIProviderFeatures.Assistence);

        public virtual string[] GetPreferredModelNames(AIChatTopic topic)
            => null;

        public virtual string[] GetDefaultModelNames()
            => ["default"];

        public virtual Task<string> ChatAsync(AIChat chat, CancellationToken cancelToken = default)
            => throw new NotImplementedException();

        public virtual IAsyncEnumerable<AIChatCompletionResponse> ChatAsStreamAsync(
            AIChat chat,
            int numAnswers,
            CancellationToken cancelToken = default)
            => throw new NotImplementedException();

        public virtual AIImageOptions GetImageOptions(string modelName)
            => new();

        public virtual Task<string[]> CreateImagesAsync(IAIImageModel prompt, int numImages = 1, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        public virtual Task<string> AnalyzeImageAsync(MediaFile file, AIChat chat, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        #region Utilities

        protected virtual async Task<string> ProcessChatAsync(
            AIChat chat,
            AIProviderSettingsBase settings,
            Func<Task<AIChatCompletionResponse>> chatInquirer,
            Action<AIChatMessage[]> addMessages)
        {
            AIChatMessage answer = null;
            string continueStr = null;
            string continueHereStr = null;
            var isComplete = false;
            var maxCompletions = Math.Max(settings.MaxCompletions, 1);

            for (var i = 0; i < maxCompletions && !isComplete; i++)
            {
                var response = await chatInquirer();
                if (response == null)
                {
                    // We do not expect the AI to give anything back in such a case.
                    break;
                }

                isComplete = response.FinishReason.EqualsNoCase(CompletedReason);
                //$"- {i} finish:{response.FinishReason} {response.Content.Truncate(200)}".Dump();

                if (i == 0 && isComplete)
                {
                    // This is the most common case: Exact one completion response. Get out.
                    chat.Assistant(response.Content);
                    return response.Content;
                }

                // Token limit has been reached.
                answer ??= AIChatMessage.FromAssistant(null);
                answer.Append(response.Content);

                continueHereStr ??= T("Smartstore.AI.Prompts.ContinueHere");
                continueStr ??= T("Smartstore.AI.Prompts.PleaseContinue");

                addMessages([
                    AIChatMessage.FromAssistant(response.Content + continueHereStr), 
                    AIChatMessage.FromUser(continueStr)]);
            }

            if (answer != null)
            {
                // Add the entire answer to the chat.
                chat.AddMessages(answer);
                return answer.ToString();
            }

            return null;
        }

        protected virtual async IAsyncEnumerable<AIChatCompletionResponse> ProcessChatAsStreamAsync(
            AIChat chat,
            AIProviderSettingsBase settings,
            int numAnswers,
            Func<int, IAsyncEnumerable<AIChatCompletionResponse>> chatInquirer,
            Action<AIChatMessage[]> addMessages)
        {
            var answers = Enumerable.Range(0, numAnswers).Select(x => AIChatMessage.FromAssistant(null)).ToArray();
            string continueStr = null;
            string continueHereStr = null;
            var isComplete = false;
            var answerIndex = 0;
            var maxCompletions = Math.Max(settings.MaxCompletions, 1);

            for (var i = 0; i < maxCompletions && !isComplete; i++)
            {
                using var psb = StringBuilderPool.Instance.Get(out var sb);

                await foreach (var response in chatInquirer(answerIndex))
                {
                    var content = response?.Content;
                    if (content != null && content.Length > 0)
                    {
                        // Combine each token to get the entire answer. The content must be unaltered,
                        // otherwise the continued answer(s) may be incorrect.
                        sb.Append(content);
                        //$"- {i} finish:{response.FinishReason} answerIndex:{answerIndex} {content}".Dump();

                        if (numAnswers > 1)
                        {
                            // Split if the content contains part of both the current and the next answer.
                            var parts = content.Split(AnswerSeparator);
                            for (var j = 0; j < parts.Length; ++j)
                            {
                                if (j > 0)
                                {
                                    if ((answerIndex + 1) < answers.Length)
                                    {
                                        // Content contains 'AnswerSeparator' at least once -> Switch to next answer.
                                        ++answerIndex;
                                    }
                                    else
                                    {
                                        // We have enough answers.
                                        isComplete = true;
                                        break;
                                    }
                                }

                                var part = parts[j];
                                if (!string.IsNullOrEmpty(part))
                                {
                                    // Emit the part.
                                    answers[answerIndex].Append(part);
                                    yield return new AIChatCompletionResponse(part, answerIndex, response.FinishReason);
                                }
                            }

                            if (isComplete)
                                break;
                        }
                        else
                        {
                            answers[answerIndex].Append(content);
                            yield return response;
                        }
                    }

                    if (response.FinishReason != null)
                    {
                        //$"- {i} finish:{response.FinishReason} answerIndex:{answerIndex} {content}".Dump();
                        if (response.FinishReason == CompletedReason)
                        {
                            isComplete = true;
                        }
                        else if (response.FinishReason == ContinueReason)
                        {
                            // INFO: The AI tends to repeat itself if there are several requests within a chat.
                            // This can be largely avoided by marking at the end of the previous answer where to continue.
                            continueHereStr ??= T("Smartstore.AI.Prompts.ContinueHere");
                            sb.Append(continueHereStr);
                            break;
                        }
                    }
                }

                // Do not waste tokens. Add only the part of the current request to the history, not the entire answer.
                addMessages([AIChatMessage.FromAssistant(sb.ToString())]);

                if (!isComplete)
                {
                    continueStr ??= T("Smartstore.AI.Prompts.PleaseContinue");
                    addMessages([AIChatMessage.FromUser(continueStr)]);
                }
            }

            // Add the entire answer to the chat.
            chat.AddMessages(answers);
        }

        protected static string ValidateModelName(string modelName, string[] supportedModelNames)
        {
            if (modelName.IsEmpty() || !supportedModelNames.Any(x => x.EqualsNoCase(modelName)))
            {
                return supportedModelNames[0];
            }

            return modelName;
        }

        #endregion
    }
}
