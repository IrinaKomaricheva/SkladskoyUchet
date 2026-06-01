using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Documents;

public partial class TransferForm : Form
{
    private readonly DocumentController _documentController;
    private readonly ProductController _productController;
    private readonly StockController _stockController;
    private readonly AuthController _authController;

    private TransferDocument _document = null!;

    private Panel _headerPanel = null!;
    private ComboBox _cmbSourceWarehouse = null!;
    private ComboBox _cmbTargetWarehouse = null!;
    private DataGridView _grid = null!;
    private Panel _buttonPanel = null!;

    public TransferForm(
        DocumentController documentController,
        ProductController productController,
        StockController stockController,
        AuthController authController)
    {
        _documentController = documentController;
        _productController = productController;
        _stockController = stockController;
        _authController = authController;

        InitializeComponent();
        SetupUI();
        _document = _documentController.GetOrCreateDraftTransfer();
        FillFromDocument();
        FormClosing += TransferForm_FormClosing;
    }

    private void SetupUI()
    {
        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };

        var lblSource = new Label { Text = "Откуда:", Location = new Point(10, 15), Size = new Size(60, 25) };
        _cmbSourceWarehouse = new ComboBox { Location = new Point(75, 15), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbSourceWarehouse.Items.Add("— Выберите склад —");
        foreach (var w in _stockController.Warehouses) _cmbSourceWarehouse.Items.Add(w);
        _cmbSourceWarehouse.SelectedIndex = 0;
        _cmbSourceWarehouse.SelectedIndexChanged += (_, _) => RefreshAvailableProducts();

        var lblTarget = new Label { Text = "Куда:", Location = new Point(290, 15), Size = new Size(40, 25) };
        _cmbTargetWarehouse = new ComboBox { Location = new Point(335, 15), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbTargetWarehouse.Items.Add("— Выберите склад —");
        foreach (var w in _stockController.Warehouses) _cmbTargetWarehouse.Items.Add(w);
        _cmbTargetWarehouse.SelectedIndex = 0;

        _headerPanel.Controls.AddRange(new Control[] { lblSource, _cmbSourceWarehouse, lblTarget, _cmbTargetWarehouse });

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
        UITheme.StyleGrid(_grid);

        _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        var btnAddLine = new Button { Text = "Добавить строку", Location = new Point(10, 7), Size = new Size(140, 28) };
        UITheme.StylePrimaryButton(btnAddLine);
        btnAddLine.Click += BtnAddLine_Click;
        var btnRemoveLine = new Button { Text = "Удалить строку", Location = new Point(155, 7), Size = new Size(130, 28) };
        UITheme.StyleDangerButton(btnRemoveLine);
        btnRemoveLine.Click += (_, _) => { if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow) _grid.Rows.RemoveAt(_grid.CurrentRow.Index); };
        var btnPost = new Button { Text = "Провести", Location = new Point(300, 7), Size = new Size(110, 28) };
        UITheme.StylePrimaryButton(btnPost);
        btnPost.Click += BtnPost_Click;
        var btnCancel = new Button { Text = "Закрыть", Location = new Point(415, 7), Size = new Size(100, 28) };
        UITheme.StyleDefaultButton(btnCancel);
        btnCancel.Click += (_, _) => Close();

        _buttonPanel.Controls.AddRange(new Control[] { btnAddLine, btnRemoveLine, btnPost, btnCancel });
        Controls.Add(_grid); Controls.Add(_buttonPanel); Controls.Add(_headerPanel);
    }

    private void FillFromDocument()
    {
        if (_document.SourceWarehouseId > 0)
            for (int i = 0; i < _cmbSourceWarehouse.Items.Count; i++)
                if (_cmbSourceWarehouse.Items[i] is Warehouse w && w.Id == _document.SourceWarehouseId)
                { _cmbSourceWarehouse.SelectedIndex = i; break; }
        if (_document.TargetWarehouseId > 0)
            for (int i = 0; i < _cmbTargetWarehouse.Items.Count; i++)
                if (_cmbTargetWarehouse.Items[i] is Warehouse w && w.Id == _document.TargetWarehouseId)
                { _cmbTargetWarehouse.SelectedIndex = i; break; }
        if (_document.IsPosted) { _grid.ReadOnly = true; _cmbSourceWarehouse.Enabled = false; _cmbTargetWarehouse.Enabled = false; }
    }

    private void RefreshAvailableProducts()
    {
        _grid.Rows.Clear();
        var warehouseId = _cmbSourceWarehouse.SelectedIndex > 0 ? (_cmbSourceWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        if (warehouseId <= 0) return;

        var stockItems = _stockController.GetStockByWarehouse(warehouseId).Where(s => s.Available > 0);
        foreach (var si in stockItems)
        {
            var product = _productController.GetById(si.ProductId);
            if (product != null)
                _grid.Rows.Add(si.ProductId, product.Name, si.Available, 0);
        }
    }

    private void BtnAddLine_Click(object? sender, EventArgs e)
    {
        var sourceId = _cmbSourceWarehouse.SelectedIndex > 0 ? (_cmbSourceWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        var targetId = _cmbTargetWarehouse.SelectedIndex > 0 ? (_cmbTargetWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;

        if (sourceId <= 0 || targetId <= 0) { MessageBox.Show("Выберите оба склада.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (sourceId == targetId) { MessageBox.Show("Склады должны различаться.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var products = _productController.Products.Where(p => p.IsActive).ToList();
        var availableItems = _stockController.GetStockByWarehouse(sourceId).Where(s => s.Available > 0).Select(s => s.ProductId).ToHashSet();
        products = products.Where(p => availableItems.Contains(p.Id)).ToList();

        if (products.Count == 0) { MessageBox.Show("Нет товаров для перемещения.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        using var selectForm = new Form
        {
            Text = "Выбор товара", Size = new Size(400, 200), FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent, MaximizeBox = false
        };
        var cmb = new ComboBox { Location = new Point(20, 30), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var p in products) cmb.Items.Add(p);
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

        var nudQty = new NumericUpDown { Location = new Point(20, 70), Size = new Size(100, 25), Minimum = 0.01m, Maximum = 999999, DecimalPlaces = 2, Value = 1 };
        var btnOk = new Button { Text = "OK", Location = new Point(200, 110), Size = new Size(80, 30) };
        btnOk.Click += (_, _) => { selectForm.DialogResult = DialogResult.OK; selectForm.Close(); };
        selectForm.Controls.AddRange(new Control[] { new Label { Text = "Товар:", Location = new Point(20, 10), Size = new Size(80, 20) }, cmb, nudQty, btnOk });

        if (selectForm.ShowDialog(this) == DialogResult.OK && cmb.SelectedItem is Product product)
        {
            var available = _stockController.GetAvailable(product.Id, sourceId);
            if (nudQty.Value > available) { MessageBox.Show($"Доступно только {available} {product.Unit}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _grid.Rows.Add(product.Id, product.Name, available, nudQty.Value);
        }
    }

    private void BtnPost_Click(object? sender, EventArgs e)
    {
        if (_document.IsPosted) { MessageBox.Show("Документ уже проведён.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        var sourceId = _cmbSourceWarehouse.SelectedIndex > 0 ? (_cmbSourceWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        var targetId = _cmbTargetWarehouse.SelectedIndex > 0 ? (_cmbTargetWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;

        if (sourceId <= 0 || targetId <= 0) { MessageBox.Show("Выберите оба склада.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (sourceId == targetId) { MessageBox.Show("Склады должны различаться.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        _document.Lines.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var productId = Convert.ToInt32(row.Cells["ProductId"].Value ?? 0);
            var qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0);
            if (productId <= 0 || qty <= 0) continue;
            _document.Lines.Add(new DocumentLine { ProductId = productId, Quantity = qty });
        }

        if (_document.Lines.Count == 0) { MessageBox.Show("Добавьте товары для перемещения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        _document.SourceWarehouseId = sourceId;
        _document.TargetWarehouseId = targetId;

        try
        {
            _documentController.PostTransfer(_document, _authController.CurrentUser?.Id ?? 0);
            MessageBox.Show($"Документ {_document.Number} проведён.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _grid.ReadOnly = true; _cmbSourceWarehouse.Enabled = false; _cmbTargetWarehouse.Enabled = false;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void InitializeComponent()
    {
        Text = "Перемещение товара";
        Size = new Size(900, 600);
        UITheme.StyleForm(this);
    }

    private void TransferForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_document.IsPosted) return;

        _document.Lines.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var productId = Convert.ToInt32(row.Cells["ProductId"].Value ?? 0);
            var qty = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0);
            if (productId <= 0 || qty <= 0) continue;
            _document.Lines.Add(new DocumentLine { ProductId = productId, Quantity = qty });
        }
        _document.SourceWarehouseId = _cmbSourceWarehouse.SelectedIndex > 0 ? (_cmbSourceWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        _document.TargetWarehouseId = _cmbTargetWarehouse.SelectedIndex > 0 ? (_cmbTargetWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;

        if (_document.Lines.Count == 0 && _document.SourceWarehouseId == 0 && _document.TargetWarehouseId == 0)
            _documentController.RemoveDraft(_document);
        else
            _documentController.SaveAllDrafts();
    }
}
