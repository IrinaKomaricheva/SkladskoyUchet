using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Products;

public partial class ProductListForm : Form
{
    private readonly ProductController _productController;
    private readonly StockController _stockController;

    private Panel _topPanel = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategory = null!;
    private CheckBox _chkActiveOnly = null!;
    private DataGridView _grid = null!;
    private Panel _bottomPanel = null!;

    public ProductListForm(ProductController productController, StockController stockController)
    {
        _productController = productController;
        _stockController = stockController;
        InitializeComponent();
        SetupUI();
        SetupObservers();
        RefreshGrid();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };

        _txtSearch = new TextBox { Location = new Point(5, 8), Size = new Size(250, 25), PlaceholderText = "🔍 Поиск по названию/артикулу..." };
        _txtSearch.TextChanged += (_, _) => RefreshGrid();

        _cmbCategory = new ComboBox { Location = new Point(265, 8), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbCategory.Items.Add("Все категории");
        foreach (var cat in _productController.Categories)
            _cmbCategory.Items.Add(cat);
        _cmbCategory.SelectedIndex = 0;
        _cmbCategory.SelectedIndexChanged += (_, _) => RefreshGrid();

        _chkActiveOnly = new CheckBox { Text = "Только активные", Location = new Point(475, 8), Size = new Size(130, 25), Checked = true };
        _chkActiveOnly.CheckedChanged += (_, _) => RefreshGrid();

        _topPanel.Controls.AddRange(new Control[] { _txtSearch, _cmbCategory, _chkActiveOnly });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };
        UITheme.StyleGrid(_grid);
        _grid.DataBindingComplete += (_, _) => AdjustColumnWidths();
        _grid.CellDoubleClick += (_, _) => BtnEdit_Click(null, EventArgs.Empty);

        _bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(5) };
        var btnAdd = new Button { Text = "Добавить", Location = new Point(5, 7), Size = new Size(120, 28) };
        UITheme.StylePrimaryButton(btnAdd);
        btnAdd.Click += BtnAdd_Click;
        var btnEdit = new Button { Text = "Изменить", Location = new Point(130, 7), Size = new Size(120, 28) };
        UITheme.StyleDefaultButton(btnEdit);
        btnEdit.Click += BtnEdit_Click;
        var btnDelete = new Button { Text = "Удалить", Location = new Point(255, 7), Size = new Size(120, 28) };
        UITheme.StyleDangerButton(btnDelete);
        btnDelete.Click += BtnDelete_Click;
        _bottomPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        Controls.Add(_grid);
        Controls.Add(_bottomPanel);
        Controls.Add(_topPanel);
    }

    private void SetupObservers()
    {
        _productController.DataChanged += (_, _) => RefreshGrid();
    }

    private void RefreshGrid()
    {
        var searchText = _txtSearch.Text;
        var categoryId = _cmbCategory.SelectedIndex > 0
            ? (_cmbCategory.SelectedItem as Category)?.Id
            : (int?)null;
        var activeOnly = _chkActiveOnly.Checked;

        var products = _productController.Search(searchText, categoryId, activeOnly);

        var displayData = products.Select(p => new
        {
            p.Id,
            p.SKU,
            Наименование = p.Name,
            Категория = _productController.GetCategoryName(p.CategoryId),
            Ед = p.Unit,
            Закупка = p.PurchasePrice,
            Продажа = p.SalePrice,
            Наценка = $"{p.MarkupPercent:N1}%",
            Активность = p.IsActive
        }).ToList();

        _grid.DataSource = displayData;
    }

    private void AdjustColumnWidths()
    {
        if (_grid.Columns.Count == 0) return;

        if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Width = 40;
        if (_grid.Columns.Contains("SKU")) _grid.Columns["SKU"].Width = 80;
        if (_grid.Columns.Contains("Закупка")) _grid.Columns["Закупка"].DefaultCellStyle.Format = "N2";
        if (_grid.Columns.Contains("Продажа")) _grid.Columns["Продажа"].DefaultCellStyle.Format = "N2";
        if (_grid.Columns.Contains("Наценка")) _grid.Columns["Наценка"].DefaultCellStyle.Format = "N2";
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new ProductEditForm(_productController);
        form.ShowDialog(this);
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var productId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var product = _productController.GetById(productId);
        if (product == null) return;

        using var form = new ProductEditForm(_productController, product);
        form.ShowDialog(this);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var productId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var productName = (string)_grid.CurrentRow.Cells["Наименование"].Value;

        if (MessageBox.Show($"Удалить товар \"{productName}\"?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _productController.DeleteProduct(productId);
        }
    }

    private void InitializeComponent()
    {
        Text = "Номенклатура";
        Size = new Size(1000, 600);
        UITheme.StyleForm(this);
    }
}
