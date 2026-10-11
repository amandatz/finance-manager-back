using System.Text.Json;

namespace FinanceManager.IntegrationTests;

internal static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
