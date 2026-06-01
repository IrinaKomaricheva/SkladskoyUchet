// ============================================================
// Models/StockItem.cs — Позиция остатка
// ============================================================
// Связывает: Товар + Склад → Количество
// StockItem — это «пересечение» номенклатуры и склада.
//
// Логика:
//   Quantity — фактический остаток на складе
//   Reserved — зарезервировано (например, под заказ)
//   Available = Quantity - Reserved
//
// Методы изменения остатка вызываются из StockController
// при проведении документов (приход, расход, перемещение).
// ============================================================

using Newtonsoft.Json;

namespace WarehouseAccounting.Models;

public class StockItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>Фактическое количество на складе</summary>
    public decimal Quantity { get; set; }

    /// <summary>Зарезервировано под выполняемые операции</summary>
    public decimal Reserved { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    // ── Вычисляемые свойства ─────────────────────────────────────────────────

    /// <summary>Свободный остаток = Количество − Резерв</summary>
    [JsonIgnore]
    public decimal Available => Quantity - Reserved;

    /// <summary>True если остаток упал до нуля или ниже</summary>
    [JsonIgnore]
    public bool IsOutOfStock => Quantity <= 0;
}
