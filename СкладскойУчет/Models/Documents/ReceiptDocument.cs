// ============================================================
// Models/Documents/ReceiptDocument.cs — Приход товара
// ============================================================
// Документ поступления товара от поставщика на склад.
// При проведении: StockItem.Quantity += Line.Quantity
// Аналог «Товарной накладной» или «Акта приёма».
// ============================================================

namespace WarehouseAccounting.Models.Documents;

public class ReceiptDocument : DocumentBase
{
    /// <summary>Поставщик, от которого пришёл товар</summary>
    public int SupplierId { get; set; }

    /// <summary>Склад-получатель</summary>
    public int WarehouseId { get; set; }

    /// <summary>Номер накладной поставщика</summary>
    public string SupplierInvoiceNumber { get; set; } = string.Empty;

    public DateTime? SupplierInvoiceDate { get; set; }
}
