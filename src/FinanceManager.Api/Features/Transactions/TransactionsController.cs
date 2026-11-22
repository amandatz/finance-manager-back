using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Transactions;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Transactions;

[ApiController]
[Route("api/v1/transactions")]
[Authorize]
public sealed class TransactionsController(
    TransactionsService service,
    ICurrentHouseholdAccessor currentHouseholdAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(
            request, currentHouseholdAccessor.HouseholdId, GetUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? creditCardId,
        [FromQuery] Guid? categoryId,
        [FromQuery] TransactionType? type,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var filter = new TransactionFilter(accountId, creditCardId, categoryId, type, dateFrom, dateTo);

        return Ok(await service.GetAllAsync(filter, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Update(
        Guid id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
