namespace FinanceManager.Application.Users.Requests;

public sealed record CreateMemberRequest(string Name, string Email, string Password);
