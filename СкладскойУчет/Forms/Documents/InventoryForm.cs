using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Models.Documents;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Documents;

public partial class InventoryForm : Form
{
    private readonly DocumentController _documentController;
    private readonly ProductController _productController;
    private readonly StockController _stockController;
    private readonly AuthController _authController;

    private InventoryDocument? _document;

    private Panel _topPanel = null!;
    private ComboBox _cmbWarehouse = null!;
    private Button _btnCreate = null!;
    private DataGridView _grid = null!;
    private Label _lblDeviations = null!;
    private Panel _buttonPanel = null!;

    public InventoryForm(
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
        FormClosing += InventoryForm_FormClosing;
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(10, 13), Size = new Size(50, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(65, 13), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("— Выберите склад —");
        foreach (var w in _stockController.Warehouses) _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;

        _btnCreate = new Button { Text = "Создать инвентаризацию", Location = new Point(280, 10), Size = new Size(180, 28) };
        UITheme.StylePrimaryButton(_btnCreate);
        _btnCreate.Click += BtnCreate_Click;

        _topPanel.Controls.AddRange(new Control[] { lblWarehouse, _cmbWarehouse, _btnCreate });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };
        _grid.CellValueChanged += Grid_CellValueChanged;
        _grid.Columns.Clear();
        _grid.Columns.Add("ProductId", "ID"); _grid.Columns["ProductId"].Visible = false;
        _grid.Columns.Add("ProductName", "Товар"); _grid.Columns["ProductName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; _grid.Columns["ProductName"].ReadOnly = true;
        _grid.Columns.Add("Accounting", "Учтено"); _grid.Columns["Accounting"].Width = 100; _grid.Columns["Accounting"].ReadOnly = true;
        _grid.Columns.Add("Actual", "Фактически"); _grid.Columns["Actual"].Width = 100;
        _grid.Columns.Add("Deviation", "Отклонение"); _grid.Columns["Deviation"].Width = 100; _grid.Columns["Deviation"].ReadOnly = true;
        UITheme.StyleGrid(_grid);

        _lblDeviations = new Label
        {
            Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0), BackColor = Color.LightGray
        };

        _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        var btnPost = new Button { Text = "Провести", Location = new Point(10, 7), Size = new Size(130, 28) };
        UITheme.StylePrimaryButton(btnPost);
        btnPost.Click += BtnPost_Click;
        var btnCancel = new Button { Text = "Закрыть", Location = new Point(145, 7), Size = new Size(100, 28) };
        UITheme.StyleDefaultButton(btnCancel);
        btnCancel.Click += (_, _) => Close();

        _buttonPanel.Controls.AddRange(new Control[] { btnPost, btnCancel });

        Controls.Add(_grid);
        Controls.Add(_lblDeviations);
        Controls.Add(_buttonPanel);
        Controls.Add(_topPanel);
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        var warehouseId = _cmbWarehouse.SelectedIndex > 0 ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id ?? 0 : 0;
        if (warehouseId <= 0) { MessageBox.Show("Выберите склад.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        _document = _documentController.CreateInventory(warehouseId);
        _grid.Rows.Clear();

        foreach (var line in _document.Lines.OfType<InventoryLine>())
        {
            var product = _productController.GetById(line.ProductId);
            _grid.Rows.Add(line.ProductId, product?.Name ?? $"Товар #{line.ProductId}",
                line.AccountingQuantity, line.ActualQuantity, line.Deviation);
        }

        UpdateDeviationSummary();
        _btnCreate.Enabled = false;
    }

    private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_document == null) return;

        var row = _grid.Rows[e.RowIndex];
        if (row.IsNewRow) return;

        var actualVal = row.Cells["Actual"].Value;
        decimal actual = actualVal != null ? Convert.ToDecimal(actualVal) : 0;

        var accounting = Convert.ToDecimal(row.Cells["Accounting"].Value ?? 0);
        row.Cells["Deviation"].Value = actual - accounting;

        var productId = Convert.ToInt32(row.Cells["ProductId"].Value ?? 0);
        var invLine = _document.Lines.OfType<InventoryLine>().FirstOrDefault(l => l.ProductId == productId);
        if (invLine != null)
        {
            invLine.ActualQuantity = actual;
        }

        UpdateDeviationSummary();
    }

    private void UpdateDeviationSummary()
    {
        if (_document == null) return;

        var deviations = _document.Lines.OfType<InventoryLine>().Count(l => l.HasDeviation);
        _lblDeviations.Text = $"Отклонений: {deviations}";
        _lblDeviations.ForeColor = deviations > 0 ? Color.Red : Color.Black;

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var deviation = Convert.ToDecimal(row.Cells["Deviation"].Value ?? 0);
            if (deviation > 0)
                row.DefaultCellStyle.BackColor = Color.LightGreen;
            else if (deviation < 0)
                row.DefaultCellStyle.BackColor = Color.LightCoral;
            else
                row.DefaultCellStyle.BackColor = Color.White;
        }
    }

    private void BtnPost_Click(object? sender, EventArgs e)
    {
        if (_document == null) { MessageBox.Show("Сначала создайте инвентаризацию.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (_document.IsPosted) { MessageBox.Show("Инвентаризация уже проведена.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        try
        {
            _documentController.PostInventory(_document, _authController.CurrentUser?.Id ?? 0);
            MessageBox.Show($"Инвентаризация {_document.Number} проведена.", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _grid.ReadOnly = true;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void InitializeComponent()
    {
        Text = "Инвентаризация";
        Size = new Size(900, 600);
        UITheme.StyleForm(this);
    }

    private void InventoryForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_document == null || _document.IsPosted) return;
        _documentController.SaveAllDrafts();
    }
}
