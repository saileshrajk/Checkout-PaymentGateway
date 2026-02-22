using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.FirstAcquiringBank;
using PaymentGateway.Api.Tests.Fixtures;
using PaymentGateway.Api.Validators;

using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PaymentGateway.Api.Tests.Component
{
    public class PaymentGatewayTests : IClassFixture<PaymentGatewayFixture>
    {
        readonly HttpClient _httpClient;
        readonly PaymentGatewayFixture Fixture;
        public PaymentGatewayTests(PaymentGatewayFixture paymentGatewayFixture) : base()
        {
            Fixture = paymentGatewayFixture;
            _httpClient = Fixture.CreateClient();
        }


        [Fact]
        public async Task GivenAnAuthorizedTransaction_AllResponseValuesShouldMatch() { 
           
            //Arrange          
            var request = new PostPaymentRequest(Guid.NewGuid(), Fixture.AuthorizedCardNumber, 12, 2026, "USD", 100, "123", "FirstAcquiringBank");
            Fixture.SetupAuthorizedResponse(request, "authCodeBack");

            //Act
            var postResponse = await _httpClient.PostAsJsonAsync("/api/Payments", request);

            //Assert
            Assert.True(postResponse.IsSuccessStatusCode);

            var responseContent = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(Fixture.JsonOptions);

            Assert.NotNull(responseContent);
            Assert.Equal(PaymentStatus.Authorized, responseContent.Status);
            Assert.Equal(100, responseContent.Amount);
            Assert.Equal("USD", responseContent.Currency);
            Assert.Equal(2026, responseContent.ExpiryYear);
            Assert.Equal(12, responseContent.ExpiryMonth);
            Assert.Equal(Fixture.AuthorizedCardNumber[^4..], responseContent.CardNumberLastFour.ToString());
            Assert.Equal("authCodeBack", responseContent.AuthorizationCode);

        }

        [Fact]
        public async Task GivenAnUnAuthorizedTransaction_AllResponseValuesShouldBeCorrect()
        {

            //Arrange          
            var request = new PostPaymentRequest(Guid.NewGuid(), Fixture.UnAuthorizedCardNumber, 12, 2026, "USD", 100, "123", "FirstAcquiringBank");
            Fixture.SetupUnAuthorizedResponse(request);

            //Act
            var postResponse = await _httpClient.PostAsJsonAsync("/api/Payments", request);

            //Assert
            Assert.True(postResponse.IsSuccessStatusCode);

            var responseContent = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(Fixture.JsonOptions);

            Assert.NotNull(responseContent);
            Assert.Equal(PaymentStatus.Declined, responseContent.Status);
            Assert.Equal(100, responseContent.Amount);
            Assert.Equal("USD", responseContent.Currency);
            Assert.Equal(2026, responseContent.ExpiryYear);
            Assert.Equal(12, responseContent.ExpiryMonth);
            Assert.Equal(Fixture.UnAuthorizedCardNumber[^4..], responseContent.CardNumberLastFour.ToString());
            Assert.Null(responseContent.AuthorizationCode);

        }

        [Fact]
        public async Task GivenABadTransactionDetails_WhenPaymentRequested_ThenAuthorizationIsDeclined()
        {

            //Arrange          
            var request = new PostPaymentRequest(Guid.NewGuid(), Fixture.AuthorizedCardNumber, 12, 2025, "USD", 100, "123", "FirstAcquiringBank");
            Fixture.SetupUnAuthorizedResponse(request);

            //Act
            var postResponse = await _httpClient.PostAsJsonAsync("/api/Payments", request);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, postResponse.StatusCode);
            var reason = postResponse.ReasonPhrase;

            var responseContent = await postResponse.Content.ReadFromJsonAsync<ValidationErrorResponse>();

            Assert.Equal("Request validation failed", responseContent.Message);         

        }

        [Fact]
        public async Task GivenAcquiringBanksFailedResponse_RequestIsRetried()
        {

            // Arrange
            var request = new PostPaymentRequest(
                Guid.NewGuid(), Fixture.AuthorizedCardNumber, 12, 2026, "USD", 100, "123", "FirstAcquiringBank");

            var authorizedJson = JsonSerializer.Serialize(
                new FirstBankPaymentResponse { AuthorizationCode = "retryAuth", Authorized = true });

            // First 2 calls return 503
            Fixture._server.Given(
                Request.Create().WithPath("/payments").UsingPost()
                    .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
            )
            .InScenario("retry-test")
            .WillSetStateTo("failed-once")
            .RespondWith(Response.Create().WithStatusCode(503));

            Fixture._server.Given(
                Request.Create().WithPath("/payments").UsingPost()
                    .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
            )
            .InScenario("retry-test")
            .WhenStateIs("failed-once")
            .WillSetStateTo("failed-twice")
            .RespondWith(Response.Create().WithStatusCode(503));

            // Third call succeeds
            Fixture._server.Given(
                Request.Create().WithPath("/payments").UsingPost()
                    .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
            )
            .InScenario("retry-test")
            .WhenStateIs("failed-twice")
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(authorizedJson));

            // Act
            var postResponse = await _httpClient.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.True(postResponse.IsSuccessStatusCode);
            var responseContent = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(Fixture.JsonOptions);
            Assert.NotNull(responseContent);
            Assert.Equal(PaymentStatus.Authorized, responseContent.Status);
            Assert.Equal("retryAuth", responseContent.AuthorizationCode);
        }

        [Fact]
        public async Task GivenAcquiringBankReturns503OnAllAttempts_Returns503()
        {
            // Arrange
            var request = new PostPaymentRequest(
                Guid.NewGuid(), "7777888855559999", 12, 2026, "USD", 100, "123", "FirstAcquiringBank");

            Fixture._server.Given(
                Request.Create().WithPath("/payments").UsingPost()
                    .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
            )
            .RespondWith(Response.Create().WithStatusCode(503));

            // Act
            var postResponse = await _httpClient.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, postResponse.StatusCode);
        }
    }
}
