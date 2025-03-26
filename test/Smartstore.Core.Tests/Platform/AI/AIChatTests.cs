using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Smartstore.Core.AI;

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

            var serializedChat = JsonConvert.SerializeObject(chat);
            var obj = JsonConvert.DeserializeObject<AIChat>(serializedChat);

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
            });
        }
    }
}
