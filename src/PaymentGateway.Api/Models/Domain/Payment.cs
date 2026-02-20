using System.Globalization;

using PaymentGateway.Api.Extensions;

namespace PaymentGateway.Api.Models.Domain
{
    public class Payment
    {
        public Guid Id { get; private set; }
        public string CardNumber { get; private set; }
        public string LastFourDigits { get; private set; }
        public int ExpiryMonth { get; private set; }
        public int ExpiryYear { get; private set; }
        public string Currency { get; private set; }
        public int Amount { get; private set; }
        public string Cvv { get; private set; }
        public PaymentStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string ExpiryDate => new DateTime(ExpiryYear.Get4DigitYear(), ExpiryMonth, 1).ToString("MM/yyyy");
        public string? AuthorizationCode { get; private set; }


        private Payment() { }

        public static Payment Create(
            Guid id,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string currency,
            int amount,
            string cvv,
            DateTime createdAt)
        {
            return new Payment
            {
                Id = id,
                CardNumber = cardNumber,
                LastFourDigits = cardNumber[^4..],
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Currency = currency,
                Amount = amount,
                Cvv = cvv,
                Status = PaymentStatus.Pending,
                CreatedAt = createdAt
            };
        }

        public void MarkAsAuthorized(string? authorizationCode)
        {
            Status = PaymentStatus.Authorized;
            AuthorizationCode = authorizationCode;
        }

        public void MarkAsDeclined()
        {
            Status = PaymentStatus.Declined;
        }

        public void MarkAsRejected()
        {
            Status = PaymentStatus.Rejected;
        }

        public void MaskCardNumber()
        {
            CardNumber = CardNumber.MaskCardNumber();
        }
    }    
}
