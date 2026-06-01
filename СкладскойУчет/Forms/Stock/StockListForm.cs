using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Stock;

public partial class StockListForm : Form
{
    private readonly StockController _stockController;
    private readonly ProductController _productController;

    private Panel _topPanel = null!;
    private ComboBox _cmbWarehouse = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkDeficitOnly = null!;
    private DataGridView _grid = null!;
    private Label _lblSummary = null!;

    public StockListForm(StockController stockController, ProductController productController)
    {
        _stockController = stockController;
        _productController = productController;
        InitializeComponent();
        SetupUI();
        SetupObservers();
        RefreshGrid();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(5, 10), Size = new Size(50, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(60, 8), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("Все склады");
        foreach (var w in _stockController.Warehouses)
            _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;
        _cmbWarehouse.SelectedIndexChanged += (_, _) => RefreshGrid();

        _txtSearch = new TextBox { Location = new Point(220, 8), Size = new Size(200, 25), PlaceholderText = "🔍 Поиск товара..." };
        _txtSearch.TextChanged += (_, _) => RefreshGrid();

        _chkDeficitOnly = new CheckBox { Text = "⚠ Только дефицит", Location = new Point(430, 8), Size = new Size(130, 25) };
        _chkDeficitOnly.CheckedChanged += (_, _) => RefreshGrid();

        _topPanel.Controls.AddRange(new Control[] { lblWarehouse, _cmbWarehouse, _txtSearch, _chkDeficitOnly });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UITheme.StyleGrid(_grid);
        _grid.DataBindingComplete += (_, _) => AdjustColumns();
        _grid.CellFormatting += Grid_CellFormatting;

        _lblSummary = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            BackColor = UITheme.VeryLightGreen,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.BoldFont
        };

        Controls.Add(_grid);
        Controls.Add(_lblSummary);
        Controls.Add(_topPanel);
    }

    private void SetupObservers()
    {
        _stockController.DataChanged += (_, _) => RefreshGrid();
    }

    private void RefreshGrid()
    {
        var warehouseId = _cmbWarehouse.SelectedIndex > 0
            ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id
            : (int?)null;

        var stockItems = warehouseId.HasValue
            ? _stockController.GetStockByWarehouse(warehouseId.Value)
            : _stockController.Stock.ToList();

        var searchText = _txtSearch.Text.Trim().ToLower();

        var products = _productController.Products.ToList();
        var productDict = products.ToDictionary(p => p.Id, p => p);
        var lowStockItems = _stockController.GetLowStockItems(products).Select(s => (s.ProductId, s.WarehouseId)).ToHashSet();

        var query = stockItems
            .Select(s => new
            {
                s.Id,
                Товар = productDict.TryGetValue(s.ProductId, out var p) ? p.Name : "",
                Артикул = p?.SKU ?? "",
                Категория = p != null ? _productController.GetCategoryName(p.CategoryId) : "",
                Склад = _stockController.GetWarehouseName(s.WarehouseId),
                Количество = s.Quantity,
                Резерв = s.Reserved,
                Доступно = s.Available,
                МинУровень = p?.MinStockLevel ?? 0,
                IsLow = lowStockItems.Contains((s.ProductId, s.WarehouseId)),
                Ед = p?.Unit ?? "",
                IsActive = p?.IsActive ?? false
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
            query = query.Where(s => s.Товар.ToLower().Contains(searchText) || s.Артикул.ToLower().Contains(searchText));

        if (_chkDeficitOnly.Checked)
            query = query.Where(s => s.IsLow);

        var data = query.OrderBy(s => s.Товар).ToList();
        _grid.DataSource = data;

        var deficitCount = data.Count(s => s.IsLow);
        _lblSummary.Text = $"Позиций: {data.Count}  |  Дефицит: {deficitCount}";
        if (deficitCount > 0)
            _lblSummary.ForeColor = Color.Red;
        else
            _lblSummary.ForeColor = Color.Black;
    }

    private void AdjustColumns()
    {
        if (_grid.Columns.Count == 0) return;
        if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
        if (_grid.Columns.Contains("IsLow")) _grid.Columns["IsLow"].Visible = false;
        if (_grid.Columns.Contains("МинУровень")) _grid.Columns["МинУровень"].Visible = false;
        if (_grid.Columns.Contains("Ед")) _grid.Columns["Ед"].Width = 50;

        if (_grid.Columns.Contains("IsActive"))
        {
            var col = _grid.Columns["IsActive"];
            var dataProp = col.DataPropertyName;
            var displayIndex = col.DisplayIndex;
            _grid.Columns.Remove(col);
            var chk = new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                DataPropertyName = dataProp,
                HeaderText = "Активность",
                Width = 80,
                ReadOnly = true,
                TrueValue = true,
                FalseValue = false,
                DisplayIndex = displayIndex
            };
            _grid.Columns.Add(chk);
        }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Rows[e.RowIndex].DataBoundItem == null) return;

        var row = _grid.Rows[e.RowIndex];
        dynamic item = row.DataBoundItem;
        bool isLow = item.IsLow;
        decimal quantity = item.Количество;

        if (quantity <= 0)
            row.DefaultCellStyle.BackColor = Color.LightCoral;
        else if (isLow)
            row.DefaultCellStyle.BackColor = Color.LightYellow;
        else
            row.DefaultCellStyle.BackColor = Color.White;
    }

    private void InitializeComponent()
    {
        Text = "Остатки на складе";
        Size = new Size(1000, 600);
        UITheme.StyleForm(this);
    }
}
