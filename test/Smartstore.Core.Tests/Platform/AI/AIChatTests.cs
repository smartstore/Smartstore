using System.Linq;
using System.Net.Mime;
using Newtonsoft.Json;
using NUnit.Framework;
using Smartstore.Core.AI;
using Smartstore.Imaging;

namespace Smartstore.Core.Tests.AI
{
    [TestFixture]
    public class AIChatTests
    {
        [Test]
        public void Can_serialize_AIChat()
        {
            var initialMessage = new AIChatMessage("What is the steepest road climb in the world?", KnownAIMessageRoles.User);
            var assistantMessage = new AIChatMessage("It was nice chatting with you.", KnownAIMessageRoles.Assistant);
            var userMessage = new AIChatMessage("I have no opinion on the matter.", KnownAIMessageRoles.User);

            var chat = new AIChat(AIChatTopic.RichText)
                .UseModel("gpt-4o-mini")
                .UserTopic(initialMessage.Content)
                .AddMessages([assistantMessage, userMessage]);

            chat.SetMetaData("SomeKey", 12345);

            chat.SetMetaData(KnownAIChatMetadataKeys.ImageChatContext, new AIImageChatContext
            {
                SourceFileIds = [101, 102, 103],
                Orientation = ImageOrientation.Landscape,
                AspectRatio = ImageAspectRatio.Ratio16x9,
                Resolution = AIImageResolution.QHD,
                OutputFormat = AIImageOutputFormat.Png
            });

            var serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            var serializedChat = JsonConvert.SerializeObject(chat, serializerSettings);
            var obj = JsonConvert.DeserializeObject<AIChat>(serializedChat, serializerSettings);
            serializedChat.Dump();

            Assert.Multiple(() =>
            {
                Assert.That(obj, Is.Not.EqualTo(null));
                Assert.That(obj.Topic, Is.EqualTo(AIChatTopic.RichText));
                Assert.That(obj.ModelName, Is.EqualTo("gpt-4o-mini"));
                Assert.That(obj.Messages, Has.Count.EqualTo(3));

                Assert.That(obj.Messages.All(x => x.Content.HasValue()));

                Assert.That(obj.Messages[1], Is.EqualTo(assistantMessage));
                Assert.That(obj.Messages[2].Content, Is.EqualTo(userMessage.Content));

                Assert.That(obj.InitialUserMessage, Is.EqualTo(initialMessage));
                Assert.That(obj.InitialUserMessage.GetHashCode(), Is.EqualTo(initialMessage.GetHashCode()));

                // INFO: Internally converted into int64, so TryGetValueAs<int> would fail.
                obj.Metadata.TryGetAndConvertValue<int>("SomeKey", out var someKeyValue);
                Assert.That(someKeyValue, Is.EqualTo(12345));

                obj.Metadata.TryGetAndConvertValue<AIImageChatContext>(KnownAIChatMetadataKeys.ImageChatContext, out var ctx);
                Assert.That(ctx, Is.Not.EqualTo(null));
                Assert.That(ctx.SourceFileIds, Is.EquivalentTo([101, 102, 103]));
                Assert.That(ctx.Orientation, Is.EqualTo(ImageOrientation.Landscape));
                Assert.That(ctx.AspectRatio, Is.EqualTo(ImageAspectRatio.Ratio16x9));
                Assert.That(ctx.Resolution, Is.EqualTo(AIImageResolution.QHD));
                Assert.That(ctx.OutputFormat, Is.EqualTo(AIImageOutputFormat.Png));
                Assert.That(ctx.OutputFormat.Value.MimeType, Is.EqualTo(MediaTypeNames.Image.Png));
            });
        }
    }
}
