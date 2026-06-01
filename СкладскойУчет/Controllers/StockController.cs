using System.ComponentModel;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class StockController
{
    private readonly BindingList<StockItem> _stock = new();
    private readonly BindingList<Warehouse> _warehouses = new();
    private readonly DataService _dataService = new();
    private int _nextId = 1;

    public event EventHandler? DataChanged;
    public event EventHandler<string>? StatusChanged;

    public BindingList<StockItem> Stock => _stock;
    public BindingList<Warehouse> Warehouses => _warehouses;

    public StockController()
    {
        LoadData();
    }

    public void Increase(int productId, int warehouseId, decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Количество должно быть больше нуля.");

        var item = _stock.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        if (item == null)
        {
            item = new StockItem
            {
                Id = _nextId++,
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0
            };
            _stock.Add(item);
        }

        item.Quantity += quantity;
        item.LastUpdated = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Остаток увеличен: товар #{productId}, склад #{warehouseId}, +{quantity}");
    }

    public void Decrease(int productId, int warehouseId, decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Количество должно быть больше нуля.");

        var item = _stock.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        if (item == null)
            throw new InvalidOperationException("Товар отсутствует на складе.");

        if (item.Available < quantity)
            throw new InvalidOperationException(
                $"Недостаточно товара на складе. Доступно: {item.Available}, требуется: {quantity}");

        item.Quantity -= quantity;
        item.LastUpdated = DateTime.Now;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Остаток уменьшен: товар #{productId}, склад #{warehouseId}, -{quantity}");
    }

    public void Move(int productId, int sourceWarehouseId, int targetWarehouseId, decimal quantity)
    {
        if (sourceWarehouseId == targetWarehouseId)
            throw new InvalidOperationException("Склад-источник и склад-приёмник должны различаться.");

        Decrease(productId, sourceWarehouseId, quantity);
        Increase(productId, targetWarehouseId, quantity);
        OnStatusChanged($"Перемещено: товар #{productId}, {sourceWarehouseId} → {targetWarehouseId}, {quantity}");
    }

    public void SetQuantity(int productId, int warehouseId, decimal newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Количество не может быть отрицательным.");

        var item = _stock.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        if (item == null)
        {
            item = new StockItem
            {
                Id = _nextId++,
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0
            };
            _stock.Add(item);
        }

        item.Quantity = newQuantity;
        item.LastUpdated = DateTime.Now;
        SaveData();
        OnDataChanged();
    }

    public decimal GetQuantity(int productId, int warehouseId)
    {
        return _stock
            .FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId)
            ?.Quantity ?? 0;
    }

    public decimal GetAvailable(int productId, int warehouseId)
    {
        return _stock
            .FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId)
            ?.Available ?? 0;
    }

    public List<StockItem> GetLowStockItems(IEnumerable<Product> products)
    {
        var productDict = products.ToDictionary(p => p.Id, p => p.MinStockLevel);
        return _stock
            .Where(s => productDict.TryGetValue(s.ProductId, out var min) && s.Quantity <= min)
            .ToList();
    }

    public List<StockItem> GetStockByProduct(int productId)
    {
        return _stock.Where(s => s.ProductId == productId).ToList();
    }

    public List<StockItem> GetStockByWarehouse(int warehouseId)
    {
        return _stock.Where(s => s.WarehouseId == warehouseId).ToList();
    }

    public StockItem? GetStockItem(int productId, int warehouseId)
    {
        return _stock.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);
    }

    public string GetWarehouseName(int warehouseId)
    {
        return _warehouses.FirstOrDefault(w => w.Id == warehouseId)?.Name ?? "";
    }

    public void AddWarehouse(Warehouse warehouse)
    {
        warehouse.Id = _warehouses.Count > 0 ? _warehouses.Max(w => w.Id) + 1 : 1;
        warehouse.CreatedAt = DateTime.Now;
        _warehouses.Add(warehouse);
        _dataService.SaveWarehouses(_warehouses.ToList());
        OnDataChanged();
    }

    public void UpdateWarehouse(Warehouse warehouse)
    {
        var existing = _warehouses.FirstOrDefault(w => w.Id == warehouse.Id);
        if (existing == null) return;
        var idx = _warehouses.IndexOf(existing);
        _warehouses[idx] = warehouse;
        _dataService.SaveWarehouses(_warehouses.ToList());
        OnDataChanged();
    }

    private void LoadData()
    {
        var loaded = _dataService.LoadStock();
        if (loaded != null && loaded.Count > 0)
        {
            foreach (var s in loaded)
                _stock.Add(s);
            _nextId = _stock.Max(s => s.Id) + 1;
        }

        var warehouses = _dataService.LoadWarehouses();
        if (warehouses != null && warehouses.Count > 0)
        {
            foreach (var w in warehouses)
                _warehouses.Add(w);
        }
        else
        {
            LoadSampleWarehouses();
        }
    }

    private void LoadSampleWarehouses()
    {
        var warehouses = new List<Warehouse>
        {
            new() { Id = 1, Name = "Основной склад", Address = "ул. Ленина, д. 1", ResponsiblePerson = "Иванов И.И.", IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 2, Name = "Розничный зал", Address = "ул. Ленина, д. 1, этаж 1", ResponsiblePerson = "Петров П.П.", IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 3, Name = "Брак", Address = "ул. Ленина, д. 1, подвал", ResponsiblePerson = "Сидоров С.С.", IsActive = true, CreatedAt = DateTime.Now }
        };
        foreach (var w in warehouses)
            _warehouses.Add(w);
        _dataService.SaveWarehouses(warehouses);
    }

    private void SaveData() => _dataService.SaveStock(_stock.ToList());

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
