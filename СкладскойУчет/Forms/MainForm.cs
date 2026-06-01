using WarehouseAccounting.Controllers;
using WarehouseAccounting.Forms.Customers;
using WarehouseAccounting.Forms.Documents;
using WarehouseAccounting.Forms.Products;
using WarehouseAccounting.Forms.Reports;
using WarehouseAccounting.Forms.Stock;
using WarehouseAccounting.Forms.Suppliers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms;

public partial class MainForm : Form
{
    private readonly AuthController _authController;
    private readonly ProductController _productController = new();
    private readonly StockController _stockController = new();
    private readonly SupplierController _supplierController = new();
    private readonly CustomerController _customerController = new();
    private DocumentController _documentController = null!;
    private ReportController _reportController = null!;

    private TableLayoutPanel _mainLayout = null!;
    private Panel _sidebar = null!;
    private Panel _workspace = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _userLabel = null!;
    private ToolStripStatusLabel _clockLabel = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    public MainForm()
    {
        _authController = new AuthController();
        _authController.Login("admin", "admin");

        InitializeComponent();

        _documentController = new DocumentController(_stockController);
        _reportController = new ReportController(_stockController, _productController, _documentController);

        SetupStatusBar();
        SetupMainLayout();
        SetupSidebar();
        SetupWorkspace();
        SetupObservers();
        UpdateUserInfo();
    }

    private void SetupStatusBar()
    {
        _statusStrip = new StatusStrip
        {
            BackColor = UITheme.DarkGreen,
            ForeColor = UITheme.TextLight
        };

        _statusLabel = new ToolStripStatusLabel("Готово") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _userLabel = new ToolStripStatusLabel("") { TextAlign = ContentAlignment.MiddleRight };
        _clockLabel = new ToolStripStatusLabel("") { TextAlign = ContentAlignment.MiddleRight };

        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _userLabel, _clockLabel });
        Controls.Add(_statusStrip);

        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) => _clockLabel.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        _clockTimer.Start();
    }

    private void SetupMainLayout()
    {
        _mainLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.WarmWhite
        };
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UITheme.SidebarWidth));
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(_mainLayout);

        Resize += (_, _) => LayoutChrome();
        Load += (_, _) => LayoutChrome();
    }

    private void LayoutChrome()
    {
        if (_statusStrip != null && _mainLayout != null)
        {
            _mainLayout.Bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height - _statusStrip.Height);
        }
    }

    private void SetupSidebar()
    {
        _sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.SidebarBg,
            AutoScroll = true
        };
        _mainLayout.Controls.Add(_sidebar, 0, 0);

        var logo = new Label
        {
            Text = "СКЛАДСКОЙ\nУЧЁТ",
            Dock = DockStyle.Top,
            Height = 70,
            ForeColor = UITheme.TextLight,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(21, 75, 25)
        };
        _sidebar.Controls.Add(logo);
        _sidebar.Controls.Add(UITheme.CreateSeparator());

        _sidebar.Controls.Add(UITheme.CreateSidebarHeader("СКЛАД"));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4e5  Приход", (_, _) => OpenChildForm(() => new ReceiptForm(_documentController, _supplierController, _productController, _authController, _stockController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4e4  Расход", (_, _) => OpenChildForm(() => new ShipmentForm(_documentController, _productController, _stockController, _authController, _customerController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4c8  Движения", (_, _) => OpenChildForm(() => new StockMovementForm(_stockController, _productController, _documentController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4ca  Остатки", (_, _) => OpenChildForm(() => new StockListForm(_stockController, _productController))));

        _sidebar.Controls.Add(UITheme.CreateSeparator());
        _sidebar.Controls.Add(UITheme.CreateSidebarHeader("ДОКУМЕНТЫ"));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f504  Перемещения", (_, _) => OpenChildForm(() => new TransferForm(_documentController, _productController, _stockController, _authController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4cb  Инвентаризация", (_, _) => OpenChildForm(() => new InventoryForm(_documentController, _productController, _stockController, _authController))));

        _sidebar.Controls.Add(UITheme.CreateSeparator());
        _sidebar.Controls.Add(UITheme.CreateSidebarHeader("СПРАВОЧНИКИ"));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f465  Поставщики", (_, _) => OpenChildForm(() => new SupplierListForm(_supplierController, _documentController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f91d  Покупатели", (_, _) => OpenChildForm(() => new CustomerListForm(_customerController, _documentController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4e6  Товары", (_, _) => OpenChildForm(() => new ProductListForm(_productController, _stockController))));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f3f7  Категории", (_, _) => ShowCategoryDialog()));
        _sidebar.Controls.Add(UITheme.CreateSidebarButton("  \U0001f4d1  Отчёты", (_, _) => OpenChildForm(() => new ReportForm(_reportController))));

        for (int i = 0; i < _sidebar.Controls.Count; i++)
            _sidebar.Controls.SetChildIndex(_sidebar.Controls[i], _sidebar.Controls.Count - 1 - i);
    }

    private void SetupWorkspace()
    {
        _workspace = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.WarmWhite
        };
        _mainLayout.Controls.Add(_workspace, 1, 0);
        ShowWelcome();
    }

    private void ShowWelcome()
    {
        _workspace.Controls.Clear();

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

        var decor = new Label
        {
            Text = "\U0001f33f",
            Font = new Font("Segoe UI", 56),
            ForeColor = UITheme.PrimaryGreen,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var title = new Label
        {
            Text = "Складской учёт",
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = UITheme.PrimaryGreen,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var subtitle = new Label
        {
            Text = "Система управления складом",
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.FromArgb(150, 150, 150),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var stats = new Label
        {
            Text = $"Товаров: {_productController.Products.Count(p => p.IsActive)}  |  " +
                   $"Складов: {_stockController.Warehouses.Count}  |  " +
                   $"Поставщиков: {_supplierController.Suppliers.Count}",
            Font = new Font("Segoe UI", 11),
            ForeColor = UITheme.MediumGreen,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var hint = new Label
        {
            Text = "Выберите раздел в боковом меню",
            Font = new Font("Segoe UI", 10, FontStyle.Italic),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        content.Controls.Add(decor, 0, 0);
        content.Controls.Add(title, 0, 1);
        content.Controls.Add(subtitle, 0, 2);
        content.Controls.Add(stats, 0, 3);
        content.Controls.Add(hint, 0, 4);

        outer.Controls.Add(content, 0, 1);
        _workspace.Controls.Add(outer);
    }

    private bool _openingForm;

    private void OpenChildForm<T>(Func<T> factory) where T : Form
    {
        if (_openingForm) return;
        _openingForm = true;

        _workspace.Controls.Clear();

        var form = factory();
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Parent = _workspace;
        form.Location = new Point(0, 0);
        form.Size = _workspace.ClientSize;
        form.FormClosed += (_, _) =>
        {
            if (_workspace.Controls.Count == 0)
                ShowWelcome();
        };
        form.Show();

        _workspace.Resize -= OnWorkspaceResize;
        _workspace.Resize += OnWorkspaceResize;

        _openingForm = false;
    }

    private void OnWorkspaceResize(object? sender, EventArgs e)
    {
        if (_workspace.Controls.Count > 0 && _workspace.Controls[0] is Form form)
            form.Size = _workspace.ClientSize;
    }

    private void SetupObservers()
    {
        _productController.StatusChanged += (_, msg) => _statusLabel.Text = msg;
        _stockController.StatusChanged += (_, msg) => _statusLabel.Text = msg;
        _documentController.StatusChanged += (_, msg) => _statusLabel.Text = msg;
        _supplierController.StatusChanged += (_, msg) => _statusLabel.Text = msg;
    }

    private void UpdateUserInfo()
    {
        if (_authController.CurrentUser != null)
        {
            var roleText = _authController.CurrentUser.IsAdmin ? "Администратор" : "Кладовщик";
            _userLabel.Text = $"{_authController.CurrentUser.FullName} ({roleText})";
        }
    }

    private void ShowCategoryDialog()
    {
        using var form = new Form
        {
            Text = "Категории товаров",
            Size = new Size(500, 400),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false
        };
        UITheme.StyleForm(form);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _productController.Categories,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };
        UITheme.StyleGrid(grid);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.DataBindingComplete += (_, _) =>
        {
            if (grid.Columns.Contains("Name")) grid.Columns["Name"].HeaderText = "Название";
            if (grid.Columns.Contains("ParentId")) grid.Columns["ParentId"].HeaderText = "Родитель";
            if (grid.Columns.Contains("Description")) grid.Columns["Description"].HeaderText = "Описание";
            if (grid.Columns.Contains("IsActive")) { grid.Columns["IsActive"].HeaderText = "Активность"; grid.Columns["IsActive"].Width = 80; }
            if (grid.Columns.Contains("Children")) grid.Columns["Children"].Visible = false;
            if (grid.Columns.Contains("Id")) grid.Columns["Id"].Width = 40;
        };

        var btnAdd = new Button { Text = "Добавить", Dock = DockStyle.Bottom, Height = 30 };
        UITheme.StylePrimaryButton(btnAdd);
        btnAdd.Click += (_, _) =>
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Название категории:", "Новая категория", "");
            if (!string.IsNullOrWhiteSpace(name))
                _productController.AddCategory(new Category { Name = name.Trim() });
        };

        var btnDelete = new Button { Text = "Удалить", Dock = DockStyle.Bottom, Height = 30 };
        UITheme.StyleDangerButton(btnDelete);
        btnDelete.Click += (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is Category cat)
            {
                try { _productController.DeleteCategory(cat.Id); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        };

        form.Controls.Add(grid);
        form.Controls.Add(btnDelete);
        form.Controls.Add(btnAdd);
        form.ShowDialog(this);
    }

    private void InitializeComponent()
    {
        Text = "Складской учёт — Комаричева Ирина Романовна, ИСс-32";
        WindowState = FormWindowState.Maximized;
        Size = new Size(1400, 900);
        MinimumSize = new Size(1024, 600);
        StartPosition = FormStartPosition.CenterScreen;
    }
}
