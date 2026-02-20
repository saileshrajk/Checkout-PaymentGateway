using System.Globalization;

namespace PaymentGateway.Api.Extensions
{
    public static class DateExtension
    {
        public static int Get4DigitYear(this int year)
        {
            return CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(year);
        }
    }
}
