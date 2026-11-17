using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// Sensible default categories (CAT-4) created for every new household at registration time.
/// </summary>
public static class CategorySeed
{
    public static IReadOnlyList<Category> CreateDefaults(Guid householdId) =>
        DefaultNames
            .Select(x => new Category(x.Name, x.Type, householdId))
            .ToList();

    private static readonly (string Name, CategoryType Type)[] DefaultNames =
    [
        ("Salário", CategoryType.Income),
        ("Outras Receitas", CategoryType.Income),
        ("Moradia", CategoryType.Expense),
        ("Alimentação", CategoryType.Expense),
        ("Transporte", CategoryType.Expense),
        ("Saúde", CategoryType.Expense),
        ("Lazer", CategoryType.Expense),
        ("Educação", CategoryType.Expense),
        ("Outras Despesas", CategoryType.Expense),
    ];
}
