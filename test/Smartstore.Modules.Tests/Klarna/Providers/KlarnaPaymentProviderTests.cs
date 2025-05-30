using Moq;
using Smartstore.Klarna.Client;
using Smartstore.Klarna.Configuration;
using Smartstore.Klarna.Providers;
using NUnit.Framework;
using System.Threading.Tasks;
using Smartstore.Checkout.Payment;
using Smartstore.Core.Checkout.Orders; // For Order
// using Smartstore.Core.Common.Domain; // For Currency - not directly needed as Order has currency
using System.Collections.Generic; // For Dictionary

namespace Smartstore.Modules.Tests.Klarna.Providers
{
    [TestFixture]
    public class KlarnaPaymentProviderTests
    {
        private Mock<KlarnaHttpClient> _mockKlarnaHttpClient;
        private KlarnaPaymentProvider _klarnaPaymentProvider;
        private KlarnaApiConfig _klarnaApiConfig; // This is the config for the Klarna *module*, not directly for HttpClient mock here

        [SetUp]
        public void Setup()
        {
            // For mocking KlarnaHttpClient, we need to provide its dependencies.
            // Since we are mocking its methods, the actual dependencies might not be used by the mock itself.
            // However, it's cleaner to provide mock instances if the constructor demands non-nulls.
            var mockHttpInnerClient = new Mock<HttpClient>(); // Dummy, not used by the mocked KlarnaHttpClient methods
            var mockApiConfigForHttpClient = new Mock<KlarnaApiConfig>(); // Dummy

            _mockKlarnaHttpClient = new Mock<KlarnaHttpClient>(mockHttpInnerClient.Object, mockApiConfigForHttpClient.Object);

            // This _klarnaApiConfig is the one used by KlarnaPaymentProvider itself, not the one passed to KlarnaHttpClient's constructor.
            // In a real DI scenario, KlarnaPaymentProvider would receive KlarnaApiConfig (derived from KlarnaSettings).
            _klarnaApiConfig = new KlarnaApiConfig { ApiKey = "cfg_key", ApiSecret = "cfg_secret", UseSandbox = true, Region = "EU", ApiUrl = "http://dummyurl.com"};

            _klarnaPaymentProvider = new KlarnaPaymentProvider(_mockKlarnaHttpClient.Object, _klarnaApiConfig);
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnPendingAndClientToken_WhenSessionCreationIsSuccessful()
        {
            // Arrange
            var order = new Order { OrderTotal = 100m, CustomerCurrencyCode = "EUR" };
            var processPaymentRequest = new ProcessPaymentRequest
            {
                Order = order,
                CustomProperties = new Dictionary<string, object>() // Initialize CustomProperties
            };
            var sessionResponse = new CreateSessionResponse { SessionId = "test_sid", ClientToken = "test_ctoken" };

            _mockKlarnaHttpClient
                .Setup(c => c.CreateCreditSessionAsync(It.IsAny<CreateSessionRequest>()))
                .ReturnsAsync(sessionResponse);

            // Act
            var result = await _klarnaPaymentProvider.ProcessPaymentAsync(processPaymentRequest);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(PaymentStatus.Pending, result.NewPaymentStatus);
            Assert.AreEqual("test_sid", processPaymentRequest.CustomProperties["KlarnaSessionId"]);
            Assert.AreEqual("test_ctoken", processPaymentRequest.CustomProperties["KlarnaClientToken"]);
            _mockKlarnaHttpClient.Verify(c => c.CreateCreditSessionAsync(It.Is<CreateSessionRequest>(req =>
                req.OrderAmount == 10000 && // 100m * 100
                req.PurchaseCurrency == "EUR"
                // TODO: Add more assertions for CreateSessionRequest properties if they are set in provider
            )), Times.Once);
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnError_WhenSessionCreationFails()
        {
            // Arrange
             var order = new Order { OrderTotal = 100m, CustomerCurrencyCode = "EUR" };
            var processPaymentRequest = new ProcessPaymentRequest
            {
                Order = order,
                CustomProperties = new Dictionary<string, object>() // Initialize
            };
            _mockKlarnaHttpClient
                .Setup(c => c.CreateCreditSessionAsync(It.IsAny<CreateSessionRequest>()))
                .ThrowsAsync(new KlarnaApiException("Session creation failed"));

            // Act
            var result = await _klarnaPaymentProvider.ProcessPaymentAsync(processPaymentRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors[0].Contains("Klarna API Error: Session creation failed"));
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnError_WhenSessionResponseIsNull()
        {
            // Arrange
            var order = new Order { OrderTotal = 100m, CustomerCurrencyCode = "EUR" };
            var processPaymentRequest = new ProcessPaymentRequest
            {
                Order = order,
                CustomProperties = new Dictionary<string, object>()
            };

            _mockKlarnaHttpClient
                .Setup(c => c.CreateCreditSessionAsync(It.IsAny<CreateSessionRequest>()))
                .ReturnsAsync((CreateSessionResponse)null); // Simulate null response

            // Act
            var result = await _klarnaPaymentProvider.ProcessPaymentAsync(processPaymentRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.AreEqual("Failed to create Klarna payment session.", result.Errors[0]);
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnError_WhenClientTokenIsNullOrEmpty()
        {
            // Arrange
            var order = new Order { OrderTotal = 100m, CustomerCurrencyCode = "EUR" };
            var processPaymentRequest = new ProcessPaymentRequest
            {
                Order = order,
                CustomProperties = new Dictionary<string, object>()
            };
            var sessionResponse = new CreateSessionResponse { SessionId = "test_sid", ClientToken = "" }; // Empty client token

            _mockKlarnaHttpClient
                .Setup(c => c.CreateCreditSessionAsync(It.IsAny<CreateSessionRequest>()))
                .ReturnsAsync(sessionResponse);

            // Act
            var result = await _klarnaPaymentProvider.ProcessPaymentAsync(processPaymentRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.AreEqual("Failed to create Klarna payment session.", result.Errors[0]);
        }


        // TODO: Add tests for CaptureAsync (successful capture, failed capture)
        // TODO: Add tests for RefundAsync (successful refund, failed refund)
        // TODO: Add tests for VoidAsync (successful void, failed void)
        // TODO: Test GetControllerType, GetConfigurationRouteName, GetPublicViewComponentName return expected values
        // TODO: Test interaction with KlarnaApiConfig settings (e.g., how they affect requests made by the provider)
    }
}
