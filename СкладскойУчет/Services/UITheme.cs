namespace WarehouseAccounting.Services;

public static class UITheme
{
    public static readonly Color DarkGreen = Color.FromArgb(27, 94, 32);
    public static readonly Color PrimaryGreen = Color.FromArgb(46, 125, 50);
    public static readonly Color MediumGreen = Color.FromArgb(76, 175, 80);
    public static readonly Color LightGreen = Color.FromArgb(129, 199, 132);
    public static readonly Color VeryLightGreen = Color.FromArgb(200, 230, 201);
    public static readonly Color WarmWhite = Color.FromArgb(245, 245, 240);
    public static readonly Color Surface = Color.White;
    public static readonly Color TextPrimary = Color.FromArgb(51, 51, 51);
    public static readonly Color TextLight = Color.White;
    public static readonly Color BorderLight = Color.FromArgb(220, 220, 220);

    public const int SidebarWidth = 220;
    public static readonly Color SidebarBg = DarkGreen;
    public static readonly Color SidebarHover = Color.FromArgb(33, 109, 38);
    public static readonly Color SidebarActive = Color.FromArgb(56, 142, 60);

    public static Font DefaultFont => new("Segoe UI", 9);
    public static Font BoldFont => new("Segoe UI", 9, FontStyle.Bold);
    public static Font HeaderFont => new("Segoe UI", 14, FontStyle.Bold);
    public static Font SidebarFont => new("Segoe UI", 10, FontStyle.Regular);
    public static Font SidebarHeaderFont => new("Segoe UI", 11, FontStyle.Bold);

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderLight;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowTemplate.Height = 28;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryGreen;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
        grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersHeight = 32;
        grid.RowsDefaultCellStyle.BackColor = Surface;
        grid.RowsDefaultCellStyle.ForeColor = TextPrimary;
        grid.RowsDefaultCellStyle.Font = DefaultFont;
        grid.RowsDefaultCellStyle.SelectionBackColor = LightGreen;
        grid.RowsDefaultCellStyle.SelectionForeColor = TextPrimary;
        grid.AlternatingRowsDefaultCellStyle.BackColor = VeryLightGreen;
    }

    public static void StylePrimaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.Font = DefaultFont;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = PrimaryGreen;
        btn.ForeColor = TextLight;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleDefaultButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.Font = DefaultFont;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderLight;
        btn.BackColor = Surface;
        btn.ForeColor = TextPrimary;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleDangerButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.Font = DefaultFont;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Color.FromArgb(211, 47, 47);
        btn.ForeColor = TextLight;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleTopPanel(Panel panel)
    {
        panel.BackColor = PrimaryGreen;
        panel.ForeColor = TextLight;
    }

    public static void StyleForm(Form form)
    {
        form.BackColor = WarmWhite;
        form.Font = DefaultFont;
    }

    public static Button CreateSidebarButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = SidebarBg,
            ForeColor = TextLight,
            Font = SidebarFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            FlatAppearance = { BorderSize = 0 },
            Cursor = Cursors.Hand
        };
        btn.MouseEnter += (_, _) => { if (btn.BackColor == SidebarBg) btn.BackColor = SidebarHover; };
        btn.MouseLeave += (_, _) => { if (btn.BackColor == SidebarHover) btn.BackColor = SidebarBg; };
        btn.Click += onClick;
        return btn;
    }

    public static Label CreateSidebarHeader(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = Color.FromArgb(180, 255, 180),
            Font = SidebarHeaderFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            BackColor = SidebarBg
        };
    }

    public static Panel CreateSeparator()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(56, 142, 60),
            Margin = new Padding(10, 0, 10, 0)
        };
    }
}
