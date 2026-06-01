using System.Drawing.Printing;
using WarehouseAccounting.Models.Documents;

namespace WarehouseAccounting.Services;

public class PrintService
{
    private readonly Font _titleFont = new("Arial", 14, FontStyle.Bold);
    private readonly Font _headerFont = new("Arial", 10, FontStyle.Bold);
    private readonly Font _normalFont = new("Arial", 9);
    private readonly Font _smallFont = new("Arial", 8);
    private readonly Pen _linePen = new(Color.Black, 1);

    private float _currentY;
    private float _pageWidth;
    private float _margin;
    private float _lineHeight;
    private PrintPageEventArgs? _args;

    public void PrintReceipt(ReceiptDocument document, string supplierName, string warehouseName, Func<int, string> getProductName)
    {
        var printDoc = CreatePrintDocument("Приходная накладная", (_, e) =>
        {
            _currentY = e.MarginBounds.Top;
            _pageWidth = e.MarginBounds.Width;
            _margin = e.MarginBounds.Left;
            _lineHeight = _normalFont.GetHeight(e.Graphics) + 4;
            _args = e;
            DrawDocumentHeader(e, "ПРИХОДНАЯ НАКЛАДНАЯ", document.Number, document.Date);
            DrawReceiptInfo(e, document, supplierName, warehouseName);
            DrawTableHeader(e, new[] { "№", "Товар", "Кол-во", "Цена", "Сумма" }, new[] { 40f, 300f, 80f, 80f, 100f });
            DrawDocumentLines(e, document.Lines, getProductName);
            DrawTotal(e, document.TotalAmount);
        });
        ShowPreview(printDoc);
    }

    public void PrintShipment(ShipmentDocument document, string warehouseName, Func<int, string> getProductName)
    {
        var printDoc = CreatePrintDocument("Расходная накладная", (_, e) =>
        {
            _currentY = e.MarginBounds.Top;
            _pageWidth = e.MarginBounds.Width;
            _margin = e.MarginBounds.Left;
            _lineHeight = _normalFont.GetHeight(e.Graphics) + 4;
            _args = e;
            DrawDocumentHeader(e, "РАСХОДНАЯ НАКЛАДНАЯ", document.Number, document.Date);
            DrawShipmentInfo(e, document, warehouseName);
            DrawTableHeader(e, new[] { "№", "Товар", "Кол-во", "Цена", "Сумма" }, new[] { 40f, 300f, 80f, 80f, 100f });
            DrawDocumentLines(e, document.Lines, getProductName);
            DrawTotal(e, document.TotalAmount);
        });
        ShowPreview(printDoc);
    }

    public void PrintTransfer(TransferDocument document, string sourceWarehouse, string targetWarehouse, Func<int, string> getProductName)
    {
        var printDoc = CreatePrintDocument("Акт перемещения", (_, e) =>
        {
            _currentY = e.MarginBounds.Top;
            _pageWidth = e.MarginBounds.Width;
            _margin = e.MarginBounds.Left;
            _lineHeight = _normalFont.GetHeight(e.Graphics) + 4;
            _args = e;
            DrawDocumentHeader(e, "АКТ ПЕРЕМЕЩЕНИЯ", document.Number, document.Date);
            DrawTransferInfo(e, document, sourceWarehouse, targetWarehouse);
            DrawTableHeader(e, new[] { "№", "Товар", "Кол-во", "Цена", "Сумма" }, new[] { 40f, 300f, 80f, 80f, 100f });
            DrawDocumentLines(e, document.Lines, getProductName);
            DrawTotal(e, document.TotalAmount);
        });
        ShowPreview(printDoc);
    }

    public void PrintInventory(InventoryDocument document, string warehouseName, Func<int, string> getProductName)
    {
        var printDoc = CreatePrintDocument("Акт инвентаризации", (_, e) =>
        {
            _currentY = e.MarginBounds.Top;
            _pageWidth = e.MarginBounds.Width;
            _margin = e.MarginBounds.Left;
            _lineHeight = _normalFont.GetHeight(e.Graphics) + 4;
            _args = e;
            DrawDocumentHeader(e, "АКТ ИНВЕНТАРИЗАЦИИ", document.Number, document.Date);
            DrawInventoryInfo(e, document, warehouseName);
            DrawTableHeader(e, new[] { "№", "Товар", "Учтено", "Факт", "Откл." }, new[] { 40f, 300f, 80f, 80f, 80f });

            int pageIndex = 0;
            foreach (var line in document.Lines.OfType<InventoryLine>())
            {
                pageIndex++;
                var cols = new[]
                {
                    pageIndex.ToString(),
                    getProductName(line.ProductId),
                    line.AccountingQuantity.ToString("N2"),
                    line.ActualQuantity.ToString("N2"),
                    line.Deviation.ToString("N2")
                };
                DrawTableRow(e, cols, new[] { 40f, 300f, 80f, 80f, 80f });
            }

            DrawTotal(e, 0);
        });
        ShowPreview(printDoc);
    }

    private PrintDocument CreatePrintDocument(string title, PrintPageEventHandler printHandler)
    {
        var doc = new PrintDocument();
        doc.PrintPage += printHandler;
        doc.DocumentName = title;
        return doc;
    }

    private void DrawDocumentHeader(PrintPageEventArgs e, string title, string number, DateTime date)
    {
        e.Graphics.DrawString(title, _titleFont, Brushes.Black, _margin, _currentY);
        _currentY += _titleFont.GetHeight(e.Graphics) + 6;

        e.Graphics.DrawString($"№ {number} от {date:dd.MM.yyyy}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _normalFont.GetHeight(e.Graphics) + 10;

        e.Graphics.DrawLine(_linePen, _margin, _currentY, _margin + _pageWidth, _currentY);
        _currentY += 6;
    }

    private void DrawReceiptInfo(PrintPageEventArgs e, ReceiptDocument doc, string supplierName, string warehouseName)
    {
        e.Graphics.DrawString($"Поставщик: {supplierName}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight;
        e.Graphics.DrawString($"Склад: {warehouseName}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight;
        if (!string.IsNullOrWhiteSpace(doc.SupplierInvoiceNumber))
        {
            e.Graphics.DrawString($"Накладная поставщика: {doc.SupplierInvoiceNumber} от {doc.SupplierInvoiceDate:dd.MM.yyyy}", _normalFont, Brushes.Black, _margin, _currentY);
            _currentY += _lineHeight;
        }
        _currentY += 6;
    }

    private void DrawShipmentInfo(PrintPageEventArgs e, ShipmentDocument doc, string warehouseName)
    {
        e.Graphics.DrawString($"Склад: {warehouseName}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight;
        e.Graphics.DrawString($"Получатель: {doc.Recipient}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight;
        e.Graphics.DrawString($"Основание: {doc.Reason}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight + 6;
    }

    private void DrawTransferInfo(PrintPageEventArgs e, TransferDocument doc, string source, string target)
    {
        e.Graphics.DrawString($"Откуда: {source}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight;
        e.Graphics.DrawString($"Куда: {target}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight + 6;
    }

    private void DrawInventoryInfo(PrintPageEventArgs e, InventoryDocument doc, string warehouseName)
    {
        e.Graphics.DrawString($"Склад: {warehouseName}", _normalFont, Brushes.Black, _margin, _currentY);
        _currentY += _lineHeight + 6;
    }

    private void DrawTableHeader(PrintPageEventArgs e, string[] columns, float[] widths)
    {
        float x = _margin;
        for (int i = 0; i < columns.Length; i++)
        {
            var rect = new RectangleF(x, _currentY, widths[i], _lineHeight + 4);
            e.Graphics.DrawRectangle(_linePen, Rectangle.Round(rect));
            e.Graphics.DrawString(columns[i], _headerFont, Brushes.Black, x + 2, _currentY + 2);
            x += widths[i];
        }
        _currentY += _lineHeight + 6;
    }

    private void DrawDocumentLines(PrintPageEventArgs e, IList<DocumentLine> lines, Func<int, string> getProductName)
    {
        int idx = 0;
        foreach (var line in lines)
        {
            idx++;
            var cols = new[]
            {
                idx.ToString(),
                getProductName(line.ProductId),
                line.Quantity.ToString("N2"),
                line.UnitPrice.ToString("N2"),
                line.TotalPrice.ToString("N2")
            };
            DrawTableRow(e, cols, new[] { 40f, 300f, 80f, 80f, 100f });
        }
    }

    private void DrawTableRow(PrintPageEventArgs e, string[] values, float[] widths)
    {
        if (_currentY + _lineHeight > e.MarginBounds.Bottom)
        {
            e.HasMorePages = true;
            return;
        }

        float x = _margin;
        for (int i = 0; i < values.Length; i++)
        {
            var rect = new RectangleF(x, _currentY, widths[i], _lineHeight + 4);
            e.Graphics.DrawRectangle(_linePen, Rectangle.Round(rect));
            e.Graphics.DrawString(values[i], _normalFont, Brushes.Black, x + 2, _currentY + 2);
            x += widths[i];
        }
        _currentY += _lineHeight + 4;
    }

    private void DrawTotal(PrintPageEventArgs e, decimal total)
    {
        _currentY += 6;
        e.Graphics.DrawString($"Итого: {total:N2} ₽", _headerFont, Brushes.Black, _margin + _pageWidth - 200, _currentY);
        _currentY += _lineHeight + 20;

        e.Graphics.DrawString("___________________", _normalFont, Brushes.Black, _margin, _currentY);
        e.Graphics.DrawString("___________________", _normalFont, Brushes.Black, _margin + 300, _currentY);
        _currentY += _normalFont.GetHeight(e.Graphics);

        e.Graphics.DrawString("Подпись", _smallFont, Brushes.Gray, _margin, _currentY);
        e.Graphics.DrawString("Подпись", _smallFont, Brushes.Gray, _margin + 300, _currentY);
    }

    private void ShowPreview(PrintDocument printDoc)
    {
        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            WindowState = FormWindowState.Maximized
        };
        preview.ShowDialog();
    }
}
