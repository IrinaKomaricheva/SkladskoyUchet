using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Controllers;

public class AuthController
{
    private readonly BindingList<User> _users = new();
    private readonly DataService _dataService = new();

    public User? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public BindingList<User> Users => _users;

    public AuthController()
    {
        LoadData();
    }

    public bool Login(string login, string password)
    {
        var user = _users.FirstOrDefault(u =>
            u.Login.Equals(login, StringComparison.OrdinalIgnoreCase) &&
            u.PasswordHash == HashPassword(password));

        if (user == null)
            return false;

        if (!user.IsActive)
            return false;

        user.LastLogin = DateTime.Now;
        CurrentUser = user;
        SaveData();
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public void RequireAdmin()
    {
        if (CurrentUser?.IsAdmin != true)
            throw new UnauthorizedAccessException("Требуются права администратора.");
    }

    public void AddUser(User user)
    {
        RequireAdmin();
        user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
        user.CreatedAt = DateTime.Now;
        _users.Add(user);
        SaveData();
    }

    public void UpdateUser(User user)
    {
        RequireAdmin();
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing == null) return;

        existing.Login = user.Login;
        existing.Role = user.Role;
        existing.FullName = user.FullName;
        existing.IsActive = user.IsActive;
        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            existing.PasswordHash = user.PasswordHash;
        SaveData();
    }

    public void DeleteUser(int id)
    {
        RequireAdmin();
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null) return;
        _users.Remove(user);
        SaveData();
    }

    private void LoadData()
    {
        var loaded = _dataService.LoadUsers();
        if (loaded != null && loaded.Count > 0)
        {
            foreach (var u in loaded)
                _users.Add(u);
            return;
        }

        var admin = new User
        {
            Id = 1,
            Login = "admin",
            PasswordHash = HashPassword("admin"),
            Role = UserRole.Admin,
            FullName = "Администратор",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _users.Add(admin);

        var storekeeper = new User
        {
            Id = 2,
            Login = "storekeeper",
            PasswordHash = HashPassword("storekeeper"),
            Role = UserRole.Storekeeper,
            FullName = "Кладовщик",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _users.Add(storekeeper);

        SaveData();
    }

    private void SaveData() => _dataService.SaveUsers(_users.ToList());
}
