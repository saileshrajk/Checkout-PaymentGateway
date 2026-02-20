using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentGatewayService paymentGatewayService,
        ILogger<PaymentsController> logger)
    {
        _paymentGatewayService = paymentGatewayService;
        _logger = logger;
    }

    /// <summary>
    /// Process a new payment
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment result with status</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/payments request received");
        var result = await _paymentGatewayService.ProcessPayment(request, cancellationToken);

        return result.ResultType switch
        {
            PaymentResultType.Success => Ok(MapToPostPaymentResponse(result.Data)),
            PaymentResultType.ValidationFailed => BadRequest(new ValidationErrorResponse(
                "Request validation failed",
                result.ValidationErrors!)),
            PaymentResultType.BankError => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Retrieve details of a previously made payment
    /// </summary>
    /// <param name="id">Payment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/payments/{PaymentId} request received", id);
        var result = await _paymentGatewayService.GetPayment(id, cancellationToken);

        if (result.ResultType == PaymentResultType.NotFound)
        {
            _logger.LogWarning("Payment {PaymentId} not found", id);
        }

        return result.ResultType switch
        {
            PaymentResultType.Success => Ok(MapToGetPaymentResponse(result.Data)),
            PaymentResultType.NotFound => NotFound(new { message = $"Payment with ID {id} not found" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private PostPaymentResponse MapToPostPaymentResponse(Payment? data)
    {
        return new PostPaymentResponse
        {
            Id = data!.Id,
            Status = data.Status,
            CardNumberLastFour = int.Parse(data.LastFourDigits),
            ExpiryMonth = data.ExpiryMonth,
            ExpiryYear = data.ExpiryYear,
            Currency = data.Currency,
            Amount = data.Amount,
            AuthorizationCode = data.AuthorizationCode
        };  
    }

    private GetPaymentResponse MapToGetPaymentResponse(Payment? data)
    {
       return new GetPaymentResponse
        {
            Id = data!.Id,
            Status = data.Status,
            CardNumberLastFour = int.Parse(data.LastFourDigits),
            ExpiryMonth = data.ExpiryMonth,
            ExpiryYear = data.ExpiryYear,
            Currency = data.Currency,
            Amount = data.Amount
        };
    }
}