using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Repositories;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(
    FinanceManagerDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.Categories
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await context.Categories
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameAndTypeAsync(
        Guid householdId,
        string name,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var candidates = await context.Categories
            .Where(x => x.HouseholdId == householdId)
            .Where(x => x.Id != excludeId)
            .Where(x => x.IsActive && x.Type == type)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        return candidates.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken)
    {
        await context.Categories.AddAsync(
            category,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
