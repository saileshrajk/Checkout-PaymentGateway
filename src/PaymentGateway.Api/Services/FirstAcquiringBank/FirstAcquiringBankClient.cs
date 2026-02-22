using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services.FirstAcquiringBank
{
    public class FirstAcquiringBankClient : IAcquiringBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FirstAcquiringBankClient> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public FirstAcquiringBankClient(HttpClient httpClient, 
            IDateTimeProvider dateTimeProvider,
            ILogger<FirstAcquiringBankClient> logger)
        {
            _httpClient = httpClient;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public string BankName => "FirstAcquiringBank";

        public async Task<BankResponse> ProcessPayment(Payment  payment, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new FirstBankPaymentRequest( payment.CardNumber, payment.ExpiryDate, payment.Currency, payment.Amount, payment.Cvv);
             
                var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Acquiring bank returned {StatusCode}: {Content}",
                        response.StatusCode, errorContent);
                    return BankResponse.ServiceUnavailable($"Bank returned {response.StatusCode}");                    
                }

                var firstBankResponse = await response.Content.ReadFromJsonAsync<FirstBankPaymentResponse>(cancellationToken);

                if (firstBankResponse == null)
                {
                    _logger.LogError("Failed to deserialize bank response");
                    return BankResponse.Error("Invalid response from bank");
                }

                return firstBankResponse.Status == Models.PaymentStatus.Authorized
                    ? BankResponse.Authorized(firstBankResponse.AuthorizationCode)
                    : BankResponse.Declined();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error communicating with acquiring bank");
                return BankResponse.Error($"Communication error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout communicating with acquiring bank");
                return BankResponse.Error("Request timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error communicating with acquiring bank");
                return BankResponse.Error($"Unexpected error: {ex.Message}");
            }
        }
    }

}
