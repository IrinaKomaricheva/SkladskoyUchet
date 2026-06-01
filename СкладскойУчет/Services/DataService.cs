// ============================================================
// Services/DataService.cs — Сохранение и загрузка данных (JSON)
// ============================================================
// Единственное место, где происходит I/O.
// Все данные хранятся в папке Data/ рядом с exe.
//
// Принцип работы:
//   Load*() — читает JSON-файл, десериализует в список объектов
//   Save*() — сериализует список в JSON и перезаписывает файл
//
// Если файл не существует — возвращает null / пустой список.
// ============================================================

using Newtonsoft.Json;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;

namespace WarehouseAccounting.Services;

public class DataService
{
    // Папка Data/ создаётся автоматически при первом сохранении
    private static readonly string DataFolder =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    // ── Пути к файлам ────────────────────────────────────────────────────────
    private string ProductsFile    => Path.Combine(DataFolder, "products.json");
    private string CategoriesFile  => Path.Combine(DataFolder, "categories.json");
    private string SuppliersFile   => Path.Combine(DataFolder, "suppliers.json");
    private string WarehousesFile  => Path.Combine(DataFolder, "warehouses.json");
    private string StockFile       => Path.Combine(DataFolder, "stock.json");
    private string UsersFile       => Path.Combine(DataFolder, "users.json");
    private string ReceiptsFile    => Path.Combine(DataFolder, "receipts.json");
    private string ShipmentsFile   => Path.Combine(DataFolder, "shipments.json");
    private string TransfersFile   => Path.Combine(DataFolder, "transfers.json");
    private string InventoriesFile => Path.Combine(DataFolder, "inventories.json");
    private string CustomersFile   => Path.Combine(DataFolder, "customers.json");

    // ── Загрузка ─────────────────────────────────────────────────────────────
    public List<Product>?           LoadProducts()    => Load<List<Product>>(ProductsFile);
    public List<Category>?          LoadCategories()  => Load<List<Category>>(CategoriesFile);
    public List<Supplier>?          LoadSuppliers()   => Load<List<Supplier>>(SuppliersFile);
    public List<Warehouse>?         LoadWarehouses()  => Load<List<Warehouse>>(WarehousesFile);
    public List<StockItem>?         LoadStock()       => Load<List<StockItem>>(StockFile);
    public List<User>?              LoadUsers()       => Load<List<User>>(UsersFile);
    public List<ReceiptDocument>?   LoadReceipts()    => Load<List<ReceiptDocument>>(ReceiptsFile);
    public List<ShipmentDocument>?  LoadShipments()   => Load<List<ShipmentDocument>>(ShipmentsFile);
    public List<TransferDocument>?  LoadTransfers()   => Load<List<TransferDocument>>(TransfersFile);
    public List<InventoryDocument>? LoadInventories() => Load<List<InventoryDocument>>(InventoriesFile);
    public List<Customer>?          LoadCustomers()   => Load<List<Customer>>(CustomersFile);

    // ── Сохранение ───────────────────────────────────────────────────────────
    public void SaveProducts(List<Product> data)           => Save(ProductsFile, data);
    public void SaveCategories(List<Category> data)        => Save(CategoriesFile, data);
    public void SaveSuppliers(List<Supplier> data)         => Save(SuppliersFile, data);
    public void SaveWarehouses(List<Warehouse> data)       => Save(WarehousesFile, data);
    public void SaveStock(List<StockItem> data)            => Save(StockFile, data);
    public void SaveUsers(List<User> data)                 => Save(UsersFile, data);
    public void SaveReceipts(List<ReceiptDocument> data)   => Save(ReceiptsFile, data);
    public void SaveShipments(List<ShipmentDocument> data) => Save(ShipmentsFile, data);
    public void SaveTransfers(List<TransferDocument> data) => Save(TransfersFile, data);
    public void SaveInventories(List<InventoryDocument> d) => Save(InventoriesFile, d);
    public void SaveCustomers(List<Customer> data)         => Save(CustomersFile, data);

    // ── Внутренние методы ────────────────────────────────────────────────────

    private T? Load<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }

    private void Save<T>(string filePath, T data)
    {
        Directory.CreateDirectory(DataFolder);
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
}
