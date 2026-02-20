namespace PaymentGateway.Api.Services
{
    public class BankResponse
    {
        public bool IsSuccess { get; private set; }
        public bool IsAuthorized { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? AuthorizationCode { get; private set; }

        public static BankResponse Authorized(string? authorizationCode)
        {
            return new BankResponse
            {
                IsSuccess = true,
                IsAuthorized = true,
                AuthorizationCode = authorizationCode
            };
        }

        public static BankResponse Declined()
        {
            return new BankResponse
            {
                IsSuccess = true,
                IsAuthorized = false
            };
        }

        public static BankResponse ServiceUnavailable()
        {
            return new BankResponse
            {
                IsSuccess = false,
                ErrorMessage = "Bank service unavailable"
            };
        }

        public static BankResponse Error(string message)
        {
            return new BankResponse
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }
}
