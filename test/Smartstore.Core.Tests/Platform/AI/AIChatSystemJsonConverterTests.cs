using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using NUnit.Framework;
using Smartstore.Core.AI;
using Smartstore.Imaging;
using Smartstore.Json;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.AI
{
    [TestFixture]
    public class AIChatSystemJsonConverterTests
    {
        private JsonSerializerOptions _jsonOptions;

        [SetUp]
        public void SetUp()
        {
            _jsonOptions = SmartJsonOptions.Default.Create(o =>
            {
                //o.Converters.Add(new AIChatStjConverter());
            });
        }

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

            var serializedChat = JsonSerializer.Serialize(chat, _jsonOptions);
            var obj = JsonSerializer.Deserialize<AIChat>(serializedChat, _jsonOptions);
            //serializedChat.Dump();

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
            });

            Assert.Multiple(() =>
            {
                obj.Metadata.TryGetAndConvertValue<AIImageChatContext>(KnownAIChatMetadataKeys.ImageChatContext, out var ctx);
                Assert.That(ctx.AspectRatio, Is.EqualTo(ImageAspectRatio.Ratio16x9));
                Assert.That(ctx.Resolution, Is.EqualTo(AIImageResolution.QHD));
                Assert.That(ctx.OutputFormat, Is.EqualTo(AIImageOutputFormat.Png));
                Assert.That(ctx.OutputFormat.Value.MimeType, Is.EqualTo(MediaTypeNames.Image.Png));
            });
        }

        [Test]
        public void Write_ShouldSerializeAllProperties()
        {
            // Arrange
            var chat = new AIChat(AIChatTopic.RichText);
            chat.UseModel("gpt-4");
            chat.System("System prompt")
                .User("User message")
                .System("Assistant response");

            chat.Metadata["key1"] = "value1";
            chat.Metadata["key2"] = 123;

            // Act
            var json = JsonSerializer.Serialize(chat, _jsonOptions);
            chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.Topic.ShouldEqual(AIChatTopic.RichText);
            chat.ModelName.ShouldEqual("gpt-4");
            chat.Messages.Count.ShouldEqual(3);
            chat.InitialUserMessage.ShouldBeNull();
            chat.Metadata["key1"].ShouldEqual("value1");
            chat.Metadata["key2"].ShouldEqual(123);
        }

        [Test]
        public void Write_ShouldSerializeWithoutMetadata_WhenMetadataIsEmpty()
        {
            // Arrange
            var chat = new AIChat(AIChatTopic.Text);
            chat.UseModel("gpt-3.5-turbo");
            chat.User("Test message");

            // Act
            var json = JsonSerializer.Serialize(chat, _jsonOptions);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.TryGetProperty("Metadata", out _).ShouldBeFalse();
        }

        [Test]
        public void Write_ShouldSerializeInitialUserMessageHash_WhenSet()
        {
            // Arrange
            var chat = new AIChat(AIChatTopic.Text);
            chat.UseModel("gpt-4");
            var initialMessage = chat.User("Initial user message");
            chat.Assistant("Response");
            chat.InitialUserMessage = initialMessage.Messages.FirstOrDefault();

            // Act
            var json = JsonSerializer.Serialize(chat, _jsonOptions);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            var hash = root.GetProperty("InitialUserMessageHash").GetInt32();
            hash.ShouldEqual(chat.InitialUserMessage.GetHashCode());
            hash.Equals(0).ShouldBeFalse();
        }

        [Test]
        public void Read_ShouldDeserializeAllProperties()
        {
            // Arrange
            var json = @"{
                ""Topic"": 2,
                ""ModelName"": ""gpt-4"",
                ""Messages"": [
                    {""Role"": ""system"", ""Content"": ""System prompt""},
                    {""Role"": ""user"", ""Content"": ""User message""},
                    {""Role"": ""assistant"", ""Content"": ""Assistant response""}
                ],
                ""Metadata"": {
                    ""key1"": ""value1"",
                    ""key2"": 123
                }
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.Topic.ShouldEqual(AIChatTopic.Translation);
            chat.ModelName.ShouldEqual("gpt-4");
            chat.Messages.Count.ShouldEqual(3);
            chat.Messages[0].Role.ShouldEqual(KnownAIMessageRoles.System);
            chat.Messages[0].Content.ShouldEqual("System prompt");
            chat.Messages[1].Role.ShouldEqual(KnownAIMessageRoles.User);
            chat.Messages[2].Role.ShouldEqual(KnownAIMessageRoles.Assistant);
            chat.Metadata.Count.ShouldEqual(2);

            chat.Metadata["key1"].ShouldEqual("value1");
            chat.Metadata["key2"].ShouldEqual(123);
        }

        [Test]
        public void Read_ShouldDeserializeWithMinimalData()
        {
            // Arrange
            var json = @"{
                ""Topic"": 0,
                ""ModelName"": null,
                ""Messages"": [],
                ""InitialUserMessageHash"": 0
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.Topic.ShouldEqual(AIChatTopic.Text);
            chat.ModelName.ShouldBeNull();
            chat.Messages.Count.ShouldEqual(0);
            chat.InitialUserMessage.ShouldBeNull();
        }

        [Test]
        public void Read_ShouldDeserializeWithoutMetadata()
        {
            // Arrange
            var json = @"{
                ""Topic"": 1,
                ""ModelName"": ""gpt-3.5-turbo"",
                ""Messages"": [{""Role"": ""user"", ""Content"": ""Test""}],
                ""InitialUserMessageHash"": 0
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.Metadata.Count.ShouldEqual(0);
        }

        [Test]
        public void Read_ShouldRestoreInitialUserMessage_WhenHashMatches()
        {
            // Arrange
            var originalChat = new AIChat(AIChatTopic.Text);
            originalChat.UseModel("gpt-4");
            var initialMessage = originalChat.User("Initial message");
            originalChat.Assistant("Response");
            originalChat.InitialUserMessage = initialMessage.Messages.FirstOrDefault();

            var json = JsonSerializer.Serialize(originalChat, _jsonOptions);

            // Act
            var deserializedChat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            deserializedChat.ShouldNotBeNull();
            deserializedChat.InitialUserMessage.ShouldNotBeNull();
            deserializedChat.InitialUserMessage.Content.ShouldEqual("Initial message");
            deserializedChat.InitialUserMessage.Role.ShouldEqual(KnownAIMessageRoles.User);
        }

        [Test]
        public void Read_ShouldThrowJsonException_WhenNotStartingWithObject()
        {
            // Arrange
            var json = @"[1, 2, 3]";

            // Act & Assert
            Assert.Throws<JsonException>(() => 
                JsonSerializer.Deserialize<AIChat>(json, _jsonOptions));
        }

        [Test]
        public void Read_ShouldThrowJsonException_WhenInvalidPropertyFormat()
        {
            // Arrange
            var json = @"{
                ""Topic"": 0,
                ""ModelName"": ""test"",
                123
            }";

            // Act & Assert
            Assert.Throws<JsonException>(() => 
                JsonSerializer.Deserialize<AIChat>(json, _jsonOptions));
        }

        [Test]
        public void Read_ShouldSkipUnknownProperties()
        {
            // Arrange
            var json = @"{
                ""Topic"": 0,
                ""ModelName"": ""gpt-4"",
                ""UnknownProperty"": ""should be skipped"",
                ""Messages"": [],
                ""InitialUserMessageHash"": 0,
                ""AnotherUnknown"": 42
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.ModelName.ShouldEqual("gpt-4");
        }

        [Test]
        public void Roundtrip_ShouldPreserveAllData()
        {
            // Arrange
            var originalChat = new AIChat(AIChatTopic.RichText);
            originalChat.UseModel("gpt-4-turbo");
            originalChat.System("You are a helpful assistant.");
            var userMsg = originalChat.User("Generate a description");
            originalChat.Assistant("Here is the description...");
            originalChat.InitialUserMessage = userMsg.Messages.FirstOrDefault();
            originalChat.Metadata["temperature"] = 0.7;
            originalChat.Metadata["max_tokens"] = 500;

            // Act
            var json = JsonSerializer.Serialize(originalChat, _jsonOptions);
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.Topic.ShouldEqual(AIChatTopic.RichText);
            chat.ModelName.ShouldEqual("gpt-4-turbo");
            chat.Messages.Count.ShouldEqual(3);
            chat.Messages[0].Content.ShouldEqual("You are a helpful assistant.");
            chat.Messages[1].Content.ShouldEqual("Generate a description");
            chat.Messages[2].Content.ShouldEqual("Here is the description...");
            chat.InitialUserMessage.ShouldNotBeNull();
            chat.InitialUserMessage.Content.ShouldEqual("You are a helpful assistant.");
            chat.Metadata.Count.ShouldEqual(2);
            chat.Metadata["temperature"].ShouldEqual(0.7);
            chat.Metadata["max_tokens"].ShouldEqual(500);
        }

        [Test]
        public void Roundtrip_ShouldPreserveEmptyChat()
        {
            // Arrange
            var originalChat = new AIChat(AIChatTopic.Text);

            // Act
            var json = JsonSerializer.Serialize(originalChat, _jsonOptions);
            var deserializedChat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            deserializedChat.ShouldNotBeNull();
            deserializedChat.Topic.ShouldEqual(AIChatTopic.Text);
            deserializedChat.ModelName.ShouldBeNull();
            deserializedChat.Messages.Count.ShouldEqual(0);
            deserializedChat.InitialUserMessage.ShouldBeNull();
            deserializedChat.Metadata.Count.ShouldEqual(0);
        }

        [Test]
        public void Read_ShouldHandleCaseInsensitivePropertyNames()
        {
            // Arrange
            var json = @"{
                ""topic"": 2,
                ""modelname"": ""gpt-3.5"",
                ""messages"": [{""Role"": ""user"", ""Content"": ""Test""}],
                ""initialusermessagehash"": 0
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.Topic.ShouldEqual(AIChatTopic.Translation);
            chat.ModelName.ShouldEqual("gpt-3.5");
            chat.Messages.Count.ShouldEqual(1);
        }

        [Test]
        public void Write_ShouldProduceValidJson()
        {
            // Arrange
            var chat = new AIChat(AIChatTopic.RichText);
            chat.UseModel("gpt-4");
            chat.User("Write a blog post");

            // Act
            var json = JsonSerializer.Serialize(chat, _jsonOptions);

            // Assert
            json.ShouldNotBeNull();
            
            // Verify it's valid JSON by parsing it
            Assert.DoesNotThrow(() => JsonDocument.Parse(json));
        }

        [Test]
        public void Read_ShouldHandleNullModelName()
        {
            // Arrange
            var json = @"{
                ""Topic"": 0,
                ""ModelName"": null,
                ""Messages"": [{""Role"": ""user"", ""Content"": ""Test""}],
                ""InitialUserMessageHash"": 0
            }";

            // Act
            var chat = JsonSerializer.Deserialize<AIChat>(json, _jsonOptions);

            // Assert
            chat.ShouldNotBeNull();
            chat.ModelName.ShouldBeNull();
        }

        [Test]
        public void Write_ShouldNotIncludeMetadata_WhenNull()
        {
            // Arrange
            var chat = new AIChat(AIChatTopic.Text);
            chat.UseModel("gpt-4");
            chat.User("Test");
            // Ensure metadata is empty (default state)

            // Act
            var json = JsonSerializer.Serialize(chat, _jsonOptions);

            // Assert
            json.Contains("\"Metadata\":").ShouldBeFalse();
        }
    }
}