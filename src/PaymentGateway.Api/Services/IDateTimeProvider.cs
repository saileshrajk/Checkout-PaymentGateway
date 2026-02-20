namespace PaymentGateway.Api.Services
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }

    }
}
