using System.ComponentModel;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class SupplierController
{
    private readonly BindingList<Supplier> _suppliers = new();
    private readonly DataService _dataService = new();
    private int _nextId = 1;

    public event EventHandler? DataChanged;
    public event EventHandler<string>? StatusChanged;

    public BindingList<Supplier> Suppliers => _suppliers;

    public SupplierController()
    {
        LoadData();
    }

    public void AddSupplier(Supplier supplier)
    {
        supplier.Id = _nextId++;
        supplier.CreatedAt = DateTime.Now;
        _suppliers.Add(supplier);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Поставщик \"{supplier.Name}\" добавлен");
    }

    public void UpdateSupplier(Supplier supplier)
    {
        var existing = _suppliers.FirstOrDefault(s => s.Id == supplier.Id);
        if (existing == null) return;

        var idx = _suppliers.IndexOf(existing);
        _suppliers[idx] = supplier;
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Поставщик \"{supplier.Name}\" обновлён");
    }

    public void DeleteSupplier(int id)
    {
        var supplier = _suppliers.FirstOrDefault(s => s.Id == id);
        if (supplier == null) return;

        _suppliers.Remove(supplier);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Поставщик \"{supplier.Name}\" удалён");
    }

    public List<Supplier> Search(string searchText = "")
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _suppliers.OrderBy(s => s.Name).ToList();

        var text = searchText.Trim().ToLower();
        return _suppliers
            .Where(s =>
                s.Name.ToLower().Contains(text) ||
                s.ShortName.ToLower().Contains(text) ||
                s.INN.Contains(text) ||
                s.ContactPerson.ToLower().Contains(text))
            .OrderBy(s => s.Name)
            .ToList();
    }

    public Supplier? GetById(int id)
    {
        return _suppliers.FirstOrDefault(s => s.Id == id);
    }

    private void LoadData()
    {
        var loaded = _dataService.LoadSuppliers();
        if (loaded != null && loaded.Count > 0)
        {
            foreach (var s in loaded)
                _suppliers.Add(s);
            _nextId = _suppliers.Max(s => s.Id) + 1;
        }
        else
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "Общество с ограниченной ответственностью \"Ромашка\"", ShortName = "ООО Ромашка", INN = "7701234567", KPP = "770101001", ContactPerson = "Иванов Иван", Phone = "+7 (495) 123-45-67", Email = "info@romashka.ru", Address = "г. Москва, ул. Тверская, д. 10", IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 2, Name = "Индивидуальный предприниматель Петров П.П.", ShortName = "ИП Петров", INN = "770212345678", ContactPerson = "Петров Пётр", Phone = "+7 (495) 234-56-78", Email = "petrov@mail.ru", Address = "г. Москва, ул. Арбат, д. 5", IsActive = true, CreatedAt = DateTime.Now },
            new() { Id = 3, Name = "Акционерное общество \"ТехноПоставка\"", ShortName = "АО ТехноПоставка", INN = "7703123456", KPP = "770301001", ContactPerson = "Сидорова Анна", Phone = "+7 (495) 345-67-89", Email = "sale@techno.ru", Address = "г. Москва, ул. Новый Арбат, д. 15", IsActive = true, CreatedAt = DateTime.Now }
        };
        _nextId = 4;
        foreach (var s in suppliers)
            _suppliers.Add(s);
        _dataService.SaveSuppliers(suppliers);
    }

    private void SaveData() => _dataService.SaveSuppliers(_suppliers.ToList());

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
