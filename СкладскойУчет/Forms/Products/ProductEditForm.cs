using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Products;

public partial class ProductEditForm : Form
{
    private readonly ProductController _productController;
    private readonly Product? _existingProduct;

    private TableLayoutPanel _table = null!;
    private TextBox _txtName = null!;
    private TextBox _txtSKU = null!;
    private TextBox _txtBarcode = null!;
    private ComboBox _cmbCategory = null!;
    private TextBox _txtUnit = null!;
    private NumericUpDown _nudPurchasePrice = null!;
    private NumericUpDown _nudSalePrice = null!;
    private NumericUpDown _nudMinStock = null!;
    private TextBox _txtDescription = null!;
    private CheckBox _chkIsActive = null!;

    public ProductEditForm(ProductController productController, Product? existingProduct = null)
    {
        _productController = productController;
        _existingProduct = existingProduct;
        InitializeComponent();
        SetupUI();

        if (_existingProduct != null) FillFields(_existingProduct);
    }

    private void SetupUI()
    {
        _table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(10),
            AutoSize = true
        };
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow("Наименование:", out _txtName);
        AddRow("Артикул (SKU):", out _txtSKU);
        AddRow("Штрихкод:", out _txtBarcode);

        _cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbCategory.Items.Add("— Без категории —");
        foreach (var cat in _productController.Categories)
            _cmbCategory.Items.Add(cat);
        _cmbCategory.SelectedIndex = 0;
        AddRow("Категория:", _cmbCategory);

        _txtUnit = new TextBox { Text = "шт" };
        AddRow("Единица измерения:", _txtUnit);

        _nudPurchasePrice = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999999, Minimum = 0 };
        AddRow("Закупочная цена:", _nudPurchasePrice);

        _nudSalePrice = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999999, Minimum = 0 };
        AddRow("Продажная цена:", _nudSalePrice);

        _nudMinStock = new NumericUpDown { DecimalPlaces = 2, Maximum = 999999999, Minimum = 0 };
        AddRow("Мин. остаток:", _nudMinStock);

        _txtDescription = new TextBox { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
        AddRow("Описание:", _txtDescription);

        _chkIsActive = new CheckBox { Text = "Активен", Checked = true };
        AddRow("", _chkIsActive);

        var panel = new Panel { Height = 50, Dock = DockStyle.Bottom };
        var btnSave = new Button
        {
            Text = "💾 Сохранить", Location = new Point(10, 10), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = Color.DodgerBlue, ForeColor = Color.White
        };
        btnSave.Click += BtnSave_Click;
        var btnCancel = new Button { Text = "Отмена", Location = new Point(140, 10), Size = new Size(120, 30) };
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        panel.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(_table);
        Controls.Add(panel);
    }

    private void AddRow(string label, Control control)
    {
        var row = _table.RowCount++;
        _table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        _table.Controls.Add(control, 1, row);
    }

    private void AddRow<T>(string label, out T textBox) where T : Control, new()
    {
        textBox = new T();
        AddRow(label, textBox);
    }

    private void FillFields(Product product)
    {
        _txtName.Text = product.Name;
        _txtSKU.Text = product.SKU;
        _txtBarcode.Text = product.Barcode;

        if (product.CategoryId > 0)
        {
            var idx = _cmbCategory.Items.Cast<Category>().ToList().FindIndex(c => c.Id == product.CategoryId);
            if (idx >= 0) _cmbCategory.SelectedIndex = idx + 1;
        }

        _txtUnit.Text = product.Unit;
        _nudPurchasePrice.Value = product.PurchasePrice;
        _nudSalePrice.Value = product.SalePrice;
        _nudMinStock.Value = product.MinStockLevel;
        _txtDescription.Text = product.Description;
        _chkIsActive.Checked = product.IsActive;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var product = new Product
        {
            Id = _existingProduct?.Id ?? 0,
            Name = _txtName.Text.Trim(),
            SKU = _txtSKU.Text.Trim(),
            Barcode = _txtBarcode.Text.Trim(),
            CategoryId = _cmbCategory.SelectedIndex > 0
                ? (_cmbCategory.SelectedItem as Category)?.Id ?? 0
                : 0,
            Unit = _txtUnit.Text.Trim(),
            PurchasePrice = _nudPurchasePrice.Value,
            SalePrice = _nudSalePrice.Value,
            MinStockLevel = _nudMinStock.Value,
            Description = _txtDescription.Text.Trim(),
            IsActive = _chkIsActive.Checked
        };

        var errors = ValidationService.ValidateProduct(product);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Ошибка валидации",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_existingProduct == null)
            _productController.AddProduct(product);
        else
            _productController.UpdateProduct(product);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void InitializeComponent()
    {
        Text = _existingProduct == null ? "Новый товар" : "Редактирование товара";
        Size = new Size(500, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
    }
}
