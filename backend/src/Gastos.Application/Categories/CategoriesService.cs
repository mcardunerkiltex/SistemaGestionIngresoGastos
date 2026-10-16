using FluentValidation;
using Gastos.Application.Abstractions;
using Gastos.Application.Common;
using Gastos.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Gastos.Application.Categories;

public sealed class CategoriesService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<UpdateCategoryRequest> updateValidator)
{
    /// <summary>Árbol de categorías: las principales con sus subcategorías, ordenadas por nombre.</summary>
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CategoryType? type, CancellationToken ct)
    {
        var all = await db.Categories.Where(c => type == null || c.Type == type).OrderBy(c => c.Name).ToListAsync(ct);
        var children = all.Where(c => c.ParentId is not null).ToLookup(c => c.ParentId!.Value);
        return all.Where(c => c.ParentId is null)
            .Select(p => ToDto(p, children[p.Id].Select(c => ToDto(c, [])).ToList()))
            .ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var parent = request.ParentId is { } parentId ? await FindAsync(parentId, ct) : null;
        var category = new Category(currentUser.RequiredId(), request.Name, request.Type, parent, request.Icon ?? parent?.Icon, request.Color ?? parent?.Color);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return ToDto(category, []);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        var category = await FindAsync(id, ct);
        category.Update(request.Name, request.Icon, request.Color);
        await db.SaveChangesAsync(ct);
        return ToDto(category, []);
    }

    /// <summary>Solo se borra una categoría sin subcategorías ni movimientos; su presupuesto se borra con ella.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await FindAsync(id, ct);
        if (await db.Categories.AnyAsync(c => c.ParentId == id, ct))
            throw new ConflictException("La categoría tiene subcategorías. Borralas primero.");
        if (await db.Transactions.AnyAsync(t => t.CategoryId == id, ct))
            throw new ConflictException("La categoría tiene movimientos. Pasalos a otra categoría antes de borrarla.");

        db.Budgets.RemoveRange(await db.Budgets.Where(b => b.CategoryId == id).ToListAsync(ct));
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Category> FindAsync(Guid id, CancellationToken ct) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Categoría no encontrada.");

    private static CategoryDto ToDto(Category c, IReadOnlyList<CategoryDto> children) =>
        new(c.Id, c.ParentId, c.Name, c.Type, c.Icon, c.Color, children);
}
