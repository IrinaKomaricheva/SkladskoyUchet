// ============================================================
// Models/User.cs — Пользователь системы
// ============================================================
// Поддерживаются две роли:
//   Admin       — полный доступ: справочники, документы, отчёты, пользователи
//   Storekeeper — работа с документами и остатками; без доступа к настройкам
//
// Пароль хранится как SHA-256 хэш (AuthController.HashPassword).
// ============================================================

namespace WarehouseAccounting.Models;

public enum UserRole
{
    Admin,       // Администратор — полный доступ
    Storekeeper  // Кладовщик — документы и остатки
}

public class User
{
    public int Id { get; set; }

    /// <summary>Логин для входа в систему</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>SHA-256 хэш пароля (не хранить plaintext!)</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Storekeeper;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLogin { get; set; }

    /// <summary>True если у пользователя права администратора</summary>
    public bool IsAdmin => Role == UserRole.Admin;

    public override string ToString() => $"{FullName} ({Login})";
}
