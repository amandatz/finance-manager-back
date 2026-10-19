using System.Security.Cryptography;
using FinanceManager.Domain.Common;

namespace FinanceManager.Domain.Entities;

public sealed class Household : Entity
{
    public const int NameMaxLength = 100;
    public const int InviteCodeLength = 8;

    private const string InviteCodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private Household()
    {
    }

    public Household(string name)
    {
        ValidateName(name);

        Name = name;
    }

    public string Name { get; private set; } = null!;

    public string? InviteCode { get; private set; }

    public string GenerateInviteCode()
    {
        InviteCode = CreateRandomCode();
        MarkAsUpdated();

        return InviteCode;
    }

    public void ConsumeInviteCode()
    {
        InviteCode = null;
        MarkAsUpdated();
    }

    private static void ValidateName(string name)
    {
        Guard.AgainstEmptyOrTooLong(name, NameMaxLength, nameof(name));
    }

    private static string CreateRandomCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(InviteCodeLength);
        var chars = new char[InviteCodeLength];

        for (var i = 0; i < InviteCodeLength; i++)
        {
            chars[i] = InviteCodeAlphabet[bytes[i] % InviteCodeAlphabet.Length];
        }

        return new string(chars);
    }
}
