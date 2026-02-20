namespace PaymentGateway.Api.Validators
{
    public record ValidationErrorResponse(string Message, Dictionary<string, string[]> Errors);

}
