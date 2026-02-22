namespace PaymentGateway.Api.Services
{
    public interface IAcquiringBankClientFactory
    {
        IAcquiringBankClient GetClient(string bankName);
    }
}