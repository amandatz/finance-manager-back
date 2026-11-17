using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Categories.Responses;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    CategoryType Type,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
