using System.ComponentModel;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
public class GenericGoogleSheetsService
{
    private readonly string[] _scopes = [SheetsService.Scope.Spreadsheets];
    private readonly string _spreadsheetId;
    private readonly SheetsService _sheetsService;
    private const string listDelimiter = "@CFD ";

    public GenericGoogleSheetsService(string credentials, string spreadsheetId)
    {
        _spreadsheetId = spreadsheetId;

        GoogleCredential credential;
        if (credentials.Trim().StartsWith("{"))
        {
            credential = GoogleCredential.FromJson(credentials).CreateScoped(_scopes);
        }
        else
        {
            using var stream = new FileStream(credentials, FileMode.Open, FileAccess.Read);
            credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
        }

        _sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "n8n-server",
        });
    }

    // --- READ (All) ---
    public async Task<List<T>> GetAllAsync<T>(string sheetName = "Sheet1") where T : class, IEntityWithId, new()
    {
        var (headerMap, allValues) = await GetSheetDataWithHeaderMap(sheetName);
        if (allValues == null || !allValues.Any()) return [];

        var items = new List<T>();
        var properties = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);

        foreach (var row in allValues)
        {
            var item = new T();
            foreach (var header in headerMap)
            {
                if (!properties.TryGetValue(header.Key, out var prop)) continue;
                if (header.Value >= row.Count) continue;

                var cellValue = row[header.Value]?.ToString();
                SetPropertyValue(item, prop, cellValue);
            }

            items.Add(item);
        }

        return items;
    }

    // --- CREATE ---
    public async Task<T> AddAsync<T>(T item, string sheetName = "Sheet1") where T : class, IEntityWithId, new()
    {
        item.Id = Guid.NewGuid().ToString();

        var (headerMap, _) = await GetSheetDataWithHeaderMap(sheetName);
        var valueRow = new object[headerMap.Count];
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (headerMap.TryGetValue(prop.Name, out var colIndex))
            {
                valueRow[colIndex] = FormatPropertyValue(prop.GetValue(item));
            }
        }

        var valueRange = new ValueRange { Values = [valueRow.ToList()] };
        var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, $"{sheetName}!A1");
        appendRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await appendRequest.ExecuteAsync();

        return item;
    }

    // --- UPDATE ---
    public async Task<bool> UpdateAsync<T>(T itemToUpdate, string sheetName = "Sheet1")
        where T : class, IEntityWithId, new()
    {
        var (rowIndex, _) = await FindRowByIdAsync(itemToUpdate.Id, sheetName);
        if (rowIndex == -1) return false;

        var (headerMap, _) = await GetSheetDataWithHeaderMap(sheetName);
        var valueRow = new object[headerMap.Count];
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (headerMap.TryGetValue(prop.Name, out var colIndex))
            {
                valueRow[colIndex] = FormatPropertyValue(prop.GetValue(itemToUpdate));
            }
        }

        var range = $"{sheetName}!A{rowIndex}:{GetColumnName(headerMap.Count)}{rowIndex}";
        var valueRange = new ValueRange { Values = [valueRow.ToList()] };
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        updateRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync();

        return true;
    }

    // --- DELETE ---
    public async Task<bool> DeleteAsync(string id, string sheetName = "Sheet1")
    {
        var (rowIndex, sheetId) = await FindRowByIdAsync(id, sheetName);
        if (rowIndex == -1 || sheetId == null) return false;

        var deleteRequest = new Request
        {
            DeleteDimension = new DeleteDimensionRequest
            {
                Range = new DimensionRange
                    { SheetId = sheetId, Dimension = "ROWS", StartIndex = rowIndex - 1, EndIndex = rowIndex }
            }
        };
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = [deleteRequest] };
        await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _spreadsheetId).ExecuteAsync();
        return true;
    }

    #region Helper Methods

    private async Task<(Dictionary<string, int> headerMap, IList<IList<object>>? values)> GetSheetDataWithHeaderMap(
        string sheetName = "Sheet1")
    {
        var range = $"{sheetName}!A1:G";
        var response = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
        var allValues = response.Values;

        if (allValues == null || !allValues.Any())
        {
            return (new Dictionary<string, int>(), null);
        }

        var headerMap = allValues[0]
            .Select((header, index) => new { Name = header.ToString(), Index = index })
            .Where(h => !string.IsNullOrEmpty(h.Name))
            .ToDictionary(h => h.Name!, h => h.Index);

        return (headerMap, allValues.Skip(1).ToList());
    }

    private async Task<(int rowIndex, int? sheetId)> FindRowByIdAsync(string id, string sheetName = "Sheet1")
    {
        var (headerMap, allValues) = await GetSheetDataWithHeaderMap(sheetName);
        if (!headerMap.TryGetValue("Id", out var idColIndex) || allValues == null)
        {
            return (-1, null);
        }

        for (var i = 0; i < allValues.Count; i++)
        {
            if (idColIndex < allValues[i].Count && allValues[i][idColIndex]?.ToString() == id)
            {
                var sheetMetadata = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var sheet = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
                return (i + 2, sheet?.Properties.SheetId); // +2 for 1-based index and skipped header
            }
        }

        return (-1, null);
    }

    private void SetPropertyValue<T>(T item, PropertyInfo prop, string? value) where T : class, IEntityWithId, new()
    {
        if (string.IsNullOrEmpty(value)) return;

        try
        {
            // Handles List<string> specifically
            if (prop.PropertyType == typeof(List<string>))
            {
                var stringList = value.Split([listDelimiter], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                prop.SetValue(item, stringList);
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    prop.SetValue(item, converter.ConvertFromString(value));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not set property '{prop.Name}' with value '{value}'. Error: {ex.Message}");
        }
    }

    private static object FormatPropertyValue(object? value)
    {
        if (value is List<string> list)
        {
            return string.Join(listDelimiter, list);
        }

        return value?.ToString() ?? string.Empty;
    }

    private static string GetColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    #endregion
}

