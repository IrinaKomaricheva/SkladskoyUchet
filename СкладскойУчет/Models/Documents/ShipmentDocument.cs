// ============================================================
// Models/Documents/ShipmentDocument.cs — Расход (отгрузка) товара
// ============================================================
// Документ списания товара со склада (продажа, передача, списание).
// При проведении: StockItem.Quantity -= Line.Quantity
// Аналог «Расходной накладной».
// ============================================================

namespace WarehouseAccounting.Models.Documents;

public class ShipmentDocument : DocumentBase
{
    /// <summary>Склад-источник отгрузки</summary>
    public int WarehouseId { get; set; }

    /// <summary>Получатель: покупатель или внутреннее подразделение</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Причина расхода: Продажа, Списание, Передача и т.д.</summary>
    public string Reason { get; set; } = "Продажа";
}
