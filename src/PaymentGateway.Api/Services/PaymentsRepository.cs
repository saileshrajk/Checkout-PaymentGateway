using System.Collections.Concurrent;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ILogger<PaymentsRepository> _logger;

    public PaymentsRepository(ILogger<PaymentsRepository> logger)
    {
        _logger = logger;
    }

    public Task<Payment> Add(Payment payment, CancellationToken cancellationToken = default)
    {
        //check for idempotency using ID as the key for now, in real world we would use a separate idempotency key
        //this logic would sit in a cache somewhere. Cache will get called first to check for idempotency and
        //if cache miss then we will call repository to add payment and then update cache with the new payment details with a ttl of say 24 hours.
        //For simplicity we are doing it in repository layer here.

        var added = false;
        var result = _payments.GetOrAdd(payment.Id, _ =>
        {
            payment.MaskCardNumber();
            added = true;
            return payment;
        });

        if (!added)
        {
            _logger.LogWarning("Idempotency duplicate detected for payment {PaymentId}", payment.Id);
        }

        return Task.FromResult(result);
    }

    public Task<Payment?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        _payments.TryGetValue(id, out var payment);

        return Task.FromResult(payment);
    }
}