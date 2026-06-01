using System.Data;
using System.Text;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace WarehouseAccounting.Services;

public interface IReport
{
    string FileExtension { get; }
    string Description { get; }
    void Generate(DataTable data, string filePath);
}

public class ExcelReport : IReport
{
    public string FileExtension => ".xlsx";
    public string Description => "Excel таблица";

    public void Generate(DataTable data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Отчёт");

        for (int i = 0; i < data.Columns.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = data.Columns[i].ColumnName;
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int row = 0; row < data.Rows.Count; row++)
            for (int col = 0; col < data.Columns.Count; col++)
                sheet.Cell(row + 2, col + 1).Value = data.Rows[row][col]?.ToString();

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}

public class PdfReport : IReport
{
    public string FileExtension => ".pdf";
    public string Description => "PDF документ";

    public void Generate(DataTable data, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        var document = new Document(PageSize.A4.Rotate(), 10, 10, 20, 20);
        var writer = PdfWriter.GetInstance(document, stream);
        document.Open();

        var titleFont = FontFactory.GetFont("Arial", 14, iTextSharp.text.Font.BOLD);
        var headerFont = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.BOLD);
        var normalFont = FontFactory.GetFont("Arial", 9);

        document.Add(new Paragraph("Отчёт", titleFont));
        document.Add(new Paragraph($"Сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont));
        document.Add(new Paragraph(" "));

        var table = new PdfPTable(data.Columns.Count);
        table.WidthPercentage = 100;

        foreach (DataColumn col in data.Columns)
        {
            var cell = new PdfPCell(new Phrase(col.ColumnName, headerFont));
            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 4;
            table.AddCell(cell);
        }

        foreach (DataRow row in data.Rows)
        {
            foreach (var item in row.ItemArray)
            {
                var cell = new PdfPCell(new Phrase(item?.ToString() ?? "", normalFont));
                cell.Padding = 3;
                table.AddCell(cell);
            }
        }

        document.Add(table);
        document.Close();
    }
}

public class CsvReport : IReport
{
    public string FileExtension => ".csv";
    public string Description => "CSV файл";

    public void Generate(DataTable data, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        writer.WriteLine(string.Join(";",
            data.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName)));

        foreach (System.Data.DataRow row in data.Rows)
            writer.WriteLine(string.Join(";", row.ItemArray.Select(v => v?.ToString() ?? "")));
    }
}

public class ReportFactory
{
    public IReport CreateReport(string format)
    {
        return format.ToLower() switch
        {
            "excel" => new ExcelReport(),
            "pdf"   => new PdfReport(),
            "csv"   => new CsvReport(),
            _ => throw new ArgumentException($"Неизвестный формат отчёта: {format}")
        };
    }

    public static List<string> GetAvailableFormats() => ["Excel", "PDF", "CSV"];
}
