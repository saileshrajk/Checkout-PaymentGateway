using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.FirstAcquiringBank;

using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PaymentGateway.Api.Tests.Fixtures
{
    public class PaymentGatewayFixture : WebApplicationFactory<PaymentsController>
    {
        public WireMockServer _server;

        public string AuthorizedCardNumber => "4111111111111111";
        public string UnAuthorizedCardNumber => "4222222222222222";

        public PaymentGatewayFixture()
        {
            _server = WireMockServer.Start(8888);
        }       
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("testappsettings.json", optional: false);
            });
        }

        public void SetupAuthorizedResponse(PostPaymentRequest request, string authCode)
        {
            var expectedJson = JsonSerializer.Serialize(new FirstBankPaymentResponse() { AuthorizationCode = authCode, Authorized = true });
            _server.Given(
             Request.Create().WithPath("/payments")
             .UsingPost()
             .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
         ).RespondWith(
             Response.Create()
                 .WithStatusCode(200)
                 .WithHeader("Content-Type", "application/json")
                 .WithBody(expectedJson)
         );
        }

        public void SetupUnAuthorizedResponse(PostPaymentRequest request)
        {
            var expectedJson = JsonSerializer.Serialize(new FirstBankPaymentResponse() { Authorized = false });

            _server.Given(
             Request.Create().WithPath("/payments")
             .UsingPost()
             .WithBody(new JsonPathMatcher($"$..[?(@.card_number == '{request.CardNumber}')]"))
         ).RespondWith(
             Response.Create()
                 .WithStatusCode(200)
                 .WithHeader("Content-Type", "application/json")
                 .WithBody(expectedJson)
         );
        }    
        

    }
}
