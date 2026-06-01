using WarehouseAccounting.Controllers;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting.Forms.Customers;

public partial class CustomerEditForm : Form
{
    private readonly CustomerController _customerController;
    private readonly Customer? _existingCustomer;

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

    public CustomerEditForm(CustomerController customerController, Customer? existingCustomer = null)
    {
        _customerController = customerController;
        _existingCustomer = existingCustomer;
        InitializeComponent();
        SetupUI();

        if (_existingCustomer != null) FillFields(_existingCustomer);
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
            Text = "Сохранить", Location = new Point(10, 10), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = UITheme.PrimaryGreen, ForeColor = Color.White
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

    private void FillFields(Customer customer)
    {
        _txtName.Text = customer.Name;
        _txtShortName.Text = customer.ShortName;
        _txtINN.Text = customer.INN;
        _txtKPP.Text = customer.KPP;
        _txtContactPerson.Text = customer.ContactPerson;
        _txtPhone.Text = customer.Phone;
        _txtEmail.Text = customer.Email;
        _txtAddress.Text = customer.Address;
        _txtPaymentTerms.Text = customer.PaymentTerms;
        _txtNotes.Text = customer.Notes;
        _chkIsActive.Checked = customer.IsActive;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var customer = new Customer
        {
            Id = _existingCustomer?.Id ?? 0,
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

        var errors = ValidationService.ValidateCustomer(customer);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Ошибка валидации",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_existingCustomer == null)
            _customerController.AddCustomer(customer);
        else
            _customerController.UpdateCustomer(customer);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void InitializeComponent()
    {
        Text = _existingCustomer == null ? "Новый покупатель" : "Редактирование покупателя";
        Size = new Size(600, 450);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
    }
}
