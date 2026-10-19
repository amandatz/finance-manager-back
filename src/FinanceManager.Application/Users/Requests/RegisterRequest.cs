namespace FinanceManager.Application.Users.Requests;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? InviteCode,
    string? HouseholdName);
