namespace PaymentGateway.Api.Models.Requests;

public record PostPaymentRequest(Guid paymentId, string CardNumber, int ExpiryMonth, int ExpiryYear, string Currency, int Amount, string Cvv, string AcquiringBankName);