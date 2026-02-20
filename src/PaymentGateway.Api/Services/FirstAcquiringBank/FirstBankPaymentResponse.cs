using System.Text.Json.Serialization;
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services.FirstAcquiringBank;

public class FirstBankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }
    public PaymentStatus Status => Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }
}