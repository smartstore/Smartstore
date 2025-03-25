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
            AIChatMessage[] messages =
            [
                new("Hello world!", KnownAIMessageRoles.User),
                new("It was nice chatting with you.", KnownAIMessageRoles.Assistant),
                new("I have no opinion on the matter.", KnownAIMessageRoles.User)
            ];

            var chat = new AIChat(AIChatTopic.RichText);
            chat.UseModel("gpt-4o-mini")
                .AddMessages(messages);

            var serializedChat = JsonConvert.SerializeObject(chat);
            var obj = JsonConvert.DeserializeObject<AIChat>(serializedChat);

            Assert.Multiple(() =>
            {
                Assert.That(obj, Is.Not.EqualTo(null));
                Assert.That(obj.Topic, Is.EqualTo(AIChatTopic.RichText));
                Assert.That(obj.ModelName, Is.EqualTo("gpt-4o-mini"));
                Assert.That(obj.Messages, Has.Count.EqualTo(3));

                Assert.That(obj.Messages.All(x => x.Content.HasValue()));

                Assert.That(obj.Messages[1], Is.EqualTo(messages[1]));
                Assert.That(obj.Messages[2].Content, Is.EqualTo(messages[2].Content));
            });
        }
    }
}
