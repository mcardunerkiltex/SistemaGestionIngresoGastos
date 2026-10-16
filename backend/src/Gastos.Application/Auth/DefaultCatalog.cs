using Gastos.Domain.Accounts;
using Gastos.Domain.Categories;

namespace Gastos.Application.Auth;

/// <summary>Cuenta y categorías con las que arranca un usuario nuevo. Todas se pueden editar.</summary>
internal static class DefaultCatalog
{
    private static readonly (string Name, string Icon, string Color, string[] Children)[] Expenses =
    [
        ("Vivienda", "home", "#5C6BC0", ["Alquiler", "Expensas", "Mantenimiento"]),
        ("Servicios", "bolt", "#FFA726", ["Luz", "Gas", "Agua", "Internet", "Celular"]),
        ("Comida", "restaurant", "#66BB6A", ["Supermercado", "Delivery", "Restaurantes"]),
        ("Transporte", "directions_car", "#42A5F5", ["Combustible", "Transporte público", "Peajes y estacionamiento"]),
        ("Salud", "favorite", "#EF5350", ["Prepaga", "Farmacia"]),
        ("Educación", "school", "#AB47BC", []),
        ("Ocio", "sports_esports", "#26C6DA", ["Salidas", "Suscripciones", "Viajes"]),
        ("Ropa", "checkroom", "#EC407A", []),
        ("Impuestos", "receipt", "#8D6E63", []),
        ("Otros gastos", "more_horiz", "#78909C", []),
    ];

    private static readonly (string Name, string Icon, string Color)[] Incomes =
    [
        ("Sueldo", "work", "#2E7D32"),
        ("Trabajos extra", "handyman", "#00897B"),
        ("Otros ingresos", "add_circle", "#558B2F"),
    ];

    public static Account DefaultAccount(Guid userId) => new(userId, "Efectivo", AccountType.Cash, 0m);

    public static IEnumerable<Category> Categories(Guid userId)
    {
        foreach (var (name, icon, color, children) in Expenses)
        {
            var parent = new Category(userId, name, CategoryType.Expense, icon: icon, color: color);
            yield return parent;
            foreach (var child in children)
                yield return new Category(userId, child, CategoryType.Expense, parent, icon, color);
        }

        foreach (var (name, icon, color) in Incomes)
            yield return new Category(userId, name, CategoryType.Income, icon: icon, color: color);
    }
}
