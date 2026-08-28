using ClosedXML.Excel;

namespace FamilyTree.Services.Import;

public sealed class ExcelSheetReader : IDisposable
{
    private readonly XLWorkbook _wb;
    private readonly IXLWorksheet _ws;
    private readonly Dictionary<string, int> _colIndex;

    public ExcelSheetReader(string filePath, string sheetName = "数据")
    {
        _wb = new XLWorkbook(filePath);
        _ws = _wb.Worksheet(sheetName) ?? _wb.Worksheets.First();
        _colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = _ws.FirstRowUsed()?.RowNumber() ?? 1;
        var lastCol = _ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var c = 1; c <= lastCol; c++)
        {
            var name = _ws.Cell(headerRow, c).GetString().Trim();
            if (name.Length > 0)
                _colIndex[name] = c;
        }
        HeaderRow = headerRow;
        DataStartRow = headerRow + 1;
        LastDataRow = _ws.LastRowUsed()?.RowNumber() ?? headerRow;
    }

    public int HeaderRow { get; }
    public int DataStartRow { get; }
    public int LastDataRow { get; }

    public IEnumerable<int> DataRows()
    {
        for (var r = DataStartRow; r <= LastDataRow; r++)
            yield return r;
    }

    public string GetString(int row, string column)
    {
        if (!_colIndex.TryGetValue(column, out var c))
            return "";
        return _ws.Cell(row, c).GetString().Trim();
    }

    public bool HasColumn(string column) => _colIndex.ContainsKey(column);

    public bool IsRowEmpty(int row, params string[] keyColumns)
    {
        foreach (var col in keyColumns)
        {
            if (GetString(row, col).Length > 0)
                return false;
        }
        return true;
    }

    public void Dispose() => _wb.Dispose();
}

public static class ImportParse
{
    public static string Or(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

    public static int Int(string? value, int defaultValue = 0) =>
        int.TryParse(value?.Trim(), out var n) ? n : defaultValue;

    public static bool Bit(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        var v = value.Trim();
        return v is "1" or "true" or "True" or "是" or "Y" or "y";
    }

    public static bool? NullableBit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Bit(value);
    }

    public static DateTime? Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value.Trim(), out var d) ? d.Date : null;
    }
}
