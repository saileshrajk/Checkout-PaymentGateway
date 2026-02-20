using System.Text.RegularExpressions;

using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Validators
{
    public class PaymentValidator
    {
        private static readonly HashSet<string> SupportedCurrencies = new() { "USD", "GBP", "EUR" };
        private static readonly Regex NumericOnlyRegex = new(@"^\d+$", RegexOptions.Compiled);

        private readonly IDateTimeProvider _dateTimeProvider;

        public PaymentValidator(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public ValidationResult Validate(PostPaymentRequest paymentRequest)
        {
            var errors = new Dictionary<string, List<string>>();

            ValidateId(paymentRequest.paymentId, errors);
            ValidateCardNumber(paymentRequest.CardNumber, errors);
            ValidateExpiryDate(paymentRequest.ExpiryMonth, paymentRequest.ExpiryYear, errors);
            ValidateCurrency(paymentRequest.Currency, errors);
            ValidateAmount(paymentRequest.Amount, errors);
            ValidateCvv(paymentRequest.Cvv, errors);

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray())
            };
        }

        private void ValidateId(Guid id, Dictionary<string, List<string>> errors)
        {
            var idErrors = new List<string>();
            if (id == Guid.Empty)
            {
                idErrors.Add("Payment ID is required");
            }
            if (idErrors.Any())
            {
                errors["PaymentId"] = idErrors;
            }
        }

        private void ValidateCardNumber(string cardNumber, Dictionary<string, List<string>> errors)
        {
            var cardErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                cardErrors.Add("Card number is required");
            }
            else
            {
                if (cardNumber.Length < 14 || cardNumber.Length > 19)
                {
                    cardErrors.Add("Card number must be between 14-19 characters long");
                }

                if (!NumericOnlyRegex.IsMatch(cardNumber))
                {
                    cardErrors.Add("Card number must only contain numeric characters");
                }
            }

            if (cardErrors.Any())
            {
                errors["CardNumber"] = cardErrors;
            }
        }

        private void ValidateExpiryDate(int expiryMonth, int expiryYear, Dictionary<string, List<string>> errors)
        {
            var monthErrors = new List<string>();
            var yearErrors = new List<string>();

            if (expiryMonth < 1 || expiryMonth > 12)
            {
                monthErrors.Add("Expiry month must be between 1-12");
            }

            var now = _dateTimeProvider.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;
            var fourDigitYear = expiryYear.Get4DigitYear();

            if (fourDigitYear < currentYear)
            {
                yearErrors.Add("Expiry year must be in the future");
            }
            else if (fourDigitYear == currentYear && expiryMonth < currentMonth)
            {
                monthErrors.Add("Expiry date must be in the future");
            }

            if (monthErrors.Any())
            {
                errors["ExpiryMonth"] = monthErrors;
            }

            if (yearErrors.Any())
            {
                errors["ExpiryYear"] = yearErrors;
            }
        }

        private void ValidateCurrency(string currency, Dictionary<string, List<string>> errors)
        {
            var currencyErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(currency))
            {
                currencyErrors.Add("Currency is required");
            }
            else
            {
                if (currency.Length != 3)
                {
                    currencyErrors.Add("Currency must be 3 characters");
                }

                if (!SupportedCurrencies.Contains(currency.ToUpperInvariant()))
                {
                    currencyErrors.Add($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");
                }
            }

            if (currencyErrors.Any())
            {
                errors["Currency"] = currencyErrors;
            }
        }

        private void ValidateAmount(int amount, Dictionary<string, List<string>> errors)
        {
            var amountErrors = new List<string>();

            if (amount <= 0)
            {
                amountErrors.Add("Amount must be greater than 0");
            }

            if (amountErrors.Any())
            {
                errors["Amount"] = amountErrors;
            }
        }

        private void ValidateCvv(string cvv, Dictionary<string, List<string>> errors)
        {
            var cvvErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(cvv))
            {
                cvvErrors.Add("CVV is required");
            }
            else
            {
                if (cvv.Length < 3 || cvv.Length > 4)
                {
                    cvvErrors.Add("CVV must be 3-4 characters long");
                }

                if (!NumericOnlyRegex.IsMatch(cvv))
                {
                    cvvErrors.Add("CVV must only contain numeric characters");
                }
            }

            if (cvvErrors.Any())
            {
                errors["Cvv"] = cvvErrors;
            }
        }
    }
}
