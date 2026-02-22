namespace PaymentGateway.Api.Extensions
{
    public static class CardExtension
    {
        public static string MaskCardNumber(this string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            {
                return cardNumber;
            }
            var lastFourDigits = cardNumber[^4..];
            var maskedSection = new string('*', cardNumber.Length - 4);
            return maskedSection + lastFourDigits;
        }
    }
}