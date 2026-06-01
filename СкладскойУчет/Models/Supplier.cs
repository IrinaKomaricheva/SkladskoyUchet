// ============================================================
// Models/Supplier.cs — Поставщик
// ============================================================
// Контрагент, у которого закупают товар.
// Хранит реквизиты для документов (ИНН, адрес, банк и т.д.)
// и историю сотрудничества.
// ============================================================

namespace WarehouseAccounting.Models;

public class Supplier
{
    public int Id { get; set; }

    /// <summary>Полное наименование организации или ФИО ИП</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Краткое название для отображения в списках</summary>
    public string ShortName { get; set; } = string.Empty;

    public string INN { get; set; } = string.Empty;   // ИНН
    public string KPP { get; set; } = string.Empty;   // КПП

    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>Условия оплаты: предоплата, постоплата N дней и т.д.</summary>
    public string PaymentTerms { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public override string ToString() => ShortName.Length > 0 ? ShortName : Name;
}
