using System.ComponentModel;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class ProductController
{
    private readonly BindingList<Product> _products = new();
    private readonly BindingList<Category> _categories = new();
    private readonly DataService _dataService = new();
    private int _nextId = 1;

    public event EventHandler? DataChanged;
    public event EventHandler<string>? StatusChanged;

    public BindingList<Product> Products => _products;
    public BindingList<Category> Categories => _categories;

    public ProductController()
    {
        LoadData();
    }

    public void AddProduct(Product product)
    {
        product.Id = _nextId++;
        product.CreatedAt = DateTime.Now;
        _products.Add(product);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Товар \"{product.Name}\" добавлен");
    }

    public void UpdateProduct(Product product)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing == null) return;

        var idx = _products.IndexOf(existing);
        _products[idx] = product;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Товар \"{product.Name}\" обновлён");
    }

    public void DeleteProduct(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return;

        _products.Remove(product);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Товар \"{product.Name}\" удалён");
    }

    public List<Product> Search(string searchText = "", int? categoryId = null, bool activeOnly = true)
    {
        var query = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var text = searchText.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(text) ||
                p.SKU.ToLower().Contains(text) ||
                p.Barcode.ToLower().Contains(text));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return query.OrderBy(p => p.Name).ToList();
    }

    public Product? FindByBarcode(string barcode)
    {
        return _products.FirstOrDefault(p => p.Barcode == barcode);
    }

    public Product? GetById(int id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public void AddCategory(Category category)
    {
        category.Id = _categories.Count > 0 ? _categories.Max(c => c.Id) + 1 : 1;
        _categories.Add(category);
        _dataService.SaveCategories(_categories.ToList());
        OnDataChanged();
    }

    public void UpdateCategory(Category category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing == null) return;
        var idx = _categories.IndexOf(existing);
        _categories[idx] = category;
        _dataService.SaveCategories(_categories.ToList());
        OnDataChanged();
    }

    public void DeleteCategory(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category == null) return;
        if (_products.Any(p => p.CategoryId == id))
            throw new InvalidOperationException("Нельзя удалить категорию, к которой привязаны товары.");
        _categories.Remove(category);
        _dataService.SaveCategories(_categories.ToList());
        OnDataChanged();
    }

    public string GetCategoryName(int? categoryId)
    {
        if (categoryId == null) return "";
        var cat = _categories.FirstOrDefault(c => c.Id == categoryId);
        return cat?.Name ?? "";
    }

    public List<Category> GetCategoryTree()
    {
        var roots = _categories.Where(c => c.ParentId == null && c.IsActive).OrderBy(c => c.Name).ToList();
        foreach (var root in roots)
            root.Children = _categories.Where(c => c.ParentId == root.Id && c.IsActive).OrderBy(c => c.Name).ToList();
        return roots;
    }

    private void LoadData()
    {
        var loaded = _dataService.LoadProducts();
        if (loaded != null && loaded.Count > 0)
        {
            foreach (var p in loaded)
                _products.Add(p);
            _nextId = _products.Max(p => p.Id) + 1;
        }
        else
        {
            LoadSampleData();
        }

        var cats = _dataService.LoadCategories();
        if (cats != null && cats.Count > 0)
        {
            foreach (var c in cats)
                _categories.Add(c);
        }
        else
        {
            LoadSampleCategories();
        }
    }

    private void LoadSampleData()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Смартфон XYZ Pro", SKU = "PH-001", Barcode = "460000000001", CategoryId = 1, Unit = "шт", PurchasePrice = 25000, SalePrice = 35000, MinStockLevel = 5, IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 2, Name = "Ноутбук WorkBook 15", SKU = "NB-001", Barcode = "460000000002", CategoryId = 1, Unit = "шт", PurchasePrice = 45000, SalePrice = 55000, MinStockLevel = 3, IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 3, Name = "Кабель USB Type-C", SKU = "CB-001", Barcode = "460000000003", CategoryId = 2, Unit = "шт", PurchasePrice = 150, SalePrice = 350, MinStockLevel = 20, IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 4, Name = "Мышь беспроводная", SKU = "MS-001", Barcode = "460000000004", CategoryId = 2, Unit = "шт", PurchasePrice = 800, SalePrice = 1500, MinStockLevel = 10, IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 5, Name = "Монитор 27\" 4K", SKU = "MN-001", Barcode = "460000000005", CategoryId = 1, Unit = "шт", PurchasePrice = 22000, SalePrice = 29000, MinStockLevel = 2, IsActive = true, CreatedAt = DateTime.Now }
        };
        _nextId = 6;
        foreach (var p in products)
            _products.Add(p);
        _dataService.SaveProducts(products);
    }

    private void LoadSampleCategories()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Электроника", ParentId = null, IsActive = true },
            new() { Id = 2, Name = "Аксессуары", ParentId = null, IsActive = true },
            new() { Id = 3, Name = "Телефоны", ParentId = 1, IsActive = true },
            new() { Id = 4, Name = "Компьютеры", ParentId = 1, IsActive = true },
            new() { Id = 5, Name = "Кабели", ParentId = 2, IsActive = true }
        };
        foreach (var c in categories)
            _categories.Add(c);
        _dataService.SaveCategories(categories);
    }

    private void SaveData() => _dataService.SaveProducts(_products.ToList());

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
