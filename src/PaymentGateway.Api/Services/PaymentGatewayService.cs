using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Services
{

    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IPaymentsRepository _repository;
        private readonly IAcquiringBankClientFactory _bankClientFactory;
        private readonly PaymentValidator _validator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<PaymentGatewayService> _logger;

        const string DefaultAcquiringBankName = "FirstAcquiringBank";

        public PaymentGatewayService(
            IPaymentsRepository repository,
            IAcquiringBankClientFactory bankClientFactory,
            PaymentValidator validator,
            IDateTimeProvider dateTimeProvider,
            ILogger<PaymentGatewayService> logger)
        {
            _repository = repository;
            _bankClientFactory = bankClientFactory;
            _validator = validator;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPayment(
            PostPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            // Validate the request
            var validationResult = _validator.Validate(request);

            var payment = Payment.Create(
                request.paymentId,
                request.CardNumber,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.Currency.ToUpperInvariant(),
                request.Amount,
                request.Cvv,
                _dateTimeProvider.UtcNow);

            _logger.LogInformation("Payment processing started for {PaymentId}", payment.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for payment {PaymentId}", payment.Id);

                payment.MarkAsRejected();

                _ = await _repository.Add(payment, cancellationToken);

                return PaymentResult.ValidationFailed(validationResult.Errors);
            }

            var acquiringBankName = request.AcquiringBankName?.Trim() ?? DefaultAcquiringBankName;

            var bankClient = _bankClientFactory.GetClient(acquiringBankName);
            var bankResponse = await bankClient.ProcessPayment(payment, cancellationToken);

            if (!bankResponse.IsSuccess)
            {
                _logger.LogWarning("Bank error for payment {PaymentId}: {ErrorMessage}", payment.Id, bankResponse.ErrorMessage);
                return PaymentResult.BankError(bankResponse.ErrorMessage ?? "Unknown bank error");
            }

            if (bankResponse.IsAuthorized)
            {
                payment.MarkAsAuthorized(bankResponse.AuthorizationCode);
                _logger.LogInformation("Payment {PaymentId} authorized", payment.Id);
            }
            else
            {
                payment.MarkAsDeclined();
                _logger.LogInformation("Payment {PaymentId} declined", payment.Id);
            }

            var paymentResponse = await _repository.Add(payment, cancellationToken);

            return PaymentResult.Success(paymentResponse);
        }

        public async Task<PaymentResult> GetPayment(Guid id, CancellationToken cancellationToken = default)
        {
            var payment = await _repository.Get(id, cancellationToken);

            return payment == null ? PaymentResult.NotFound() : PaymentResult.Success(payment);
        }
    }

    public class PaymentResult
    {
        public bool IsSuccess { get; private set; }
        public Payment? Data { get; private set; }
        public PaymentResultType ResultType { get; private set; }
        public Dictionary<string, string[]>? ValidationErrors { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static PaymentResult Success(Payment data)
        {
            return new PaymentResult
            {
                IsSuccess = true,
                Data = data,
                ResultType = PaymentResultType.Success
            };
        }

        public static PaymentResult ValidationFailed(Dictionary<string, string[]> errors)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ResultType = PaymentResultType.ValidationFailed,
                ValidationErrors = errors
            };
        }

        public static PaymentResult BankError(string errorMessage)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ResultType = PaymentResultType.BankError,
                ErrorMessage = errorMessage
            };
        }

        public static PaymentResult NotFound()
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ResultType = PaymentResultType.NotFound
            };
        }
    }
}