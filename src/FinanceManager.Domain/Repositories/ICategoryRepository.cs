using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAndTypeAsync(
        Guid householdId,
        string name,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
