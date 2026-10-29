using Moq;
using Moq.Protected;
using Smartstore.Klarna.Client;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Smartstore.Modules.Tests.Klarna.Client
{
    [TestFixture]
    public class KlarnaHttpClientTests
    {
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private KlarnaHttpClient _klarnaHttpClient;
        private KlarnaApiConfig _apiConfig;

        // Helper method to determine API URL based on config (example implementation)
        private string GetTestApiBaseUrl(KlarnaApiConfig config)
        {
            if (config.UseSandbox)
            {
                return config.Region switch
                {
                    "EU" => "https://api.playground.klarna.com/",
                    "NA" => "https://api-na.playground.klarna.com/",
                    "OC" => "https://api-oc.playground.klarna.com/",
                    _ => "https://api.playground.klarna.com/"
                };
            }
            else
            {
                return config.Region switch
                {
                    "EU" => "https://api.klarna.com/",
                    "NA" => "https://api-na.klarna.com/",
                    "OC" => "https://api-oc.klarna.com/",
                    _ => "https://api.klarna.com/"
                };
            }
        }

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _apiConfig = new KlarnaApiConfig
            {
                ApiKey = "test_key",
                ApiSecret = "test_secret",
                UseSandbox = true,
                Region = "EU"
                // ApiUrl will be set using GetTestApiBaseUrl
            };
            _apiConfig.ApiUrl = GetTestApiBaseUrl(_apiConfig); // Set the ApiUrl based on Region/Sandbox

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                // BaseAddress is set by KlarnaHttpClient constructor using _apiConfig.ApiUrl
            };
            _klarnaHttpClient = new KlarnaHttpClient(httpClient, _apiConfig);
        }

        [Test]
        public async Task CreateCreditSessionAsync_ShouldReturnSessionResponse_WhenApiCallIsSuccessful()
        {
            // Arrange
            var expectedResponse = new CreateSessionResponse { SessionId = "123", ClientToken = "abc" };
            var request = new CreateSessionRequest { OrderAmount = 1000, PurchaseCurrency = "EUR" };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.ToString().StartsWith(_apiConfig.ApiUrl)), // Check BaseAddress usage
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(expectedResponse)),
                })
                .Verifiable();

            // Act
            var result = await _klarnaHttpClient.CreateCreditSessionAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse.SessionId, result.SessionId);
            Assert.AreEqual(expectedResponse.ClientToken, result.ClientToken);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == (_apiConfig.ApiUrl + "payments/v1/sessions")), // Ensure full URL is correct
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public void CreateCreditSessionAsync_ShouldThrowKlarnaApiException_WhenApiReturnsError()
        {
            // Arrange
            var request = new CreateSessionRequest { OrderAmount = 1000, PurchaseCurrency = "EUR" };
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error_code\":\"BAD_VALUE\",\"error_messages\":[\"Invalid value for order_amount\"]}")
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<KlarnaApiException>(async () => await _klarnaHttpClient.CreateCreditSessionAsync(request));
            Assert.IsTrue(ex.Message.Contains("BAD_VALUE"));
        }

        // TODO: Add more tests for other KlarnaHttpClient methods (Update, Get, CreateOrder, CreateCustomerToken)
        // Test cases for different API responses (e.g., 401 Unauthorized, 403 Forbidden, 500 InternalServerError)
        // Test cases for proper request serialization and header setup (e.g., Authorization header)
    }
}
