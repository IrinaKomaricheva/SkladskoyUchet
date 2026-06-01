// ============================================================
// Models/Category.cs — Категория товаров
// ============================================================
// Иерархическая структура (родитель → дочерние).
// Пример: «Электроника» → «Телефоны» → «Смартфоны».
// ParentId == null означает корневую категорию.
// ============================================================

namespace WarehouseAccounting.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>null — корневая категория; иначе — ссылка на родителя</summary>
    public int? ParentId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // ── Навигационное свойство (заполняется в памяти, не в JSON) ────────────
    public List<Category> Children { get; set; } = new();

    public override string ToString() => Name;
}
