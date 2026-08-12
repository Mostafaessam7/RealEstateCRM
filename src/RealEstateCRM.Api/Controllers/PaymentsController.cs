using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Payments;
using RealEstateCRM.Application.Payments.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/deals/{dealId:guid}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> List(Guid dealId, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.ListForDealAsync(dealId, cancellationToken));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionDto>> CreateCheckout(Guid dealId, CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        var appUrl = _configuration["App:PublicUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        var successUrl = $"{appUrl}/deals?payment=success";
        var cancelUrl = $"{appUrl}/deals?payment=cancelled";

        var session = await _paymentService.CreateCheckoutAsync(dealId, request, successUrl, cancelUrl, cancellationToken);
        return Ok(session);
    }
}

/// <summary>Stripe calls this directly — unauthenticated by design, secured by signature verification instead.</summary>
[ApiController]
[Route("api/payments")]
public class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentWebhooksController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        await _paymentService.HandleWebhookAsync(payload, signature, cancellationToken);
        return Ok();
    }
}
