using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

public class RecipientFileParserService
{
    public async Task<List<string>> ExtractEmailsAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new Exception("Recipients file is empty.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" => await ExtractFromCsvAsync(file),
            ".xlsx" => await ExtractFromExcelAsync(file),
            _ => throw new Exception("Only CSV and Excel (.xlsx) files are supported.")
        };
    }

    private async Task<List<string>> ExtractFromCsvAsync(IFormFile file)
    {
        var emails = new List<string>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new Exception("CSV file is empty.");

        var headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
        int emailIndex = headers.FindIndex(h => h.Equals("email", StringComparison.OrdinalIgnoreCase));

        if (emailIndex == -1)
            throw new Exception("CSV must contain an 'email' column.");

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');
            if (values.Length <= emailIndex) continue;

            var email = values[emailIndex].Trim();
            if (!string.IsNullOrWhiteSpace(email))
                emails.Add(email);
        }

        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<string>> ExtractFromExcelAsync(IFormFile file)
    {
        var emails = new List<string>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
            throw new Exception("Excel file is empty.");

        var headerCells = firstRow.CellsUsed().ToList();
        int emailColumnNumber = -1;

        foreach (var cell in headerCells)
        {
            var header = cell.GetString().Trim();
            if (header.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                emailColumnNumber = cell.Address.ColumnNumber;
                break;
            }
        }

        if (emailColumnNumber == -1)
            throw new Exception("Excel file must contain an 'email' column.");

        var rows = worksheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var email = row.Cell(emailColumnNumber).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(email))
                emails.Add(email);
        }

        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}