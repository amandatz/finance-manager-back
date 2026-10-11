using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.FinancialAccounts;

[ApiController]
[Route("api/v1/financial-accounts")]
public sealed class FinancialAccountsController(FinancialAccountsService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FinancialAccountResponse>> Create(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FinancialAccountResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FinancialAccountResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        // NotFoundException (thrown when the account doesn't exist or is inactive) is
        // caught by DomainExceptionHandler and mapped to a 404 ProblemDetails response.
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FinancialAccountResponse>> Update(
        Guid id,
        UpdateFinancialAccountRequest request,
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
}
