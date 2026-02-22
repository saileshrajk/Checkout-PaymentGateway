using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Pending,
    Authorized,
    Declined,
    Rejected
}