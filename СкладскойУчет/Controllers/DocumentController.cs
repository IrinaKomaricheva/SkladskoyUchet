using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class DocumentController
{
    private readonly List<ReceiptDocument> _receipts = new();
    private readonly List<ShipmentDocument> _shipments = new();
    private readonly List<TransferDocument> _transfers = new();
    private readonly List<InventoryDocument> _inventories = new();

    private readonly StockController _stockController;
    private readonly DataService _dataService = new();

    private int _receiptNext = 1;
    private int _shipmentNext = 1;
    private int _transferNext = 1;
    private int _inventoryNext = 1;

    public event EventHandler? DataChanged;
    public event EventHandler<string>? StatusChanged;

    public DocumentController(StockController stockController)
    {
        _stockController = stockController;
        LoadData();
    }

    public IReadOnlyList<ReceiptDocument> Receipts => _receipts.AsReadOnly();
    public IReadOnlyList<ShipmentDocument> Shipments => _shipments.AsReadOnly();
    public IReadOnlyList<TransferDocument> Transfers => _transfers.AsReadOnly();
    public IReadOnlyList<InventoryDocument> Inventories => _inventories.AsReadOnly();

    public ReceiptDocument CreateReceipt(int supplierId, int warehouseId)
    {
        var doc = new ReceiptDocument
        {
            Number = GenerateNumber("РП", _receiptNext++),
            Date = DateTime.Now,
            Status = DocumentStatus.Draft,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            Lines = new List<DocumentLine>()
        };
        _receipts.Add(doc);
        SaveData();
        OnStatusChanged($"Создан черновик прихода {doc.Number}");
        return doc;
    }

    public ReceiptDocument GetOrCreateDraftReceipt()
    {
        var draft = _receipts.Where(d => d.Status == DocumentStatus.Draft)
            .OrderByDescending(d => d.Id).FirstOrDefault();
        return draft ?? CreateReceipt(0, 0);
    }

    public void PostReceipt(ReceiptDocument document, int postedByUserId)
    {
        if (document.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Можно провести только черновик.");

        var errors = ValidationService.ValidateReceipt(document);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        foreach (var line in document.Lines)
            _stockController.Increase(line.ProductId, document.WarehouseId, line.Quantity);

        document.Status = DocumentStatus.Posted;
        document.PostedByUserId = postedByUserId;
        document.PostedAt = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Документ {document.Number} проведён");
    }

    public ShipmentDocument CreateShipment(int warehouseId, string recipient)
    {
        var doc = new ShipmentDocument
        {
            Number = GenerateNumber("РС", _shipmentNext++),
            Date = DateTime.Now,
            Status = DocumentStatus.Draft,
            WarehouseId = warehouseId,
            Recipient = recipient,
            Reason = "Продажа",
            Lines = new List<DocumentLine>()
        };
        _shipments.Add(doc);
        SaveData();
        OnStatusChanged($"Создан черновик расхода {doc.Number}");
        return doc;
    }

    public ShipmentDocument GetOrCreateDraftShipment()
    {
        var draft = _shipments.Where(d => d.Status == DocumentStatus.Draft)
            .OrderByDescending(d => d.Id).FirstOrDefault();
        return draft ?? CreateShipment(0, "");
    }

    public void PostShipment(ShipmentDocument document, int postedByUserId)
    {
        if (document.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Можно провести только черновик.");

        if (!document.Lines.Any())
            throw new InvalidOperationException("Добавьте хотя бы одну строку товара.");

        foreach (var line in document.Lines)
            _stockController.Decrease(line.ProductId, document.WarehouseId, line.Quantity);

        document.Status = DocumentStatus.Posted;
        document.PostedByUserId = postedByUserId;
        document.PostedAt = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Документ {document.Number} проведён");
    }

    public TransferDocument CreateTransfer(int sourceWarehouseId, int targetWarehouseId)
    {
        var doc = new TransferDocument
        {
            Number = GenerateNumber("ПМ", _transferNext++),
            Date = DateTime.Now,
            Status = DocumentStatus.Draft,
            SourceWarehouseId = sourceWarehouseId,
            TargetWarehouseId = targetWarehouseId,
            Lines = new List<DocumentLine>()
        };
        _transfers.Add(doc);
        SaveData();
        OnStatusChanged($"Создан черновик перемещения {doc.Number}");
        return doc;
    }

    public TransferDocument GetOrCreateDraftTransfer()
    {
        var draft = _transfers.Where(d => d.Status == DocumentStatus.Draft)
            .OrderByDescending(d => d.Id).FirstOrDefault();
        return draft ?? CreateTransfer(0, 0);
    }

    public void PostTransfer(TransferDocument document, int postedByUserId)
    {
        if (document.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Можно провести только черновик.");

        if (document.SourceWarehouseId == document.TargetWarehouseId)
            throw new InvalidOperationException("Склады должны различаться.");

        if (!document.Lines.Any())
            throw new InvalidOperationException("Добавьте хотя бы одну строку товара.");

        foreach (var line in document.Lines)
            _stockController.Move(line.ProductId, document.SourceWarehouseId, document.TargetWarehouseId, line.Quantity);

        document.Status = DocumentStatus.Posted;
        document.PostedByUserId = postedByUserId;
        document.PostedAt = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Документ {document.Number} проведён");
    }

    public InventoryDocument CreateInventory(int warehouseId)
    {
        var doc = new InventoryDocument
        {
            Number = GenerateNumber("ИН", _inventoryNext++),
            Date = DateTime.Now,
            Status = DocumentStatus.Draft,
            WarehouseId = warehouseId,
            Lines = new List<DocumentLine>()
        };

        var stockItems = _stockController.GetStockByWarehouse(warehouseId);
        foreach (var si in stockItems)
        {
            doc.Lines.Add(new InventoryLine
            {
                ProductId = si.ProductId,
                AccountingQuantity = si.Quantity,
                ActualQuantity = si.Quantity,
                Quantity = si.Quantity
            });
        }

        _inventories.Add(doc);
        SaveData();
        OnStatusChanged($"Создана инвентаризация {doc.Number}");
        return doc;
    }

    public void PostInventory(InventoryDocument document, int postedByUserId)
    {
        if (document.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Можно провести только черновик.");

        if (!document.Lines.Any())
            throw new InvalidOperationException("Нет строк для инвентаризации.");

        foreach (var line in document.Lines.OfType<InventoryLine>())
        {
            if (line.HasDeviation)
            {
                _stockController.SetQuantity(line.ProductId, document.WarehouseId, line.ActualQuantity);
            }
        }

        document.Status = DocumentStatus.Posted;
        document.PostedByUserId = postedByUserId;
        document.PostedAt = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Инвентаризация {document.Number} проведена");
    }

    public void CancelDocument(DocumentBase document, int cancelledByUserId)
    {
        if (document.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("Можно отменить только проведённый документ.");

        if (document is ReceiptDocument receipt)
        {
            foreach (var line in receipt.Lines)
                _stockController.Decrease(line.ProductId, receipt.WarehouseId, line.Quantity);
        }
        else if (document is ShipmentDocument shipment)
        {
            foreach (var line in shipment.Lines)
                _stockController.Increase(line.ProductId, shipment.WarehouseId, line.Quantity);
        }
        else if (document is TransferDocument transfer)
        {
            foreach (var line in transfer.Lines)
            {
                _stockController.Decrease(line.ProductId, transfer.TargetWarehouseId, line.Quantity);
                _stockController.Increase(line.ProductId, transfer.SourceWarehouseId, line.Quantity);
            }
        }
        else if (document is InventoryDocument inventory)
        {
            var stockItems = _stockController.GetStockByWarehouse(inventory.WarehouseId);
            foreach (var line in inventory.Lines.OfType<InventoryLine>())
            {
                if (line.HasDeviation)
                {
                    _stockController.SetQuantity(line.ProductId, inventory.WarehouseId, line.AccountingQuantity);
                }
            }
        }

        document.Status = DocumentStatus.Cancelled;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Документ {document.Number} отменён");
    }

    public IEnumerable<DocumentBase> GetAllPostedDocuments()
    {
        var all = new List<DocumentBase>();
        all.AddRange(_receipts.Where(d => d.IsPosted));
        all.AddRange(_shipments.Where(d => d.IsPosted));
        all.AddRange(_transfers.Where(d => d.IsPosted));
        all.AddRange(_inventories.Where(d => d.IsPosted));
        return all.OrderByDescending(d => d.PostedAt);
    }

    public List<DocumentLine> GetMovementsForProduct(int productId, int? warehouseId = null)
    {
        var result = new List<(DocumentLine Line, DateTime Date, string DocType, string DocNumber)>();

        foreach (var doc in _receipts.Where(d => d.IsPosted && (warehouseId == null || d.WarehouseId == warehouseId)))
        {
            foreach (var line in doc.Lines.Where(l => l.ProductId == productId))
                result.Add((line, doc.PostedAt ?? doc.Date, "Приход", doc.Number));
        }

        foreach (var doc in _shipments.Where(d => d.IsPosted && (warehouseId == null || d.WarehouseId == warehouseId)))
        {
            foreach (var line in doc.Lines.Where(l => l.ProductId == productId))
                result.Add((line, doc.PostedAt ?? doc.Date, "Расход", doc.Number));
        }

        foreach (var doc in _transfers.Where(d => d.IsPosted))
        {
            foreach (var line in doc.Lines.Where(l => l.ProductId == productId))
            {
                if (warehouseId == null || doc.SourceWarehouseId == warehouseId)
                    result.Add((line, doc.PostedAt ?? doc.Date, "Перемещено (списание)", doc.Number));
                if (warehouseId == null || doc.TargetWarehouseId == warehouseId)
                    result.Add((line, doc.PostedAt ?? doc.Date, "Перемещено (оприход.)", doc.Number));
            }
        }

        return result.OrderBy(r => r.Date).Select(r => r.Line).ToList();
    }

    public IEnumerable<(DocumentLine Line, DateTime Date, string DocType, string DocNumber, int? WarehouseId)> GetAllMovements(
        DateTime from, DateTime to, int? productId = null)
    {
        var result = new List<(DocumentLine, DateTime, string, string, int?)>();

        foreach (var doc in _receipts.Where(d => d.IsPosted && d.PostedAt >= from && d.PostedAt <= to))
        {
            foreach (var line in doc.Lines.Where(l => productId == null || l.ProductId == productId))
                result.Add((line, doc.PostedAt ?? doc.Date, "Приход", doc.Number, doc.WarehouseId));
        }

        foreach (var doc in _shipments.Where(d => d.IsPosted && d.PostedAt >= from && d.PostedAt <= to))
        {
            foreach (var line in doc.Lines.Where(l => productId == null || l.ProductId == productId))
                result.Add((line, doc.PostedAt ?? doc.Date, "Расход", doc.Number, doc.WarehouseId));
        }

        foreach (var doc in _transfers.Where(d => d.IsPosted && d.PostedAt >= from && d.PostedAt <= to))
        {
            foreach (var line in doc.Lines.Where(l => productId == null || l.ProductId == productId))
            {
                result.Add((line, doc.PostedAt ?? doc.Date, "Перемещение (ист.)", doc.Number, doc.SourceWarehouseId));
                result.Add((line, doc.PostedAt ?? doc.Date, "Перемещение (цель)", doc.Number, doc.TargetWarehouseId));
            }
        }

        return result.OrderBy(r => r.Item2);
    }

    public string GenerateNumber(string prefix, int id) => $"{prefix}-{id:D4}";

    public void RemoveDraft(DocumentBase doc)
    {
        switch (doc)
        {
            case ReceiptDocument r: _receipts.Remove(r); break;
            case ShipmentDocument s: _shipments.Remove(s); break;
            case TransferDocument t: _transfers.Remove(t); break;
            case InventoryDocument i: _inventories.Remove(i); break;
        }
        SaveData();
    }

    public void SaveAllDrafts() => SaveData();

    private void LoadData()
    {
        var receipts = _dataService.LoadReceipts();
        if (receipts != null) { _receipts.AddRange(receipts); _receiptNext = receipts.Count > 0 ? receipts.Max(d => ExtractId(d.Number)) + 1 : 1; }

        var shipments = _dataService.LoadShipments();
        if (shipments != null) { _shipments.AddRange(shipments); _shipmentNext = shipments.Count > 0 ? shipments.Max(d => ExtractId(d.Number)) + 1 : 1; }

        var transfers = _dataService.LoadTransfers();
        if (transfers != null) { _transfers.AddRange(transfers); _transferNext = transfers.Count > 0 ? transfers.Max(d => ExtractId(d.Number)) + 1 : 1; }

        var inventories = _dataService.LoadInventories();
        if (inventories != null) { _inventories.AddRange(inventories); _inventoryNext = inventories.Count > 0 ? inventories.Max(d => ExtractId(d.Number)) + 1 : 1; }
    }

    private static int ExtractId(string number)
    {
        var parts = number.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[1], out var id))
            return id;
        return 0;
    }

    private void SaveData()
    {
        _dataService.SaveReceipts(_receipts);
        _dataService.SaveShipments(_shipments);
        _dataService.SaveTransfers(_transfers);
        _dataService.SaveInventories(_inventories);
    }

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
