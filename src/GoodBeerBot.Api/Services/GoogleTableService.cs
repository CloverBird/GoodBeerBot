using GoodBeerBot.Api.Configurations;
using GoodBeerBot.Api.Models;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace GoodBeerBot.Api.Services;

public class GoogleTableService : ITableService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;

    private const string SHEET_NAME = "СРОКИ";
    private const string RESPONSE_SHEET_NAME = "ПОЗИЦИИ";
    private const string REPORTS_SHEET_NAME = "ОТЧЁТЫ";

    public GoogleTableService(SheetsService sheetsService, GoogleTablesConfiguration googleTablesConfiguration)
    {
        _sheetsService = sheetsService;
        _spreadsheetId = googleTablesConfiguration.SheetId;
    }

    public async Task<List<DataItem>> GetDataItemsAsync()
    {
        var values = await ReadAllAsync(SHEET_NAME);
        if (values.Count == 0) return new List<DataItem>();

        var headers = values[0].Select(v => v?.ToString() ?? "").ToArray();
        int idxDays = IndexOf(headers, "осталось дней");
        int idxName = IndexOf(headers, "продукт");
        int idxLeft = IndexOf(headers, "остаток");
        int idxExpiry = IndexOf(headers, "дата окончания");

        if (new[] { idxDays, idxName, idxLeft, idxExpiry }.Any(i => i < 0))
            return new List<DataItem>();

        var list = new List<DataItem>();
        foreach (var row in values.Skip(1))
        {
            string name = GetCell(row, idxName);
            var str1 = GetCell(row, idxLeft);
            double? left = string.IsNullOrEmpty(str1) ? null : double.Parse(str1);
            var str2 = GetCell(row, idxDays);
            int? days = string.IsNullOrEmpty(str2) ? null : int.Parse(str2);
            var str3 = GetCell(row, idxExpiry);
            DateOnly? expiry = string.IsNullOrEmpty(str3) ? null : DateOnly.ParseExact(str3, "dd.MM.yyyy");
            list.Add(new DataItem(name, left, days, expiry));
        }
        return list;
    }

    public async Task<List<Position>> ReadPositionsAsync()
    {
        var values = await ReadAllAsync(RESPONSE_SHEET_NAME);
        if (values.Count < 2) return new();

        var list = new List<Position>();
        foreach (var row in values.Skip(1))
        {
            var name = GetCell(row, 2);
            if (string.IsNullOrWhiteSpace(name)) continue;


            var expiryStr = GetCell(row, 3);
            if (!DateOnly.TryParse(expiryStr, out var expiry))
                continue;

            list.Add(new Position(name, expiry));
        }

        return list;
    }

    public async Task AppendReportRowAsync(DateTime when, long chatId, string point, string name, DateOnly? expiry, int qty)
    {
        await EnsureSheetWithHeaderAsync(
            REPORTS_SHEET_NAME,
            new[] { "Дата/Время", "Chat ID", "Точка", "Наименование", "Срок годности до", "Остаток" });

        var rows = new List<IList<object>>
        {
            new List<object>
            {
                when.ToString("dd.MM.yyyy HH:mm:ss"),
                chatId,
                point,
                name,
                expiry?.ToString("dd.MM.yyyy"),
                qty
            }
        };

        var req = _sheetsService.Spreadsheets.Values.Append(
            new ValueRange { Values = rows },
            _spreadsheetId,
            $"{REPORTS_SHEET_NAME}!A:F"
        );
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await req.ExecuteAsync();
    }

    public async Task SavePositionsToSheetAsync(long chatId, IEnumerable<Position> positions)
    {
        await EnsureSheetWithHeaderAsync(RESPONSE_SHEET_NAME, new[] { "Дата/Время", "Chat ID", "Наименование", "Срок годности до" });

        await ClearBodyAsync(RESPONSE_SHEET_NAME);

        // form data
        var rows = positions.Select(p => new List<object?>
        {
            DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            chatId,
            p.Name,
            p.Expiry?.ToString("dd.MM.yyyy")
        }).Cast<IList<object>>().ToList();

        if (rows.Count == 0) return;

        // add to table
        var req = _sheetsService.Spreadsheets.Values.Append(
            new ValueRange { Values = rows },
            _spreadsheetId,
            $"{RESPONSE_SHEET_NAME}!A:D"
        );
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        await req.ExecuteAsync();
    }

    #region Helpers
    private async Task<IList<IList<object>>> ReadAllAsync(string sheetName)
    {
        var resp = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{sheetName}!A:Z").ExecuteAsync();
        return resp.Values ?? new List<IList<object>>();
    }

    private static int IndexOf(string[] headers, string strToFind)
    {
        int i = 0;
        foreach (var h in headers)
        {
            if (string.Equals(h?.Trim(), strToFind, StringComparison.OrdinalIgnoreCase))
                return i;
            i++;
        }
        return -1;
    }

    private static string GetCell(IList<object> row, int idx) =>
        (idx >= 0 && idx < row.Count) ? row[idx]?.ToString() ?? "" : "";

    private async Task ClearBodyAsync(string sheetName)
    {
        var clear = new ClearValuesRequest();
        await _sheetsService.Spreadsheets.Values.Clear(clear, _spreadsheetId, $"{sheetName}!A2:Z").ExecuteAsync();
    }

    public async Task ClearReportsSheet()
    {
        var clear = new ClearValuesRequest();
        await _sheetsService.Spreadsheets.Values.Clear(clear, _spreadsheetId, $"{REPORTS_SHEET_NAME}!A2:Z").ExecuteAsync();
    }

    private async Task EnsureSheetWithHeaderAsync(string sheetName, string[] headers)
    {
        var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();

        // check if this sheet exists
        var sheet = spreadsheet.Sheets.FirstOrDefault(s =>
            string.Equals(s.Properties.Title, sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
        {
            var addSheetRequest = new AddSheetRequest
            {
                Properties = new SheetProperties { Title = sheetName }
            };

            var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request> { new Request { AddSheet = addSheetRequest } }
            };

            await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _spreadsheetId).ExecuteAsync();
        }

        // Записуємо заголовки в перший рядок
        var headerRange = $"{sheetName}!A1:{GetColumnLetter(headers.Length)}1";
        var headerData = new ValueRange
        {
            Values = new List<IList<object>> { headers.Cast<object>().ToList() }
        };

        var req = _sheetsService.Spreadsheets.Values.Update(headerData, _spreadsheetId, headerRange);

        req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await req.ExecuteAsync();
    }

    private static string GetColumnLetter(int colNumber)
    {
        // Конвертує індекс стовпця (1-based) у буквену позначку (A, B, C...)
        string columnString = "";
        while (colNumber > 0)
        {
            int currentLetterNumber = (colNumber - 1) % 26;
            char currentLetter = (char)(currentLetterNumber + 65);
            columnString = currentLetter + columnString;
            colNumber = (colNumber - (currentLetterNumber + 1)) / 26;
        }
        return columnString;
    }
    #endregion
}