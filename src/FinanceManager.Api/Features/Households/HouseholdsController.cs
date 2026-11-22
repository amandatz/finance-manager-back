using System.Globalization;
using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Households;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Households;

[ApiController]
[Route("api/v1/households")]
[Authorize]
public sealed class HouseholdsController(
    HouseholdsService householdsService,
    HouseholdExpenseSplitService expenseSplitService,
    ICurrentHouseholdAccessor currentHouseholdAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Create(
        CreateHouseholdRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await householdsService.CreateHouseholdAsync(GetUserId(), request.HouseholdName, cancellationToken));
    }

    [HttpPost("invite-code")]
    [Authorize(Roles = nameof(HouseholdRole.Owner))]
    public async Task<ActionResult<InviteCodeResponse>> GenerateInviteCode(CancellationToken cancellationToken)
    {
        return Ok(await householdsService.GenerateInviteCodeAsync(
            currentHouseholdAccessor.HouseholdId, cancellationToken));
    }

    [HttpPost("join")]
    public async Task<ActionResult<LoginResponse>> Join(
        JoinHouseholdRequest request,
        CancellationToken cancellationToken)
    {
        // BusinessRuleViolationException (invalid code) and ConflictException (already a
        // member) are caught by DomainExceptionHandler and mapped to 400/409 ProblemDetails
        // responses.
        return Ok(await householdsService.JoinAsync(GetUserId(), request.InviteCode, cancellationToken));
    }

    [HttpGet("expense-split")]
    public async Task<ActionResult<HouseholdExpenseSplitResponse>> GetExpenseSplit(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var (year, month) = ParsePeriod(period);

        return Ok(await expenseSplitService.GetReportAsync(
            currentHouseholdAccessor.HouseholdId, year, month, cancellationToken));
    }

    private static (int Year, int Month) ParsePeriod(string? period)
    {
        if (period is null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return (today.Year, today.Month);
        }

        if (!DateTime.TryParseExact(
                period, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new DomainValidationException("Period must be in the 'yyyy-MM' format.", "period");
        }

        return (parsed.Year, parsed.Month);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
