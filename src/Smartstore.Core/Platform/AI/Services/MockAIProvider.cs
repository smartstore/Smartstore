using System.Runtime.CompilerServices;
using Smartstore.Core.Platform.AI.Prompting;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Platform.AI
{
    [SystemName("Smartstore.AI.Mock")]
    [FriendlyName("Mock Chat")]
    public class MockAIProvider : AIProviderBase
    {
        private readonly static AIProviderFeatures _features =
            AIProviderFeatures.TextCreation |
            AIProviderFeatures.TextTranslation |
            AIProviderFeatures.ImageCreation;

        public override bool IsActive()
            => true;

        public override bool Supports(AIProviderFeatures feature)
            => _features.HasFlag(feature);

        private static string GetLoremIpsum(uint times = 1)
        {
            var str = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. " +
                "At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. " +
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. " +
                "At vero eos et accusam et justo duo dolores et ea rebum. " +
                "Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";

            return times == 1 ? str : string.Concat(Enumerable.Repeat(str, (int)times));
        }

        private static string GetLoremIpsumHtml(uint times = 1)
        {
            var title = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr";
            string html = "<div>";
            
            for (int i = 0; i < 5; i++)
            {
                var titleTag = i == 0 ? "h4" : "h6";
                html += $"<{titleTag}>{title}</{titleTag}><p>{GetLoremIpsum()}</p>";
            }
                
            html += "</div>";

            return times == 1 ? html : string.Concat(Enumerable.Repeat(html, (int)times));
        }

        public override Task<string> ChatAsync(AIChat chat, CancellationToken cancelToken = default)
        {
            if (chat == null || !chat.HasMessages())
            {
                Task.FromResult((string)null);
            }

            var answer = chat.Topic == AIChatTopic.RichText ? GetLoremIpsumHtml() : GetLoremIpsum();
            chat.Assistant(answer);

            return Task.FromResult(answer);
        }

        public override async IAsyncEnumerable<string> ChatAsStreamAsync(AIChat chat, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            if (chat == null || !chat.HasMessages())
            {
                yield break;
            }

            var newMessage = AIChatMessage.FromAssistant(null);
            var answer = chat.Topic == AIChatTopic.RichText ? GetLoremIpsumHtml() : GetLoremIpsum();

            await foreach (var substring in SplitStringAsync(answer))
            {
                newMessage.Append(substring);
                yield return substring;
            }

            chat.AddMessages(newMessage);
        }

        private static async IAsyncEnumerable<string> SplitStringAsync(string input)
        {
            var random = new Random();
            int currentIndex = 0;

            while (currentIndex < input.Length)
            {
                var length = random.Next(3, 6);

                if (currentIndex + length > input.Length)
                {
                    length = input.Length - currentIndex;
                }

                var substring = input.Substring(currentIndex, length);

                // Let's simulate the velocity of a ChatGPT language model.
                await Task.Delay(Random.Shared.Next(5, 25));
                
                await Task.Yield();

                yield return substring;

                currentIndex += length;
            }
        }

        public override async Task<string[]> CreateImagesAsync(IAIImageModel model, int numImages, CancellationToken cancelToken = default)
        {
            Guard.NotNull(model);
            Guard.NotEmpty(model.Prompt);
            Guard.IsPositive(numImages);

            var urls = new List<string>();

            for (int i = 0; i < numImages; i++)
            {
                urls.Add($"https://picsum.photos/800/600?random={i}");
            }

            await Task.Delay(1000, cancelToken);

            return [.. urls];
        }
    }
}