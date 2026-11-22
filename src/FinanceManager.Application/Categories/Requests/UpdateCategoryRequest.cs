using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Categories.Requests;

public sealed record UpdateCategoryRequest(
    string Name,
    CategoryType Type,
    bool DefaultIsHouseholdExpense = false);
