using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class CustomerController
{
    private readonly List<Customer> _customers = new();
    private readonly DataService _dataService = new();
    private int _nextId = 1;

    public event EventHandler? DataChanged;
    public event EventHandler<string>? StatusChanged;

    public CustomerController()
    {
        LoadData();
    }

    public IReadOnlyList<Customer> Customers => _customers.AsReadOnly();

    public Customer? GetById(int id) => _customers.FirstOrDefault(c => c.Id == id);

    public List<Customer> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _customers.ToList();

        var lower = searchText.ToLower();
        return _customers.Where(c =>
            c.Name.ToLower().Contains(lower) ||
            c.INN.ToLower().Contains(lower) ||
            c.ShortName.ToLower().Contains(lower)
        ).ToList();
    }

    public void AddCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ArgumentException("Наименование покупателя обязательно.");

        customer.Id = _nextId++;
        _customers.Add(customer);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Добавлен покупатель: {customer.Name}");
    }

    public void UpdateCustomer(Customer customer)
    {
        var existing = GetById(customer.Id);
        if (existing == null)
            throw new InvalidOperationException("Покупатель не найден.");

        existing.Name = customer.Name;
        existing.ShortName = customer.ShortName;
        existing.INN = customer.INN;
        existing.KPP = customer.KPP;
        existing.ContactPerson = customer.ContactPerson;
        existing.Phone = customer.Phone;
        existing.Email = customer.Email;
        existing.Address = customer.Address;
        existing.PaymentTerms = customer.PaymentTerms;
        existing.Notes = customer.Notes;
        existing.IsActive = customer.IsActive;

        SaveData();
        OnDataChanged();
        OnStatusChanged($"Обновлён покупатель: {customer.Name}");
    }

    public void DeleteCustomer(int id)
    {
        var customer = GetById(id);
        if (customer == null)
            throw new InvalidOperationException("Покупатель не найден.");

        _customers.Remove(customer);
        SaveData();
        OnDataChanged();
        OnStatusChanged($"Удалён покупатель: {customer.Name}");
    }

    private void LoadData()
    {
        var loaded = _dataService.LoadCustomers();
        if (loaded != null && loaded.Count > 0)
        {
            _customers.AddRange(loaded);
            _nextId = _customers.Max(c => c.Id) + 1;
        }
        else
        {
            SeedData();
        }
    }

    private void SeedData()
    {
        _customers.AddRange(new[]
        {
            new Customer
            {
                Id = _nextId++,
                Name = "ООО \"ТоргСеть\"",
                ShortName = "ТоргСеть",
                INN = "7701234567",
                KPP = "770101001",
                ContactPerson = "Смирнов Алексей",
                Phone = "+7 (495) 100-20-30",
                Email = "buy@torсset.ru",
                Address = "г. Москва, ул. Тверская, д. 10",
                PaymentTerms = "Безналичный расчёт, 14 дней",
                IsActive = true
            },
            new Customer
            {
                Id = _nextId++,
                Name = "ИП Иванов",
                ShortName = "ИП Иванов",
                INN = "7712345678",
                ContactPerson = "Иванов Сергей Петрович",
                Phone = "+7 (916) 555-44-33",
                Email = "ivanov.sp@mail.ru",
                Address = "г. Москва, ул. Ленина, д. 5",
                PaymentTerms = "Наличный расчёт",
                IsActive = true
            },
            new Customer
            {
                Id = _nextId++,
                Name = "АО \"ПромСнаб\"",
                ShortName = "ПромСнаб",
                INN = "7723456789",
                KPP = "772301001",
                ContactPerson = "Кузнецова Мария",
                Phone = "+7 (495) 200-40-50",
                Email = "orders@promsnab.ru",
                Address = "г. Москва, пр-т Мира, д. 25",
                PaymentTerms = "Безналичный расчёт, 30 дней",
                IsActive = true
            }
        });
        SaveData();
    }

    private void SaveData() => _dataService.SaveCustomers(_customers);

    private void OnDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);
}
