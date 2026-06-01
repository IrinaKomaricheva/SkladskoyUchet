using System.Data;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class ReportController
{
    private readonly StockController _stockController;
    private readonly ProductController _productController;
    private readonly DocumentController _documentController;
    private readonly ReportFactory _reportFactory = new();

    public ReportController(
        StockController stockController,
        ProductController productController,
        DocumentController documentController)
    {
        _stockController = stockController;
        _productController = productController;
        _documentController = documentController;
    }

    public DataTable GetStockReport(int? warehouseId = null)
    {
        var table = new DataTable("Остатки");
        table.Columns.Add("Товар", typeof(string));
        table.Columns.Add("Артикул", typeof(string));
        table.Columns.Add("Категория", typeof(string));
        table.Columns.Add("Единица", typeof(string));
        table.Columns.Add("Склад", typeof(string));
        table.Columns.Add("Количество", typeof(decimal));
        table.Columns.Add("Резерв", typeof(decimal));
        table.Columns.Add("Доступно", typeof(decimal));
        table.Columns.Add("Закуп. цена", typeof(decimal));
        table.Columns.Add("Сумма", typeof(decimal));

        var stockItems = warehouseId.HasValue
            ? _stockController.GetStockByWarehouse(warehouseId.Value)
            : _stockController.Stock.ToList();

        foreach (var item in stockItems)
        {
            var product = _productController.GetById(item.ProductId);
            if (product == null) continue;

            table.Rows.Add(
                product.Name,
                product.SKU,
                _productController.GetCategoryName(product.CategoryId),
                product.Unit,
                _stockController.GetWarehouseName(item.WarehouseId),
                item.Quantity,
                item.Reserved,
                item.Available,
                product.PurchasePrice,
                item.Quantity * product.PurchasePrice);
        }

        return table;
    }

    public DataTable GetTurnoverReport(DateTime startDate, DateTime endDate, int? productId = null)
    {
        var table = new DataTable("Обороты");
        table.Columns.Add("Дата", typeof(DateTime));
        table.Columns.Add("Тип", typeof(string));
        table.Columns.Add("Документ", typeof(string));
        table.Columns.Add("Товар", typeof(string));
        table.Columns.Add("Артикул", typeof(string));
        table.Columns.Add("Количество", typeof(decimal));
        table.Columns.Add("Цена", typeof(decimal));
        table.Columns.Add("Сумма", typeof(decimal));

        var movements = _documentController.GetAllMovements(startDate, endDate, productId);
        foreach (var (line, date, docType, docNumber, _) in movements)
        {
            var product = _productController.GetById(line.ProductId);
            table.Rows.Add(
                date,
                docType,
                docNumber,
                product?.Name ?? "",
                product?.SKU ?? "",
                line.Quantity,
                line.UnitPrice,
                line.TotalPrice);
        }

        return table;
    }

    public DataTable GetShortageReport()
    {
        var table = new DataTable("Дефицит");
        table.Columns.Add("Товар", typeof(string));
        table.Columns.Add("Артикул", typeof(string));
        table.Columns.Add("Категория", typeof(string));
        table.Columns.Add("Склад", typeof(string));
        table.Columns.Add("Остаток", typeof(decimal));
        table.Columns.Add("Мин. уровень", typeof(decimal));
        table.Columns.Add("Необходимо", typeof(decimal));

        var products = _productController.Products.ToList();
        var lowStock = _stockController.GetLowStockItems(products);

        foreach (var item in lowStock)
        {
            var product = _productController.GetById(item.ProductId);
            if (product == null) continue;

            table.Rows.Add(
                product.Name,
                product.SKU,
                _productController.GetCategoryName(product.CategoryId),
                _stockController.GetWarehouseName(item.WarehouseId),
                item.Quantity,
                product.MinStockLevel,
                product.MinStockLevel - item.Quantity);
        }

        return table;
    }

    public DataTable GetReceiptsReport(DateTime from, DateTime to)
    {
        var table = new DataTable("Поступления");
        table.Columns.Add("Номер", typeof(string));
        table.Columns.Add("Дата", typeof(DateTime));
        table.Columns.Add("Поставщик", typeof(string));
        table.Columns.Add("Склад", typeof(string));
        table.Columns.Add("Сумма", typeof(decimal));
        table.Columns.Add("Статус", typeof(string));

        var docs = _documentController.Receipts
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderByDescending(d => d.Date);

        var suppliers = _documentController.GetType(); // dummy, we'll get supplier names separately
        foreach (var doc in docs)
        {
            var supplierName = "";
            var warehouseName = _stockController.GetWarehouseName(doc.WarehouseId);
            table.Rows.Add(
                doc.Number,
                doc.Date,
                supplierName,
                warehouseName,
                doc.TotalAmount,
                doc.Status.ToString());
        }

        return table;
    }

    public DataTable GetShipmentsReport(DateTime from, DateTime to)
    {
        var table = new DataTable("Расход");
        table.Columns.Add("Номер", typeof(string));
        table.Columns.Add("Дата", typeof(DateTime));
        table.Columns.Add("Склад", typeof(string));
        table.Columns.Add("Получатель", typeof(string));
        table.Columns.Add("Причина", typeof(string));
        table.Columns.Add("Сумма", typeof(decimal));
        table.Columns.Add("Статус", typeof(string));

        var docs = _documentController.Shipments
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderByDescending(d => d.Date);

        foreach (var doc in docs)
        {
            table.Rows.Add(
                doc.Number,
                doc.Date,
                _stockController.GetWarehouseName(doc.WarehouseId),
                doc.Recipient,
                doc.Reason,
                doc.TotalAmount,
                doc.Status.ToString());
        }

        return table;
    }

    public void ExportReport(DataTable data, string format, string filePath)
    {
        var report = _reportFactory.CreateReport(format);
        report.Generate(data, filePath);
    }
}
