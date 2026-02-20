using Microsoft.Extensions.Logging;

namespace PaymentGateway.Api.Services
{
    public class AcquiringBankClientFactory : IAcquiringBankClientFactory
    {
        private readonly Dictionary<string, IAcquiringBankClient> _clients;
        private readonly ILogger<AcquiringBankClientFactory> _logger;

        public AcquiringBankClientFactory(
            IEnumerable<IAcquiringBankClient> clients,
            ILogger<AcquiringBankClientFactory> logger)
        {
            _clients = clients.ToDictionary(c => c.BankName, StringComparer.OrdinalIgnoreCase);
            _logger = logger;
        }

        public IAcquiringBankClient GetClient(string bankName)
        {
            if (!_clients.TryGetValue(bankName, out var client))
            {
                _logger.LogWarning("No acquiring bank client found for '{BankName}'", bankName);
                throw new ArgumentException($"No acquiring bank client found for '{bankName}'.", nameof(bankName));
            }

            return client;
        }
    }
}
