using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Entities;

public sealed class FinancialAccount : Entity
{
    private FinancialAccount()
    {
    }

    public const int NameMaxLength = 100;
    public const int InstitutionNameMaxLength = 150;

    public FinancialAccount(
        string name,
        FinancialAccountType type,
        string? institutionName = null)
    {
        ValidateName(name);
        ValidateInstitutionName(institutionName);
        ValidateType(type);

        Name = name;
        Type = type;
        InstitutionName = institutionName;
    }

    public string Name { get; private set; } = null!;
    public FinancialAccountType Type { get; private set; }
    public string? InstitutionName { get; private set; }

    public void Update(
        string name,
        FinancialAccountType type,
        string? institutionName)
    {
        ValidateName(name);
        ValidateInstitutionName(institutionName);
        ValidateType(type);

        Name = name;
        Type = type;
        InstitutionName = institutionName;
        MarkAsUpdated();
    }

    private static void ValidateName(string name)
    {
        Guard.AgainstEmptyOrTooLong(name, NameMaxLength, nameof(name));
    }

    private static void ValidateInstitutionName(string? institutionName)
    {
        if (institutionName is { Length: > InstitutionNameMaxLength })
        {
            throw new ArgumentException(
                $"InstitutionName must not exceed {InstitutionNameMaxLength} characters.",
                nameof(institutionName));
        }
    }

    private static void ValidateType(FinancialAccountType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException(
                "Type is not a valid FinancialAccountType.",
                nameof(type));
        }
    }
}