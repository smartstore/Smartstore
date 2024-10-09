using System.Runtime.CompilerServices;
using Smartstore.Core.Platform.AI.Prompting;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

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
            string html = "<div><h4>Luxuriöse Handwerkskunst und Präzision in einem Zeitmesser</h4>    <p>Bergen Sie das unvergleichliche Prestige einer Uhr, deren Herstellung sich auf eine beeindruckende Erfolgsgeschichte und Expertise stützt. Der TRANSOCEAN CHRONOGRAPH verkörpert die Essenz von Luxus und technischer Perfektion. Jedes Detail dieses Zeitmessers wird von erfahrenen Uhrmachern mit größter Sorgfalt gefertigt, was den TRANSOCEAN CHRONOGRAPH zu einem Meisterwerk der Uhrmacherkunst macht. Seine elegante Erscheinung und die präzise Funktionalität machen ihn zur ersten Wahl für Kenner und Liebhaber edler Zeitmesser.</p>    <p style=\"width:450px;\"><i class=\"far fa-xl fa-file-image ai-preview-file\" title=\"Luxuriöse Handwerkskunst und Präzision in einem Zeitmesser\"></i></p>    <h6>Elegantes Design trifft auf moderne Technologie</h6>    <p>Das Design des TRANSOCEAN CHRONOGRAPH ist eine Hommage an die klassische Eleganz, angereichert mit den neuesten technologischen Fortschritten. Das Gehäuse, gefertigt aus poliertem Edelstahl oder luxuriösem Roségold, strahlt zeitlose Schönheit aus. Das Zifferblatt besticht durch klare Linien und eine perfekte Lesbarkeit, während die Chronographen-Funktion eine präzise Zeitmessung garantiert. Die edlen Armbänder aus Leder oder Metall sorgen nicht nur für hohen Tragekomfort, sondern unterstreichen auch die erstklassige Verarbeitung.</p>    <p style=\"width:450px;\"><i class=\"far fa-xl fa-file-image ai-preview-file\" title=\"Elegantes Design trifft auf moderne Technologie\"></i></p>    <h6>Robust und zuverlässig – Der ideale Begleiter für jeden Anlass</h6>    <p>Ob im beruflichen Alltag oder bei besonderen Anlässen, der TRANSOCEAN CHRONOGRAPH ist der perfekte Begleiter für jede Situation. Seine Wasserdichtigkeit und Stoßfestigkeit gewährleisten, dass er auch unter extremen Bedingungen zuverlässig funktioniert. Dank der hochwertigen Materialien und der erstklassigen Verarbeitung bleibt diese Uhr über Jahre hinweg ein treuer Begleiter, der stets perfekt am Handgelenk sitzt und Ihre Persönlichkeit unterstreicht. Mit dem TRANSOCEAN CHRONOGRAPH können Sie sich auf höchstmögliche Zuverlässigkeit und Robustheit verlassen.</p>    <p style=\"width:450px;\"><i class=\"far fa-xl fa-file-image ai-preview-file\" title=\"Robust und zuverlässig – Der ideale Begleiter für jeden Anlass\"></i></p>    <h6>Innovatives Uhrwerk für höchste Präzision</h6>    <p>Im Herzen des TRANSOCEAN CHRONOGRAPH schlägt ein präzises und zuverlässiges Uhrwerk, das in der Lage ist, jede Sekunde genau zu messen. Dieses Kaliber steht für technische Raffinesse und höchste Fertigungsstandards. Durch die Verwendung modernster Herstellungstechniken und Materialien wird eine fortwährende Funktionstüchtigkeit und Genauigkeit gewährleistet. Die reibungslose Mechanik und die exzellente Ganggenauigkeit machen jede Bewegung des Zeigers zu einem Erlebnis und unterstreichen die Spitzenqualität, für die diese Uhr steht.</p>    <p style=\"width:450px;\"><i class=\"far fa-xl fa-file-image ai-preview-file\" title=\"Innovatives Uhrwerk für höchste Präzision\"></i></p>    <h6>Ein Statement für Stilbewusstsein und Erfolg</h6>    <p>Mit dem TRANSOCEAN CHRONOGRAPH tragen Sie nicht nur eine Uhr, sondern ein Statement für Stil, Erfolg und Engagement. Diese Uhr ist mehr als ein reiner Zeitmesser – sie ist ein Ausdruck Ihrer Persönlichkeit und Ihres anspruchsvollen Geschmacks. Ob im Meeting, auf einem gesellschaftlichen Event oder in der Freizeit, diese Uhr zieht Blicke auf sich und vermittelt eine starke Botschaft von Stilbewusstsein und Status. Setzen Sie ein Zeichen und präsentieren Sie sich mit einer Uhr, die Tradition und Innovation perfekt vereint.</p>    <p style=\"width:450px;\"><i class=\"far fa-xl fa-file-image ai-preview-file\" title=\"Ein Statement für Stilbewusstsein und Erfolg\"></i></p></div>";
            
            return times == 1 ? html : string.Concat(Enumerable.Repeat(html, (int)times));
        }

        public override RouteInfo GetDialogRoute(AIChatTopic topic)
        {
            var action = topic switch
            {
                AIChatTopic.Image => "Image",
                AIChatTopic.Text => "Text",
                AIChatTopic.RichText => "RichText",
                AIChatTopic.Translation => "Translation",
                AIChatTopic.Suggestion => "Suggestion",
                _ => throw new AIException($"Unknown chat topic {topic}.")
            };

            return new(action, "AI", new { area = "Admin" });
        }

        public override Task<string> ChatAsync(AIChat chat, CancellationToken cancelToken = default)
        {
            if (chat == null || !chat.HasMessages())
            {
                Task.FromResult((string)null);
            }

            var answer = chat.Topic == AIChatTopic.Text ? GetLoremIpsum() : GetLoremIpsumHtml();
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
            var answer = chat.Topic == AIChatTopic.Text ? GetLoremIpsum() : GetLoremIpsumHtml();

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