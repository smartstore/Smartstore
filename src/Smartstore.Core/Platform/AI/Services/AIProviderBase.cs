using Smartstore.Core.AI.Metadata;
using Smartstore.Core.AI.Prompting;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.IO;
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

        public virtual AIMetadata Metadata { get; protected set; }

        public virtual AIModelCollection GetModels(AIChatTopic topic)
        {
            var outputType = topic == AIChatTopic.Image ? AIOutputType.Image : AIOutputType.Text;
            return Metadata?.MergeModels(outputType, GetPreferredModelNames(outputType));
        }

        public virtual Task<AIModelCollection> GetLiveModelsAsync(CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        protected virtual string[] GetPreferredModelNames(AIOutputType outputType)
            => [];

        public virtual Task<string> ChatAsync(AIChat chat, CancellationToken cancelToken = default)
        {
            if (chat == null || !chat.HasMessages())
            {
                return Task.FromResult<string>(null);
            }

            if (chat.Topic == AIChatTopic.Image)
            {
                if (!chat.Metadata.TryGetAndConvertValue<AIImageChatContext>(KnownAIChatMetadataKeys.ImageChatContext, out var ctx) || ctx == null)
                {
                    throw new AIException($"Please provide an image chat context through chat metadata \"{KnownAIChatMetadataKeys.ImageChatContext}\" for a chat of AIChatTopic.Image.");
                }

                return ImageChatAsync(chat, ctx, cancelToken);
            }
            else
            {
                return TextChatAsync(chat, cancelToken);
            }
        }

        /// <summary>
        /// Starts or continues a text-to-text AI conversation. Adds the latest answer to the chat.
        /// </summary>
        /// <returns>AI text answer.</returns>
        protected virtual Task<string> TextChatAsync(AIChat chat, CancellationToken cancelToken = default)
            => throw new NotImplementedException();

        /// <summary>
        /// Starts or continues a text-to-image AI conversation, including source image(s), to create an image.
        /// </summary>
        /// <param name="context">Context data for image generation.</param>
        /// <returns>Path of a temporary image file.</returns>
        protected virtual Task<string> ImageChatAsync(AIChat chat, AIImageChatContext context, CancellationToken cancelToken = default)
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

        protected static float? ClampTemperature(float temperature)
        {
            if (temperature == 1) return null;
            if (temperature < 0) return 0;
            if (temperature > 2) return 2;

            return (float?)Math.Round(temperature, 1);
        }

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

        /// <summary>
        /// Creates a temporary image file from <paramref name="imageData"/> in <paramref name="tempDirectory"/>.
        /// </summary>
        /// <param name="imageData">AI image data.</param>
        /// <param name="tempDirectory">Directory where to create the temp file.</param>
        /// <param name="mimeType">Mime type of the AI image. "png" if <c>null</c>.</param>
        /// <returns>Temp file or <c>null</c> if the file cannot be created.</returns>
        protected virtual async Task<IFile> CreateTempImageFileAsync(
            BinaryData imageData,
            IDirectory tempDirectory,
            string mimeType = null,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(tempDirectory);

            if (imageData == null || imageData.IsEmpty)
            {
                return null;
            }

            var extension = MimeTypes.MapMimeTypeToExtension(mimeType).OrDefault("png");
            var fileName = Path.GetRandomFileName() + '.' + extension;
            var path = PathUtility.Join(tempDirectory.SubPath, fileName);

            using var imageStream = imageData.ToStream();
            var file = await tempDirectory.FileSystem.CreateFileAsync(path, imageStream, true, cancelToken);
            if (file?.Exists == true)
            {
                return file;
            }

            return null;
        }

        #endregion
    }
}
