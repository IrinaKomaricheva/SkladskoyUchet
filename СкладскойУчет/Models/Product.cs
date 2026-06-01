// ============================================================
// Models/Product.cs — Товар (номенклатурная позиция)
// ============================================================
// Основная сущность системы. Хранит всё, что нужно знать о
// товаре: название, штрихкод, категорию, единицы измерения,
// закупочную и продажную цены, минимальный остаток для
// уведомления о дефиците.
// ============================================================

using Newtonsoft.Json;

namespace WarehouseAccounting.Models;

public class Product
{
    public int Id { get; set; }

    /// <summary>Полное наименование товара</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Артикул / внутренний код</summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>Штрихкод EAN-13 или QR</summary>
    public string Barcode { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    /// <summary>Единица измерения: шт, кг, л, м и т.д.</summary>
    public string Unit { get; set; } = "шт";

    /// <summary>Закупочная цена (себестоимость)</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Продажная цена</summary>
    public decimal SalePrice { get; set; }

    /// <summary>Минимальный остаток — при достижении выдаётся предупреждение</summary>
    public decimal MinStockLevel { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ── Вычисляемые свойства (не сохраняются в JSON) ────────────────────────

    /// <summary>Наценка в процентах относительно закупочной цены</summary>
    [JsonIgnore]
    public decimal MarkupPercent =>
        PurchasePrice > 0 ? (SalePrice - PurchasePrice) / PurchasePrice * 100 : 0;

    public override string ToString() => $"[{SKU}] {Name}";
}
