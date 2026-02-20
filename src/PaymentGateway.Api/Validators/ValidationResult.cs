namespace PaymentGateway.Api.Validators
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}
