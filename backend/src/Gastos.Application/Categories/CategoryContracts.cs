using Gastos.Domain.Categories;

namespace Gastos.Application.Categories;

public sealed record CategoryDto(Guid Id, Guid? ParentId, string Name, CategoryType Type, string? Icon, string? Color, IReadOnlyList<CategoryDto> Children);

public sealed record CreateCategoryRequest(string Name, CategoryType Type, Guid? ParentId, string? Icon, string? Color);

public sealed record UpdateCategoryRequest(string Name, string? Icon, string? Color);
