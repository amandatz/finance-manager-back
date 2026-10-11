namespace FinanceManager.Application.Users.Responses;

public sealed record CreatedUserResponse(Guid Id, string Name, string Email);
