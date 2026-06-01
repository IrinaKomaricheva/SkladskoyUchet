// ============================================================
// Services/ValidationService.cs — Валидация данных
// ============================================================
// Проверяет корректность данных перед сохранением.
// Возвращает список ошибок (если список пуст — данные верны).
//
// Используется в формах перед вызовом controller.Add/Update:
//   var errors = ValidationService.ValidateProduct(product);
//   if (errors.Any()) { MessageBox.Show(string.Join("\n", errors)); return; }
// ============================================================

using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;

namespace WarehouseAccounting.Services;

public static class ValidationService
{
    // ── Валидация товара ─────────────────────────────────────────────────────

    public static List<string> ValidateProduct(Product product)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(product.Name))
            errors.Add("Наименование товара обязательно.");

        if (string.IsNullOrWhiteSpace(product.SKU))
            errors.Add("Артикул обязателен.");

        if (product.PurchasePrice < 0)
            errors.Add("Закупочная цена не может быть отрицательной.");

        if (product.SalePrice < 0)
            errors.Add("Продажная цена не может быть отрицательной.");

        if (product.MinStockLevel < 0)
            errors.Add("Минимальный остаток не может быть отрицательным.");

        return errors;
    }

    // ── Валидация поставщика ─────────────────────────────────────────────────

    public static List<string> ValidateSupplier(Supplier supplier)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(supplier.Name))
            errors.Add("Наименование поставщика обязательно.");

        if (!string.IsNullOrWhiteSpace(supplier.INN) && supplier.INN.Length is not (10 or 12))
            errors.Add("ИНН должен содержать 10 или 12 цифр.");

        return errors;
    }

    // ── Валидация покупателя ──────────────────────────────────────────────────

    public static List<string> ValidateCustomer(Customer customer)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(customer.Name))
            errors.Add("Наименование покупателя обязательно.");

        if (!string.IsNullOrWhiteSpace(customer.INN) && customer.INN.Length is not (10 or 12))
            errors.Add("ИНН должен содержать 10 или 12 цифр.");

        return errors;
    }

    // ── Валидация документа прихода ──────────────────────────────────────────

    public static List<string> ValidateReceipt(ReceiptDocument doc)
    {
        var errors = new List<string>();

        if (doc.SupplierId <= 0)
            errors.Add("Выберите поставщика.");

        if (doc.WarehouseId <= 0)
            errors.Add("Выберите склад.");

        if (!doc.Lines.Any())
            errors.Add("Добавьте хотя бы одну строку товара.");

        if (doc.Lines.Any(l => l.Quantity <= 0))
            errors.Add("Количество во всех строках должно быть больше нуля.");

        return errors;
    }
}
