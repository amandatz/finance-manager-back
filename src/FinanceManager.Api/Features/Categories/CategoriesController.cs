using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Categories;
using FinanceManager.Application.Categories.Requests;
using FinanceManager.Application.Categories.Responses;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Features.Categories;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public sealed class CategoriesController(
    CategoriesService service,
    ICurrentHouseholdAccessor currentHouseholdAccessor) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(
            request, currentHouseholdAccessor.HouseholdId, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(
        [FromQuery] CategoryType? type,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(type, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
