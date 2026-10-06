using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class FinancialAccount : Entity
{
    private FinancialAccount()
    {
    }

    public FinancialAccount(
        string name,
        FinancialAccountType type,
        string? institutionName = null)
    {
        Name = name;
        Type = type;
        InstitutionName = institutionName;
    }

    public string Name { get; private set; } = null!;
    public FinancialAccountType Type { get; private set; }
    public string? InstitutionName { get; private set; }

    public void Update(
        string name,
        string? institutionName)
    {
        Name = name;
        InstitutionName = institutionName;
        MarkAsUpdated();
    }
}