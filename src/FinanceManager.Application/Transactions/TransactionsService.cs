using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.Transactions;

public sealed class TransactionsService(
    ITransactionRepository repository,
    IFinancialAccountRepository financialAccountRepository,
    ICategoryRepository categoryRepository)
{
    public async Task<TransactionResponse> CreateAsync(
        CreateTransactionRequest request,
        Guid householdId,
        Guid recordedByUserId,
        CancellationToken cancellationToken)
    {
        await EnsureAccountExistsAsync(request.FinancialAccountId, cancellationToken);
        var category = await GetActiveCategoryOrThrowAsync(request.CategoryId, cancellationToken);
        EnsureTypeMatchesCategory(request.Type, category);

        var transaction = new Transaction(
            request.FinancialAccountId,
            request.CategoryId,
            recordedByUserId,
            request.Type,
            request.Amount,
            request.Currency,
            request.Date,
            householdId,
            request.Description,
            request.IsHouseholdExpense);

        await repository.AddAsync(transaction, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        TransactionFilter filter,
        CancellationToken cancellationToken)
    {
        var transactions = await repository.GetAllAsync(filter, cancellationToken);

        return transactions
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<TransactionResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await GetActiveOrThrowAsync(id, cancellationToken);

        return Map(transaction);
    }

    public async Task<TransactionResponse> UpdateAsync(
        Guid id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await GetActiveOrThrowAsync(id, cancellationToken);
        var category = await GetActiveCategoryOrThrowAsync(request.CategoryId, cancellationToken);
        EnsureTypeMatchesCategory(request.Type, category);

        transaction.Update(
            request.CategoryId,
            request.Type,
            request.Amount,
            request.Currency,
            request.Date,
            request.Description,
            request.IsHouseholdExpense);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(transaction);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await GetActiveOrThrowAsync(id, cancellationToken);

        transaction.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Transaction> GetActiveOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(id, cancellationToken);

        return transaction is { IsActive: true }
            ? transaction
            : throw NotFoundException.For(nameof(Transaction), id);
    }

    private async Task EnsureAccountExistsAsync(Guid financialAccountId, CancellationToken cancellationToken)
    {
        var account = await financialAccountRepository.GetByIdAsync(financialAccountId, cancellationToken);

        if (account is not { IsActive: true })
        {
            throw NotFoundException.For(nameof(FinancialAccount), financialAccountId);
        }
    }

    private async Task<Category> GetActiveCategoryOrThrowAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        return category is { IsActive: true } ? category : throw NotFoundException.For(nameof(Category), categoryId);
    }

    private static void EnsureTypeMatchesCategory(TransactionType type, Category category)
    {
        var expectedType = type switch
        {
            TransactionType.Income => CategoryType.Income,
            TransactionType.Expense => CategoryType.Expense,
            _ => throw new DomainValidationException("Type is not a valid TransactionType.", "type"),
        };

        if (category.Type != expectedType)
        {
            throw new BusinessRuleViolationException(
                $"Transaction type '{type}' is not compatible with category type '{category.Type}'.");
        }
    }

    private static TransactionResponse Map(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.FinancialAccountId,
            transaction.CategoryId,
            transaction.RecordedByUserId,
            transaction.Type,
            transaction.Amount,
            transaction.Currency,
            transaction.Date,
            transaction.Description,
            transaction.IsHouseholdExpense,
            transaction.IsActive,
            transaction.CreatedAt,
            transaction.UpdatedAt);
}
