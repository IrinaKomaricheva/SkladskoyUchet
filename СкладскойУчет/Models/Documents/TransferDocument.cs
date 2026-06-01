// ============================================================
// Models/Documents/TransferDocument.cs — Перемещение между складами
// ============================================================
// Документ внутреннего перемещения: товар списывается с одного
// склада и приходуется на другой.
// При проведении:
//   SourceWarehouse.StockItem.Quantity -= Line.Quantity
//   TargetWarehouse.StockItem.Quantity += Line.Quantity
// ============================================================

namespace WarehouseAccounting.Models.Documents;

public class TransferDocument : DocumentBase
{
    /// <summary>Склад-источник (откуда перемещают)</summary>
    public int SourceWarehouseId { get; set; }

    /// <summary>Склад-приёмник (куда перемещают)</summary>
    public int TargetWarehouseId { get; set; }
}
