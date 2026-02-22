using System.Text.Json.Serialization;

using Microsoft.Extensions.Primitives;

namespace PaymentGateway.Api.Services.FirstAcquiringBank
{
    public record FirstBankPaymentRequest(
        [property:JsonPropertyName("card_number")]
        string CardNumber,

        [property:JsonPropertyName("expiry_date")]
        string ExpiryDate,

        [property:JsonPropertyName("currency")]
        string Currency,

        [property:JsonPropertyName("amount")]
        int Amount,

        [property:JsonPropertyName("cvv")]
        StringValues Cvv);

}