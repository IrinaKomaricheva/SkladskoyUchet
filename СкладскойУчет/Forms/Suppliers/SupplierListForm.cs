using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Suppliers;

public partial class SupplierListForm : Form
{
    private readonly SupplierController _supplierController;
    private readonly DocumentController _documentController;

    private Panel _topPanel = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkActiveOnly = null!;
    private DataGridView _grid = null!;
    private Panel _bottomPanel = null!;

    public SupplierListForm(SupplierController supplierController, DocumentController documentController)
    {
        _supplierController = supplierController;
        _documentController = documentController;
        InitializeComponent();
        SetupUI();
        SetupObservers();
        RefreshGrid();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };

        _txtSearch = new TextBox { Location = new Point(5, 8), Size = new Size(300, 25), PlaceholderText = "🔍 Поиск по названию / ИНН..." };
        _txtSearch.TextChanged += (_, _) => RefreshGrid();

        _chkActiveOnly = new CheckBox { Text = "Только активные", Location = new Point(315, 8), Size = new Size(130, 25), Checked = true };
        _chkActiveOnly.CheckedChanged += (_, _) => RefreshGrid();

        _topPanel.Controls.AddRange(new Control[] { _txtSearch, _chkActiveOnly });

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
        _grid.DataBindingComplete += (_, _) => AdjustColumns();
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
        _supplierController.DataChanged += (_, _) => RefreshGrid();
    }

    private void RefreshGrid()
    {
        var searchText = _txtSearch.Text;
        var suppliers = _supplierController.Search(searchText);

        if (_chkActiveOnly.Checked)
            suppliers = suppliers.Where(s => s.IsActive).ToList();

        var displayData = suppliers.Select(s => new
        {
            s.Id,
            Наименование = s.Name,
            Кратко = s.ShortName,
            ИНН = s.INN,
            Контакт = s.ContactPerson,
            Телефон = s.Phone,
            Email = s.Email,
            Активность = s.IsActive
        }).ToList();

        _grid.DataSource = displayData;
    }

    private void AdjustColumns()
    {
        if (_grid.Columns.Count == 0) return;
        if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Width = 40;
        if (_grid.Columns.Contains("ИНН")) _grid.Columns["ИНН"].Width = 100;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new SupplierEditForm(_supplierController);
        form.ShowDialog(this);
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var supplierId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var supplier = _supplierController.GetById(supplierId);
        if (supplier == null) return;

        using var form = new SupplierEditForm(_supplierController, supplier);
        form.ShowDialog(this);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var supplierId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var supplierName = (string)_grid.CurrentRow.Cells["Наименование"].Value;

        if (MessageBox.Show($"Удалить поставщика \"{supplierName}\"?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _supplierController.DeleteSupplier(supplierId);
        }
    }

    private void InitializeComponent()
    {
        Text = "Поставщики";
        Size = new Size(900, 550);
        UITheme.StyleForm(this);
    }
}
