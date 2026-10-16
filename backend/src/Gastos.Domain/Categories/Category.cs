using Gastos.Domain.Common;

namespace Gastos.Domain.Categories;

/// <summary>Categoría de gasto o ingreso. Dos niveles: categoría y subcategoría.</summary>
public sealed class Category : UserOwnedEntity
{
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = default!;
    public CategoryType Type { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }

    private Category() { }

    public Category(Guid userId, string name, CategoryType type, Category? parent = null, string? icon = null, string? color = null)
    {
        if (parent is not null)
        {
            if (parent.ParentId is not null) throw new DomainException("Solo se permiten dos niveles de categorías.");
            if (parent.Type != type) throw new DomainException("La subcategoría debe ser del mismo tipo que su categoría.");
        }

        UserId = userId;
        ParentId = parent?.Id;
        Rename(name);
        Type = type;
        Icon = icon;
        Color = color;
    }

    public void Update(string name, string? icon, string? color)
    {
        Rename(name);
        Icon = icon;
        Color = color;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("La categoría necesita un nombre.");
        Name = name.Trim();
    }
}
