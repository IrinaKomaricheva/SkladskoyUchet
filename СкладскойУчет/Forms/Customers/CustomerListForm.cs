using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Customers;

public partial class CustomerListForm : Form
{
    private readonly CustomerController _customerController;
    private readonly DocumentController _documentController;

    private Panel _topPanel = null!;
    private TextBox _txtSearch = null!;
    private CheckBox _chkActiveOnly = null!;
    private DataGridView _grid = null!;
    private Panel _bottomPanel = null!;

    public CustomerListForm(CustomerController customerController, DocumentController documentController)
    {
        _customerController = customerController;
        _documentController = documentController;
        InitializeComponent();
        SetupUI();
        SetupObservers();
        RefreshGrid();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };

        _txtSearch = new TextBox { Location = new Point(5, 8), Size = new Size(300, 25), PlaceholderText = "Поиск по названию / ИНН..." };
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
        _customerController.DataChanged += (_, _) => RefreshGrid();
    }

    private void RefreshGrid()
    {
        var searchText = _txtSearch.Text;
        var customers = _customerController.Search(searchText);

        if (_chkActiveOnly.Checked)
            customers = customers.Where(c => c.IsActive).ToList();

        var displayData = customers.Select(c => new
        {
            c.Id,
            Наименование = c.Name,
            Кратко = c.ShortName,
            ИНН = c.INN,
            Контакт = c.ContactPerson,
            Телефон = c.Phone,
            Email = c.Email,
            Активность = c.IsActive
        }).ToList();

        _grid.DataSource = displayData;
    }

    private void AdjustColumns()
    {
        if (_grid.Columns.Count == 0) return;
        if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Width = 40;
        if (_grid.Columns.Contains("ИНН")) _grid.Columns["ИНН"].Width = 100;
        if (_grid.Columns.Contains("Активность")) _grid.Columns["Активность"].Width = 80;

        if (_grid.Columns.Contains("Активность"))
        {
            var col = _grid.Columns["Активность"];
            var dataProp = col.DataPropertyName;
            var displayIndex = col.DisplayIndex;
            _grid.Columns.Remove(col);
            var chk = new DataGridViewCheckBoxColumn
            {
                Name = "Активность",
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

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new CustomerEditForm(_customerController);
        form.ShowDialog(this);
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var customerId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var customer = _customerController.GetById(customerId);
        if (customer == null) return;

        using var form = new CustomerEditForm(_customerController, customer);
        form.ShowDialog(this);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow == null) return;

        var customerId = (int)_grid.CurrentRow.Cells["Id"].Value;
        var customerName = (string)_grid.CurrentRow.Cells["Наименование"].Value;

        if (MessageBox.Show($"Удалить покупателя \"{customerName}\"?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _customerController.DeleteCustomer(customerId);
        }
    }

    private void InitializeComponent()
    {
        Text = "Покупатели";
        Size = new Size(900, 550);
        UITheme.StyleForm(this);
    }
}
