using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Suppliers;

public partial class SupplierEditForm : Form
{
    private readonly SupplierController _supplierController;
    private readonly Supplier? _existingSupplier;

    private TableLayoutPanel _table = null!;
    private TextBox _txtName = null!;
    private TextBox _txtShortName = null!;
    private TextBox _txtINN = null!;
    private TextBox _txtKPP = null!;
    private TextBox _txtContactPerson = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtAddress = null!;
    private TextBox _txtPaymentTerms = null!;
    private TextBox _txtNotes = null!;
    private CheckBox _chkIsActive = null!;

    public SupplierEditForm(SupplierController supplierController, Supplier? existingSupplier = null)
    {
        _supplierController = supplierController;
        _existingSupplier = existingSupplier;
        InitializeComponent();
        SetupUI();

        if (_existingSupplier != null) FillFields(_existingSupplier);
    }

    private void SetupUI()
    {
        _table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(10),
            AutoSize = true
        };
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _txtName = new TextBox();
        _txtShortName = new TextBox();
        _txtINN = new TextBox();
        _txtKPP = new TextBox();
        _txtContactPerson = new TextBox();
        _txtPhone = new TextBox();
        _txtEmail = new TextBox();
        _txtAddress = new TextBox();
        _txtPaymentTerms = new TextBox();
        _txtNotes = new TextBox { Multiline = true, Height = 60 };
        _chkIsActive = new CheckBox { Text = "Активен", Checked = true };

        AddRow("Наименование:", _txtName, 0, 2);
        AddRow("Кратко:", _txtShortName);
        AddRow("ИНН:", _txtINN, 0, 2);
        AddRow("КПП:", _txtKPP);
        AddRow("Конт. лицо:", _txtContactPerson, 0, 2);
        AddRow("Телефон:", _txtPhone);
        AddRow("Email:", _txtEmail, 0, 2);
        AddRow("Адрес:", _txtAddress);
        AddRow("Условия оплаты:", _txtPaymentTerms);
        AddRow("Примечания:", _txtNotes);
        AddRow("", _chkIsActive);

        var panel = new Panel { Height = 50, Dock = DockStyle.Bottom };
        var btnSave = new Button
        {
            Text = "💾 Сохранить", Location = new Point(10, 10), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = Color.DodgerBlue, ForeColor = Color.White
        };
        btnSave.Click += BtnSave_Click;
        var btnCancel = new Button { Text = "Отмена", Location = new Point(140, 10), Size = new Size(120, 30) };
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        panel.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(_table);
        Controls.Add(panel);
    }

    private void AddRow(string label, Control control, int? colSpan = null, int? colSpan2 = null)
    {
        var row = _table.RowCount++;
        _table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        if (colSpan.HasValue && colSpan.Value > 1)
            _table.SetColumnSpan(control, colSpan.Value);
        _table.Controls.Add(control, 1, row);
    }

    private void FillFields(Supplier supplier)
    {
        _txtName.Text = supplier.Name;
        _txtShortName.Text = supplier.ShortName;
        _txtINN.Text = supplier.INN;
        _txtKPP.Text = supplier.KPP;
        _txtContactPerson.Text = supplier.ContactPerson;
        _txtPhone.Text = supplier.Phone;
        _txtEmail.Text = supplier.Email;
        _txtAddress.Text = supplier.Address;
        _txtPaymentTerms.Text = supplier.PaymentTerms;
        _txtNotes.Text = supplier.Notes;
        _chkIsActive.Checked = supplier.IsActive;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var supplier = new Supplier
        {
            Id = _existingSupplier?.Id ?? 0,
            Name = _txtName.Text.Trim(),
            ShortName = _txtShortName.Text.Trim(),
            INN = _txtINN.Text.Trim(),
            KPP = _txtKPP.Text.Trim(),
            ContactPerson = _txtContactPerson.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            Address = _txtAddress.Text.Trim(),
            PaymentTerms = _txtPaymentTerms.Text.Trim(),
            Notes = _txtNotes.Text.Trim(),
            IsActive = _chkIsActive.Checked
        };

        var errors = ValidationService.ValidateSupplier(supplier);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Ошибка валидации",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_existingSupplier == null)
            _supplierController.AddSupplier(supplier);
        else
            _supplierController.UpdateSupplier(supplier);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void InitializeComponent()
    {
        Text = _existingSupplier == null ? "Новый поставщик" : "Редактирование поставщика";
        Size = new Size(600, 450);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
    }
}
