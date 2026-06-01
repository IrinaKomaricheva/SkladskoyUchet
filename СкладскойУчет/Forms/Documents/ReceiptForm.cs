using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Documents;

public partial class ReceiptForm : Form
{
    private readonly DocumentController _documentController;
    private readonly SupplierController _supplierController;
    private readonly ProductController _productController;
    private readonly StockController _stockController;
    private readonly AuthController _authController;

    private ReceiptDocument _document = null!;

    private Panel _headerPanel = null!;
    private Label _lblNumber = null!;
    private DateTimePicker _dtpDate = null!;
    private ComboBox _cmbSupplier = null!;
    private ComboBox _cmbWarehouse = null!;
    private DataGridView _grid = null!;
    private Label _lblTotal = null!;
    private Panel _buttonPanel = null!;

    public ReceiptForm(
        DocumentController documentController,
        SupplierController supplierController,
        ProductController productController,
        AuthController authController,
        StockController? stockController = null)
    {
        _documentController = documentController;
        _supplierController = supplierController;
        _productController = productController;
        _stockController = stockController ?? new StockController();
        _authController = authController;

        InitializeComponent();
        SetupUI();
        _document = _documentController.GetOrCreateDraftReceipt();
        FillFromDocument();
        FormClosing += ReceiptForm_FormClosing;
    }

    private void SetupUI()
    {
        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };

        _lblNumber = new Label { Text = "№ ---", Font = new Font("Arial", 12, FontStyle.Bold), Location = new Point(10, 10), Size = new Size(200, 25) };
        _dtpDate = new DateTimePicker { Location = new Point(250, 10), Size = new Size(140, 25), Value = DateTime.Now };

        var lblSupplier = new Label { Text = "Поставщик:", Location = new Point(10, 45), Size = new Size(80, 25) };
        _cmbSupplier = new ComboBox { Location = new Point(95, 45), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSupplier.Items.Add("— Выберите поставщика —");
        foreach (var s in _supplierController.Suppliers)
            _cmbSupplier.Items.Add(s);
        _cmbSupplier.SelectedIndex = 0;

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(360, 45), Size = new Size(50, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(410, 45), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("— Выберите склад —");
        foreach (var w in _stockController.Warehouses)
            _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;

        _headerPanel.Controls.AddRange(new Control[] { _lblNumber, _dtpDate, lblSupplier, _cmbSupplier, lblWarehouse, _cmbWarehouse });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };

        _grid.Columns.Clear();
        _grid.Columns.Add("ProductId", "ID товара");
        _grid.Columns.Add("ProductName", "Товар");
        _grid.Columns.Add("Quantity", "Кол-во");
        _grid.Columns.Add("UnitPrice", "Цена");
        _grid.Columns.Add("TotalPrice", "Сумма");
        _grid.Columns["ProductId"].Visible = false;
        _grid.Columns["ProductName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _grid.Columns["Quantity"].Width = 80;
        _grid.Columns["UnitPrice"].Width = 100;
        _grid.Columns["TotalPrice"].Width = 120;
        _grid.Columns["TotalPrice"].ReadOnly = true;
        UITheme.StyleGrid(_grid);
        _grid.CellEndEdit += (_, _) => RecalculateTotal();

        _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(5) };
        _lblTotal = new Label { Text = "Итого: 0.00 ₽", Font = new Font("Arial", 11, FontStyle.Bold), Location = new Point(10, 8), Size = new Size(200, 25) };

        var btnAddLine = new Button { Text = "Добавить строку", Location = new Point(250, 7), Size = new Size(140, 28) };
        UITheme.StylePrimaryButton(btnAddLine);
        btnAddLine.Click += BtnAddLine_Click;
        var btnRemoveLine = new Button { Text = "Удалить строку", Location = new Point(395, 7), Size = new Size(130, 28) };
        UITheme.StyleDangerButton(btnRemoveLine);
        btnRemoveLine.Click += (_, _) => { if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow) { _grid.Rows.RemoveAt(_grid.CurrentRow.Index); RecalculateTotal(); } };

        var btnSave = new Button { Text = "Сохранить", Location = new Point(540, 7), Size = new Size(100, 28) };
        UITheme.StyleDefaultButton(btnSave);
        btnSave.Click += BtnSave_Click;
        var btnPost = new Button { Text = "Провести", Location = new Point(645, 7), Size = new Size(110, 28) };
        UITheme.StylePrimaryButton(btnPost);
        btnPost.Click += BtnPost_Click;
        var btnCancel = new Button { Text = "Закрыть", Location = new Point(760, 7), Size = new Size(100, 28) };
        UITheme.StyleDefaultButton(btnCancel);
        btnCancel.Click += (_, _) => Close();

        _buttonPanel.Controls.AddRange(new Control[] { _lblTotal, btnAddLine, btnRemoveLine, btnSave, btnPost, btnCancel });

        Controls.Add(_grid);
        Controls.Add(_buttonPanel);
        Controls.Add(_headerPanel);
    }

    private void FillFromDocument()
    {
        _lblNumber.Text = $"№ {_document.Number}";
        _dtpDate.Value = _document.Date;
        if (_document.SupplierId > 0)
        {
            for (int i = 0; i < _cmbSupplier.Items.Count; i++)
                if (_cmbSupplier.Items[i] is Supplier s && s.Id == _document.SupplierId)
                { _cmbSupplier.SelectedIndex = i; break; }
        }
        if (_document.WarehouseId > 0)
        {
            for (int i = 0; i < _cmbWarehouse.Items.Count; i++)
                if (_cmbWarehouse.Items[i] is Warehouse w && w.Id == _document.WarehouseId)
                { _cmbWarehouse.SelectedIndex = i; break; }
        }

        foreach (var line in _document.Lines)
        {
            var product = _productController.GetById(line.ProductId);
            _grid.Rows.Add(line.ProductId, product?.Name ?? $"Товар #{line.ProductId}", line.Quantity, line.UnitPrice, line.TotalPrice);
        }
        RecalculateTotal();

        if (_document.IsPosted)
        {
            _cmbSupplier.Enabled = false;
            _cmbWarehouse.Enabled = false;
            _grid.ReadOnly = true;
        }
    }

    private void RecalculateTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var qty = row.Cells["Quantity"].Value != null ? Convert.ToDecimal(row.Cells["Quantity"].Value) : 0;
            var price = row.Cells["UnitPrice"].Value != null ? Convert.ToDecimal(row.Cells["UnitPrice"].Value) : 0;
            row.Cells["TotalPrice"].Value = qty * price;
            total += qty * price;
        }
        _lblTotal.Text = $"Итого: {total:N2} ₽";
    }

    private void BtnAddLine_Click(object? sender, EventArgs e)
    {
        var products = _productController.Products.Where(p => p.IsActive).ToList();
        if (products.Count == 0)
        {
            MessageBox.Show("Сначала добавьте товары в справочник.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var selectForm = new Form
        {
            Text = "Выбор товара", Size = new Size(400, 200), FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent, MaximizeBox = false
        };
        var cmb = new ComboBox { Location = new Point(20, 30), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var p in products) cmb.Items.Add(p);
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

        var nudQty = new NumericUpDown { Location = new Point(20, 70), Size = new Size(100, 25), Minimum = 0.01m, Maximum = 999999, DecimalPlaces = 2, Value = 1 };
        var lblQty = new Label { Text = "Количество:", Location = new Point(130, 72), Size = new Size(80, 20) };

        var btnOk = new Button { Text = "OK", Location = new Point(200, 110), Size = new Size(80, 30) };
        btnOk.Click += (_, _) => { selectForm.DialogResult = DialogResult.OK; selectForm.Close(); };

        selectForm.Controls.AddRange(new Control[] { new Label { Text = "Товар:", Location = new Point(20, 10), Size = new Size(80, 20) }, cmb, nudQty, lblQty, btnOk });

        if (selectForm.ShowDialog(this) == DialogResult.OK && cmb.SelectedItem is Product product)
        {
            _grid.Rows.Add(product.Id, product.Name, nudQty.Value, product.PurchasePrice, nudQty.Value * product.PurchasePrice);
            RecalculateTotal();
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SaveDocumentLines();
        MessageBox.Show($"Документ {_document.Number} сохранён.", "Сохранено", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnPost_Click(object? sender, EventArgs e)
    {
        if (_document.IsPosted)
        {
            MessageBox.Show("Документ уже проведён.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaveDocumentLines();

        _document.SupplierId = _cmbSupplier.SelectedIndex > 0 ? (_cmbSupplier.SelectedItem as Supplier)?.Id ?? 0 : 0;
        _document.WarehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        _document.Date = _dtpDate.Value;

        try
        {
            _documentController.PostReceipt(_document, _authController.CurrentUser?.Id ?? 0);
            MessageBox.Show($"Документ {_document.Number} проведён.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _grid.ReadOnly = true;
            _cmbSupplier.Enabled = false;
            _cmbWarehouse.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveDocumentLines()
    {
        _document.Lines.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var productId = row.Cells["ProductId"].Value != null ? Convert.ToInt32(row.Cells["ProductId"].Value) : 0;
            if (productId <= 0) continue;
            _document.Lines.Add(new DocumentLine
            {
                ProductId = productId,
                Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0),
                UnitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0)
            });
        }
    }

    private void ReceiptForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_document.IsPosted) return;

        SaveDocumentLines();
        _document.SupplierId = _cmbSupplier.SelectedIndex > 0 ? (_cmbSupplier.SelectedItem as Supplier)?.Id ?? 0 : 0;
        _document.WarehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        _document.Date = _dtpDate.Value;

        if (_document.Lines.Count == 0 && _document.SupplierId == 0 && _document.WarehouseId == 0)
            _documentController.RemoveDraft(_document);
        else
            _documentController.SaveAllDrafts();
    }

    private void InitializeComponent()
    {
        Text = "Приходная накладная";
        Size = new Size(900, 600);
        UITheme.StyleForm(this);
    }
}
