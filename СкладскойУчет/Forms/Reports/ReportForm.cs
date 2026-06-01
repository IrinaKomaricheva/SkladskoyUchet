using System.Data;
using WarehouseAccounting.Controllers;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Reports;

public partial class ReportForm : Form
{
    private readonly ReportController _reportController;
    private readonly StockController _stockController;
    private readonly ExportService _exportService = new();

    private Panel _topPanel = null!;
    private ComboBox _cmbReportType = null!;
    private ComboBox _cmbWarehouse = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Button _btnGenerate = null!;
    private DataGridView _grid = null!;
    private Panel _bottomPanel = null!;
    private Label _lblRowCount = null!;

    private DataTable? _currentReportData;

    public ReportForm(ReportController reportController)
    {
        _reportController = reportController;
        _stockController = new Controllers.StockController();
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        _topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };

        var lblType = new Label { Text = "Отчёт:", Location = new Point(5, 13), Size = new Size(45, 25) };
        _cmbReportType = new ComboBox { Location = new Point(55, 13), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbReportType.Items.AddRange(new[] { "Остатки на складе", "Обороты за период", "Дефицит товаров", "Поступления за период", "Расход за период" });
        _cmbReportType.SelectedIndex = 0;
        _cmbReportType.SelectedIndexChanged += (_, _) => ToggleFilters();

        var lblWarehouse = new Label { Text = "Склад:", Location = new Point(265, 13), Size = new Size(50, 25) };
        _cmbWarehouse = new ComboBox { Location = new Point(320, 13), Size = new Size(140, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbWarehouse.Items.Add("Все склады");
        foreach (var w in _stockController.Warehouses) _cmbWarehouse.Items.Add(w);
        _cmbWarehouse.SelectedIndex = 0;

        var lblFrom = new Label { Text = "с:", Location = new Point(470, 13), Size = new Size(20, 25) };
        _dtpFrom = new DateTimePicker { Location = new Point(490, 13), Size = new Size(130, 25), Value = DateTime.Today.AddMonths(-1) };
        var lblTo = new Label { Text = "по:", Location = new Point(625, 13), Size = new Size(25, 25) };
        _dtpTo = new DateTimePicker { Location = new Point(650, 13), Size = new Size(130, 25), Value = DateTime.Today };

        _btnGenerate = new Button { Text = "Сформировать", Location = new Point(790, 10), Size = new Size(130, 28) };
        UITheme.StylePrimaryButton(_btnGenerate);
        _btnGenerate.Click += BtnGenerate_Click;

        _topPanel.Controls.AddRange(new Control[] { lblType, _cmbReportType, lblWarehouse, _cmbWarehouse, lblFrom, _dtpFrom, lblTo, _dtpTo, _btnGenerate });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UITheme.StyleGrid(_grid);

        _bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        _lblRowCount = new Label { Text = "Строк: 0", Location = new Point(10, 8), Size = new Size(150, 25), Font = new Font("Arial", 9, FontStyle.Bold) };

        var btnExportExcel = new Button { Text = "Excel", Location = new Point(200, 7), Size = new Size(90, 28) };
        UITheme.StylePrimaryButton(btnExportExcel);
        btnExportExcel.Click += BtnExportExcel_Click;
        var btnExportPdf = new Button { Text = "PDF", Location = new Point(295, 7), Size = new Size(90, 28) };
        UITheme.StyleDefaultButton(btnExportPdf);
        btnExportPdf.Click += BtnExportPdf_Click;
        var btnExportCsv = new Button { Text = "CSV", Location = new Point(390, 7), Size = new Size(90, 28) };
        UITheme.StyleDefaultButton(btnExportCsv);
        btnExportCsv.Click += BtnExportCsv_Click;

        _bottomPanel.Controls.AddRange(new Control[] { _lblRowCount, btnExportExcel, btnExportPdf, btnExportCsv });

        Controls.Add(_grid);
        Controls.Add(_bottomPanel);
        Controls.Add(_topPanel);

        ToggleFilters();
    }

    private void ToggleFilters()
    {
        var index = _cmbReportType.SelectedIndex;
        var needsWarehouse = index == 0 || index == 3 || index == 4;
        var needsPeriod = index == 1;

        _cmbWarehouse.Visible = needsWarehouse;
        _dtpFrom.Visible = index == 1 || index == 3 || index == 4;
        _dtpTo.Visible = index == 1 || index == 3 || index == 4;
    }

    private void BtnGenerate_Click(object? sender, EventArgs e)
    {
        try
        {
            _currentReportData = _cmbReportType.SelectedIndex switch
            {
                0 => _reportController.GetStockReport(GetWarehouseFilter()),
                1 => _reportController.GetTurnoverReport(_dtpFrom.Value, _dtpTo.Value),
                2 => _reportController.GetShortageReport(),
                3 => _reportController.GetReceiptsReport(_dtpFrom.Value, _dtpTo.Value),
                4 => _reportController.GetShipmentsReport(_dtpFrom.Value, _dtpTo.Value),
                _ => new DataTable()
            };

            _grid.DataSource = _currentReportData;
            _lblRowCount.Text = $"Строк: {_currentReportData?.Rows.Count ?? 0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private int? GetWarehouseFilter()
    {
        if (_cmbWarehouse.SelectedIndex > 0 && _cmbWarehouse.SelectedItem is WarehouseAccounting.Models.Warehouse w)
            return w.Id;
        return null;
    }

    private void BtnExportExcel_Click(object? sender, EventArgs e)
    {
        if (_currentReportData == null) return;
        _exportService.ExportWithDialog(_currentReportData, GetReportFileName("xlsx"));
    }

    private void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        if (_currentReportData == null) return;
        _exportService.ExportWithDialog(_currentReportData, GetReportFileName("pdf"));
    }

    private void BtnExportCsv_Click(object? sender, EventArgs e)
    {
        if (_currentReportData == null) return;
        _exportService.ExportWithDialog(_currentReportData, GetReportFileName("csv"));
    }

    private string GetReportFileName(string ext)
    {
        var reportName = _cmbReportType?.Text ?? "report";
        return $"{reportName}_{DateTime.Today:yyyyMMdd}.{ext}";
    }

    private void InitializeComponent()
    {
        Text = "Отчёты";
        Size = new Size(1100, 650);
        UITheme.StyleForm(this);
    }
}
