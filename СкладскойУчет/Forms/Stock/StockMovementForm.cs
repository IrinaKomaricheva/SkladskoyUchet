using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Stock;

public partial class StockMovementForm : Form
{
    private readonly StockController _stockController;
    private readonly ProductController _productController;
    private readonly DocumentController _documentController;

    private Panel _topPanel = null!;
    private ComboBox _cmbProduct = null!;
    private ComboBox _cmbWarehouse = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Button _btnShow = null!;
    private DataGridView _grid = null!;
    private Label _lblBalance = null!;

    public StockMovementForm(
        StockController stockController,
        ProductController productController,
        DocumentController documentController)
    {
        _stockController = stockController;
        _productController = productController;
        _documentController = documentController;
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };

        var lblProduct = new Label { Text = "Товар:", Location = new Point(5, 10), Size = new Size(45, 25) };
        _cmbProduct = new ComboBox { Location = new Point(55, 8), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbProduct.Items.Add("— Все товары —");
        foreach (var p in _productController.Products)
            _cmbProduct.Items.Add(p);
        _cmbProduct.SelectedIndex = 0;

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(285, 10), Size = new Size(45, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(335, 8), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("Все склады");
        foreach (var w in _stockController.Warehouses)
            _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;

        var lblFrom = new Label { Text = "с:", Location = new Point(475, 10), Size = new Size(20, 25) };
        _dtpFrom = new DateTimePicker { Location = new Point(495, 8), Size = new Size(130, 25), Value = DateTime.Today.AddMonths(-1) };
        var lblTo = new Label { Text = "по:", Location = new Point(630, 10), Size = new Size(25, 25) };
        _dtpTo = new DateTimePicker { Location = new Point(655, 8), Size = new Size(130, 25), Value = DateTime.Today };

        _btnShow = new Button { Text = "Показать", Location = new Point(795, 6), Size = new Size(100, 28) };
        UITheme.StylePrimaryButton(_btnShow);
        _btnShow.Click += BtnShow_Click;

        _topPanel.Controls.AddRange(new Control[] {
            lblProduct, _cmbProduct, lblWarehouse, _cmbWarehouse,
            lblFrom, _dtpFrom, lblTo, _dtpTo, _btnShow
        });

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

        _lblBalance = new Label
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
        Controls.Add(_lblBalance);
        Controls.Add(_topPanel);
    }

    private void BtnShow_Click(object? sender, EventArgs e)
    {
        var productId = _cmbProduct.SelectedIndex > 0
            ? (_cmbProduct.SelectedItem as Product)?.Id
            : (int?)null;
        var warehouseId = _cmbWarehouse.SelectedIndex > 0
            ? (_cmbWarehouse.SelectedItem as Warehouse)?.Id
            : (int?)null;

        var from = _dtpFrom.Value.Date;
        var to = _dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

        var movements = _documentController.GetAllMovements(from, to, productId);

        decimal balance = productId.HasValue && warehouseId.HasValue
            ? _stockController.GetQuantity(productId.Value, warehouseId.Value)
            : productId.HasValue
                ? _stockController.GetStockByProduct(productId.Value).Sum(s => s.Quantity)
                : 0;

        if (warehouseId.HasValue)
        {
            movements = movements.Where(m => m.WarehouseId == null || m.WarehouseId == warehouseId.Value);
        }

        var products = _productController.Products.ToList();
        var productDict = products.ToDictionary(p => p.Id, p => p);

        var data = movements.Select(m => new
        {
            Дата = m.Date,
            Тип = m.DocType,
            Документ = m.DocNumber,
            Товар = productDict.TryGetValue(m.Line.ProductId, out var p) ? p.Name : "",
            Артикул = p?.SKU ?? "",
            Количество = m.DocType.Contains("Расход", StringComparison.OrdinalIgnoreCase) || m.DocType.Contains("списание", StringComparison.OrdinalIgnoreCase) || m.DocType.Contains("ист", StringComparison.OrdinalIgnoreCase)
                ? -m.Line.Quantity : m.Line.Quantity,
            Цена = m.Line.UnitPrice,
            Сумма = m.Line.TotalPrice
        }).ToList();

        _grid.DataSource = data;

        var productName = productId.HasValue ? productDict.TryGetValue(productId.Value, out var pp) ? pp.Name : "" : "все товары";
        var warehouseName = warehouseId.HasValue ? _stockController.GetWarehouseName(warehouseId.Value) : "все склады";
        _lblBalance.Text = $"Текущий остаток: {balance:N2}  |  {productName}  |  {warehouseName}";
    }

    private void InitializeComponent()
    {
        Text = "Движение товара";
        Size = new Size(900, 600);
        UITheme.StyleForm(this);
    }
}
