using FinanceManager.Application.Categories.Requests;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.Categories;

public sealed class CategoriesService(ICategoryRepository repository)
{
    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        await EnsureNameNotTakenAsync(householdId, request.Name, request.Type, excludeId: null, cancellationToken);

        var category = new Category(request.Name, request.Type, householdId);

        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(category);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CategoryType? type,
        CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);

        return categories
            .Where(x => x.IsActive)
            .Where(x => type is null || x.Type == type)
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    public async Task<CategoryResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await GetActiveOrThrowAsync(id, cancellationToken);

        return Map(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await GetActiveOrThrowAsync(id, cancellationToken);

        await EnsureNameNotTakenAsync(
            category.HouseholdId, request.Name, request.Type, excludeId: id, cancellationToken);

        category.Update(request.Name, request.Type);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await GetActiveOrThrowAsync(id, cancellationToken);

        category.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetActiveOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(id, cancellationToken);

        return category is { IsActive: true } ? category : throw NotFoundException.For(nameof(Category), id);
    }

    private async Task EnsureNameNotTakenAsync(
        Guid householdId,
        string name,
        CategoryType type,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsWithNameAndTypeAsync(householdId, name, type, excludeId, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A {type} category named '{name}' already exists.");
        }
    }

    private static CategoryResponse Map(Category category) =>
        new(
            category.Id,
            category.Name,
            category.Type,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
}
