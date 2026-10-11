namespace FinanceManager.Application.Users.Requests;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
