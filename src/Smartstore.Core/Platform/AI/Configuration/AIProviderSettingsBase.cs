using Smartstore.Core.Configuration;

namespace Smartstore.Core.AI
{
    public abstract class AIProviderSettingsBase : ISettings
    {
        public string ApiKey { get; set; }

        /// <summary>
        /// The maximum number of output tokens. Default is <c>null</c>. Affects the response when the token limit is reached:
        /// If <c>null</c>, the AI reduces the length of the response. Otherwise, it sends the response in chunks, which the provider combines into a single response.
        /// </summary>
        /// <remarks>
        /// The effective upper limit depends on the AI provider, model and the length of the input.
        /// For example: If the model supports a maximum of 16,384 tokens and the input contains 10,000 tokens, the response will consist of a maximum of 6,384 tokens.
        /// </remarks>
        public int? MaxCompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of completion requests per chat.
        /// This upper limit is for safety reasons to avoid infinite cycles if the AI makes a mistake.
        /// </summary>
        public int MaxCompletions { get; set; } = 25;

        // TODO: Different temperature settings for Textcreation and Translations?
        /// <summary>
        /// What sampling temperature to use, between 0 and 2.
        /// Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
        /// We generally recommend altering this or top_p but not both. 
        /// </summary>
        public float Temperature { get; set; } = 1;

        /// <summary>
        /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. 
        /// So 0.1 means only the tokens comprising the top 10% probability mass are considered.
        /// We generally recommend altering this or temperature but not both.
        /// </summary>
        public float TopP { get; set; } = 1;

        /// <summary>
        /// Gets or sets the names of the offered AI models to generate text.
        /// The available AI models depend on the used AI provider.
        /// </summary>
        /// <example>chatgpt-4o-latest</example>
        public string[] TextModelNames { get; set; }

        /// <summary>
        /// Gets or sets the names of the offered AI models to create images.
        /// The available AI models depend on the used AI provider.
        /// </summary>
        /// <example>dall-e-3</example>
        public string[] ImageModelNames { get; set; }

        /// <summary>
        /// Gets or sets the name of the AI model used to analyze images.
        /// </summary>
        /// <example>chatgpt-4o-latest</example>
        public string ImageAnalyzerModelName { get; set; }
    }
}
