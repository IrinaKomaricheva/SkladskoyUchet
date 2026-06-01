// ============================================================
// Models/Documents/InventoryDocument.cs — Инвентаризация
// ============================================================
// Документ сверки фактических остатков с учётными.
// Алгоритм работы:
//   1. Создать документ — система заполняет строки текущими остатками
//   2. Кладовщик вводит фактическое количество (ActualQuantity)
//   3. Провести — система корректирует остатки по результатам сверки
//
// Отклонение (Deviation) = Фактическое − Учётное:
//   > 0 — излишек (приходуется)
//   < 0 — недостача (списывается)
// ============================================================

namespace WarehouseAccounting.Models.Documents;

public class InventoryDocument : DocumentBase
{
    /// <summary>Склад, на котором проводится инвентаризация</summary>
    public int WarehouseId { get; set; }
}

/// <summary>
/// Строка инвентаризации — расширяет базовую DocumentLine
/// </summary>
public class InventoryLine : DocumentLine
{
    /// <summary>Учётное количество (из базы данных на момент инвентаризации)</summary>
    public decimal AccountingQuantity { get; set; }

    /// <summary>Фактическое количество (результат пересчёта)</summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>Отклонение = Фактическое − Учётное</summary>
    public decimal Deviation => ActualQuantity - AccountingQuantity;

    /// <summary>True если отклонение есть</summary>
    public bool HasDeviation => Deviation != 0;
}
