// ============================================================
// Services/ExportService.cs — Экспорт и печать
// ============================================================
// Вспомогательный сервис для сохранения файлов отчётов.
// Открывает SaveFileDialog, вызывает ReportFactory и
// по желанию открывает сгенерированный файл в системном
// приложении (Excel, Adobe Reader и т.д.).
// ============================================================

using System.Data;

namespace WarehouseAccounting.Services;

public class ExportService
{
    private readonly ReportFactory _factory = new();

    /// <summary>
    /// Показать диалог сохранения, экспортировать данные и открыть файл.
    /// Возвращает путь к сохранённому файлу или null при отмене.
    /// </summary>
    public string? ExportWithDialog(DataTable data, string defaultFileName = "report")
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Экспорт отчёта",
            FileName = defaultFileName,
            Filter = "Excel (*.xlsx)|*.xlsx|PDF (*.pdf)|*.pdf|CSV (*.csv)|*.csv",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() != DialogResult.OK) return null;

        var format = dialog.FilterIndex switch
        {
            1 => "Excel",
            2 => "PDF",
            3 => "CSV",
            _ => "Excel"
        };

        var report = _factory.CreateReport(format);
        report.Generate(data, dialog.FileName);

        // Предложить открыть файл
        if (MessageBox.Show(
            $"Файл сохранён:\n{dialog.FileName}\n\nОткрыть?",
            "Экспорт завершён",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information) == DialogResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dialog.FileName,
                UseShellExecute = true
            });
        }

        return dialog.FileName;
    }
}
