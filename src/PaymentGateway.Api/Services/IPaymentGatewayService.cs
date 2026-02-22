using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services
{
    public interface IPaymentGatewayService
    {
        Task<PaymentResult> ProcessPayment(
            PostPaymentRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentResult> GetPayment(Guid id, CancellationToken cancellationToken = default);
    }
}