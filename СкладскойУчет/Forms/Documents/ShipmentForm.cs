using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Documents;

public partial class ShipmentForm : Form
{
    private readonly DocumentController _documentController;
    private readonly ProductController _productController;
    private readonly StockController _stockController;
    private readonly AuthController _authController;
    private readonly CustomerController _customerController;

    private ShipmentDocument _document = null!;

    private Panel _headerPanel = null!;
    private Label _lblNumber = null!;
    private DateTimePicker _dtpDate = null!;
    private ComboBox _cmbWarehouse = null!;
    private ComboBox _cmbCustomer = null!;
    private TextBox _txtReason = null!;
    private DataGridView _grid = null!;
    private Label _lblTotal = null!;
    private Panel _buttonPanel = null!;

    public ShipmentForm(
        DocumentController documentController,
        ProductController productController,
        StockController stockController,
        AuthController authController,
        CustomerController customerController)
    {
        _documentController = documentController;
        _productController = productController;
        _stockController = stockController;
        _authController = authController;
        _customerController = customerController;

        InitializeComponent();
        SetupUI();
        _document = _documentController.GetOrCreateDraftShipment();
        FillFromDocument();
        FormClosing += ShipmentForm_FormClosing;
    }

    private void SetupUI()
    {
        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };

        _lblNumber = new Label { Text = "№ ---", Font = new Font("Arial", 12, FontStyle.Bold), Location = new Point(10, 10), Size = new Size(200, 25) };
        _dtpDate = new DateTimePicker { Location = new Point(250, 10), Size = new Size(140, 25), Value = DateTime.Now };

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(10, 45), Size = new Size(50, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(65, 45), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("— Выберите склад —");
        foreach (var w in _stockController.Warehouses) _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;

        var lblRecipient = new Label { Text = "Получатель:", Location = new Point(280, 45), Size = new Size(80, 25) };
        _cmbCustomer = new ComboBox { Location = new Point(360, 45), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbCustomer.Items.Add("— Выберите покупателя —");
        foreach (var c in _customerController.Customers.Where(c => c.IsActive)) _cmbCustomer.Items.Add(c);
        _cmbCustomer.SelectedIndex = 0;

        var lblReason = new Label { Text = "Причина:", Location = new Point(10, 75), Size = new Size(60, 25) };
        _txtReason = new TextBox { Location = new Point(75, 75), Size = new Size(200, 25), Text = "Продажа" };

        _headerPanel.Controls.AddRange(new Control[] { _lblNumber, _dtpDate, lblWarehouse, _cmbWarehouse, lblRecipient, _cmbCustomer, lblReason, _txtReason });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, BackgroundColor = Color.White
        };
        _grid.Columns.Clear();
        _grid.Columns.Add("ProductId", "ID"); _grid.Columns["ProductId"].Visible = false;
        _grid.Columns.Add("ProductName", "Товар"); _grid.Columns["ProductName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _grid.Columns.Add("Available", "Доступно"); _grid.Columns["Available"].Width = 80; _grid.Columns["Available"].ReadOnly = true;
        _grid.Columns.Add("Quantity", "Кол-во"); _grid.Columns["Quantity"].Width = 80;
        _grid.Columns.Add("UnitPrice", "Цена"); _grid.Columns["UnitPrice"].Width = 100;
        _grid.Columns.Add("TotalPrice", "Сумма"); _grid.Columns["TotalPrice"].Width = 120; _grid.Columns["TotalPrice"].ReadOnly = true;
        UITheme.StyleGrid(_grid);
        _grid.CellEndEdit += (_, _) => RecalculateTotal();

        _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        _lblTotal = new Label { Text = "Итого: 0.00 ₽", Font = new Font("Arial", 11, FontStyle.Bold), Location = new Point(10, 8), Size = new Size(200, 25) };
        var btnAddLine = new Button { Text = "Добавить строку", Location = new Point(250, 7), Size = new Size(140, 28) };
        UITheme.StylePrimaryButton(btnAddLine);
        btnAddLine.Click += BtnAddLine_Click;
        var btnRemoveLine = new Button { Text = "Удалить строку", Location = new Point(395, 7), Size = new Size(130, 28) };
        UITheme.StyleDangerButton(btnRemoveLine);
        btnRemoveLine.Click += (_, _) => { if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow) { _grid.Rows.RemoveAt(_grid.CurrentRow.Index); RecalculateTotal(); } };
        var btnPost = new Button { Text = "Провести", Location = new Point(540, 7), Size = new Size(110, 28) };
        UITheme.StylePrimaryButton(btnPost);
        btnPost.Click += BtnPost_Click;
        var btnCancel = new Button { Text = "Закрыть", Location = new Point(655, 7), Size = new Size(100, 28) };
        UITheme.StyleDefaultButton(btnCancel);
        btnCancel.Click += (_, _) => Close();

        _buttonPanel.Controls.AddRange(new Control[] { _lblTotal, btnAddLine, btnRemoveLine, btnPost, btnCancel });
        Controls.Add(_grid); Controls.Add(_buttonPanel); Controls.Add(_headerPanel);
    }

    private void FillFromDocument()
    {
        _lblNumber.Text = $"№ {_document.Number}";
        _dtpDate.Value = _document.Date;
        _txtReason.Text = _document.Reason;
        if (!string.IsNullOrWhiteSpace(_document.Recipient))
        {
            for (int i = 0; i < _cmbCustomer.Items.Count; i++)
                if (_cmbCustomer.Items[i] is Customer c && c.Name == _document.Recipient)
                { _cmbCustomer.SelectedIndex = i; break; }
        }
        if (_document.WarehouseId > 0)
            for (int i = 0; i < _cmbWarehouse.Items.Count; i++)
                if (_cmbWarehouse.Items[i] is Warehouse w && w.Id == _document.WarehouseId)
                { _cmbWarehouse.SelectedIndex = i; break; }
        foreach (var line in _document.Lines)
        {
            var product = _productController.GetById(line.ProductId);
            var available = _stockController.GetAvailable(line.ProductId, _document.WarehouseId);
            _grid.Rows.Add(line.ProductId, product?.Name ?? $"Товар #{line.ProductId}", available, line.Quantity, line.UnitPrice, line.TotalPrice);
        }
        RecalculateTotal();
        if (_document.IsPosted) { _grid.ReadOnly = true; _cmbWarehouse.Enabled = false; _cmbCustomer.Enabled = false; }
    }

    private void RecalculateTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0);
            var price = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0);
            row.Cells["TotalPrice"].Value = qty * price;
            total += qty * price;
        }
        _lblTotal.Text = $"Итого: {total:N2} ₽";
    }

    private void BtnAddLine_Click(object? sender, EventArgs e)
    {
        var warehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        if (warehouseId <= 0) { MessageBox.Show("Выберите склад.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        var products = _productController.Products.Where(p => p.IsActive).ToList();
        var availableItems = _stockController.GetStockByWarehouse(warehouseId)
            .Where(s => s.Available > 0)
            .Select(s => s.ProductId)
            .ToHashSet();
        products = products.Where(p => availableItems.Contains(p.Id)).ToList();

        if (products.Count == 0) { MessageBox.Show("Нет доступных товаров на выбранном складе.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

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
            var available = _stockController.GetAvailable(product.Id, warehouseId);
            if (nudQty.Value > available) { MessageBox.Show($"Доступно только {available} {product.Unit}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _grid.Rows.Add(product.Id, product.Name, available, nudQty.Value, product.SalePrice, nudQty.Value * product.SalePrice);
            RecalculateTotal();
        }
    }

    private void BtnPost_Click(object? sender, EventArgs e)
    {
        if (_document.IsPosted) { MessageBox.Show("Документ уже проведён.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        _document.Lines.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var productId = Convert.ToInt32(row.Cells["ProductId"].Value ?? 0);
            if (productId <= 0) continue;
            _document.Lines.Add(new DocumentLine
            {
                ProductId = productId,
                Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0),
                UnitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0)
            });
        }

        _document.WarehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        _document.Recipient = _cmbCustomer.SelectedIndex > 0 ? (_cmbCustomer.SelectedItem as Customer)?.Name ?? "" : "";
        _document.Reason = _txtReason.Text.Trim();
        _document.Date = _dtpDate.Value;

        try
        {
            _documentController.PostShipment(_document, _authController.CurrentUser?.Id ?? 0);
            MessageBox.Show($"Документ {_document.Number} проведён.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _grid.ReadOnly = true; _cmbWarehouse.Enabled = false; _cmbCustomer.Enabled = false;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void InitializeComponent()
    {
        Text = "Расходная накладная";
        Size = new Size(900, 600);
        UITheme.StyleForm(this);
    }

    private void ShipmentForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_document.IsPosted) return;

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
        _document.WarehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        _document.Recipient = _cmbCustomer.SelectedIndex > 0 ? (_cmbCustomer.SelectedItem as Customer)?.Name ?? "" : "";
        _document.Reason = _txtReason.Text.Trim();
        _document.Date = _dtpDate.Value;

        if (_document.Lines.Count == 0 && _document.WarehouseId == 0 && string.IsNullOrWhiteSpace(_document.Recipient))
            _documentController.RemoveDraft(_document);
        else
            _documentController.SaveAllDrafts();
    }
}
