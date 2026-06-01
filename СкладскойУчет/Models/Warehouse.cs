// ============================================================
// Models/Warehouse.cs — Склад (физическое место хранения)
// ============================================================
// В системе может быть несколько складов:
// «Основной», «Розничный зал», «Брак», «Транзитный» и т.д.
// StockItem связывает конкретный товар с конкретным складом
// и хранит количество.
// ============================================================

namespace WarehouseAccounting.Models;

public class Warehouse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>Ответственное лицо (кладовщик)</summary>
    public string ResponsiblePerson { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public override string ToString() => Name;
}
