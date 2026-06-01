// ============================================================
// Models/Documents/DocumentBase.cs — Базовый складской документ
// ============================================================
// Все типы документов (приход, расход, перемещение,
// инвентаризация) наследуются от этого класса.
//
// Жизненный цикл документа:
//   Draft (черновик) → Posted (проведён) → Cancelled (отменён)
//
// Проведение документа изменяет остатки в StockItem.
// Отмена — откатывает изменения (паттерн Memento).
// ============================================================

namespace WarehouseAccounting.Models.Documents;

public enum DocumentStatus
{
    Draft,      // Черновик — ещё не влияет на остатки
    Posted,     // Проведён — остатки изменены
    Cancelled   // Отменён — остатки откачены назад
}

public abstract class DocumentBase
{
    public int Id { get; set; }

    /// <summary>Номер документа (генерируется автоматически)</summary>
    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>Пользователь, создавший документ</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Пользователь, проведший документ</summary>
    public int? PostedByUserId { get; set; }

    public DateTime? PostedAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    // ── Строки документа (позиции товаров) ──────────────────────────────────
    public List<DocumentLine> Lines { get; set; } = new();

    // ── Вычисляемые свойства ─────────────────────────────────────────────────

    /// <summary>Общая сумма документа</summary>
    public decimal TotalAmount => Lines.Sum(l => l.TotalPrice);

    public bool IsPosted => Status == DocumentStatus.Posted;
    public bool IsDraft => Status == DocumentStatus.Draft;
    public bool IsCancelled => Status == DocumentStatus.Cancelled;

    public override string ToString() => $"{Number} от {Date:dd.MM.yyyy}";
}

/// <summary>
/// Строка документа — одна товарная позиция
/// </summary>
public class DocumentLine
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    /// <summary>Количество по документу</summary>
    public decimal Quantity { get; set; }

    /// <summary>Цена за единицу</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Сумма строки = Количество × Цена</summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    public string Notes { get; set; } = string.Empty;
}
