using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;

using Moq;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.UnitTests;

public class PaymentValidatorTests
{
    
    readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly PaymentValidator _validator;

    public PaymentValidatorTests()
    {
        _validator = new PaymentValidator(_dateTimeProvider.Object);
        _dateTimeProvider.SetupGet(d => d.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void Validate_ValidPayment_ReturnsSuccess()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest
        (
            Guid.NewGuid(),
            "1234567890123456",
            12,
            DateTime.UtcNow.Year + 1,
            "EUR",
            1000,
            "123",
            "Test Bank"
        );
        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "Card number is required")]
    [InlineData("123", "Card number must be between 14-19 characters long")]
    [InlineData("12345678901234567890", "Card number must be between 14-19 characters long")]
    [InlineData("123456789012345a", "Card number must only contain numeric characters")]
    public void Validate_InvalidCardNumber_ReturnsError(string cardNumber, string expectedError)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), cardNumber,12,DateTime.UtcNow.Year + 1,"EUR",1000,"123","Test Bank");
        
        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors["CardNumber"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Validate_InvalidExpiryMonth_ReturnsError(int expiryMonth)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", expiryMonth, DateTime.UtcNow.Year + 1, "EUR", 1000, "123", "Test Bank");
        
        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry month must be between 1-12", result.Errors["ExpiryMonth"]);
    }

    [Fact]
    public void Validate_ExpiryYearInPast_ReturnsError()
    {        
        // Arrange
        var pastYear = DateTime.UtcNow.Year - 1;
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, pastYear, "EUR", 1000, "123", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry year must be in the future", result.Errors["ExpiryYear"]);
    }

    [Fact]
    public void Validate_ExpiryDateInPastThisYear_ReturnsError()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var pastMonth = DateTime.UtcNow.Month - 1;
        if (pastMonth < 1)
        {
            pastMonth = 1; // Skip test if we're in January
            return;
        }

        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", pastMonth, currentYear, "EUR", 1000, "123", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry date must be in the future", result.Errors["ExpiryMonth"]);
    }

    [Theory]
    [InlineData("", "Currency is required")]
    [InlineData("US", "Currency must be 3 characters")]
    [InlineData("USDA", "Currency must be 3 characters")]
    [InlineData("XXX", "Currency must be one of")]
    public void Validate_InvalidCurrency_ReturnsError(string currency, string expectedErrorContains)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, DateTime.UtcNow.Year + 1, currency, 1000, "123", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedErrorContains, result.Errors["Currency"][0]);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    [InlineData("usd")] // Should handle lowercase
    public void Validate_ValidCurrency_ReturnsSuccess(string currency)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, DateTime.UtcNow.Year + 1, currency, 1000, "123", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidAmount_ReturnsError(int amount)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, DateTime.UtcNow.Year + 1, "EUR", amount, "123", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Amount must be greater than 0", result.Errors["Amount"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    public void Validate_InvalidCvv_ReturnsError(string cvv)
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, DateTime.UtcNow.Year + 1, "EUR", 1000, cvv, "Test Bank");
        
        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("Cvv"));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Validate_ValidCvv_ReturnsSuccess(string cvv)
    {

        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "1234567890123456", 12, DateTime.UtcNow.Year + 1, "EUR", 1000, cvv, "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest(Guid.NewGuid(), "", 0, 2020, "", -1, "", "Test Bank");

        // Act
        var result = _validator.Validate(postPaymentRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5); 
    }
}
