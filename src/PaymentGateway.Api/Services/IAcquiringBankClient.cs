using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services
{
    public interface IAcquiringBankClient
    {
        string BankName { get; }
        Task<BankResponse> ProcessPayment(
           Payment payment,
           CancellationToken cancellationToken = default);
    }
}