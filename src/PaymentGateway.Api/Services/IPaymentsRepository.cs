using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public interface IPaymentsRepository
{
    Task<Payment> Add(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> Get(Guid id, CancellationToken cancellationToken = default);
}
