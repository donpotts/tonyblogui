using System.ComponentModel;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using TonyBlogUI.Shared;

namespace TonyBlogUI.Server.Services;

/// <summary>
/// Service for performing CRUD operations on Google Sheets.
/// Supports generic entity types that implement IEntityWithId.
/// </summary>
public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;
    
    private static readonly string[] Scopes = [SheetsService.Scope.Spreadsheets];
    private const string ListDelimiter = ", ";
    private const string ApplicationName = "TonyBlogUI";

    /// <summary>
    /// Maps Google Sheet column headers to C# property names.
    /// Add mappings here when sheet headers don't match property names exactly.
    /// </summary>
    private static readonly Dictionary<string, string> HeaderToPropertyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Cluster Name", "ClusterName" },
        { "Primary Keyword", "PrimaryKeyword" }
    };

    public GoogleSheetsService(string credentials, string spreadsheetId)
    {
        _spreadsheetId = spreadsheetId;
        _sheetsService = CreateSheetsService(credentials);
    }

    #region CRUD Operations

    /// <summary>
    /// Retrieves all entities from the specified sheet.
    /// </summary>
    public async Task<List<T>> GetAllAsync<T>(string sheetName = "Sheet1") 
        where T : class, IEntityWithId, new()
    {
        var (headerMap, rows) = await GetSheetDataAsync(sheetName);
        if (rows == null || rows.Count == 0) 
            return [];

        var properties = typeof(T).GetProperties().ToDictionary(p => p.Name);
        var items = new List<T>();

        foreach (var row in rows)
        {
            var item = MapRowToEntity<T>(row, headerMap, properties);
            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Adds a new entity to the specified sheet.
    /// </summary>
    public async Task<T> AddAsync<T>(T entity, string sheetName = "Sheet1") 
        where T : class, IEntityWithId, new()
    {
        entity.Id = Guid.NewGuid().ToString();

        var (headerMap, _) = await GetSheetDataAsync(sheetName);
        var rowValues = MapEntityToRow(entity, headerMap);

        var valueRange = new ValueRange { Values = [rowValues] };
        var range = $"{EscapeSheetName(sheetName)}!A1";
        
        var request = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();

        return entity;
    }

    /// <summary>
    /// Updates an existing entity in the specified sheet.
    /// </summary>
    public async Task<bool> UpdateAsync<T>(T entity, string sheetName = "Sheet1")
        where T : class, IEntityWithId, new()
    {
        var (rowIndex, _) = await FindRowByIdAsync(entity.Id, sheetName);
        if (rowIndex == -1) 
            return false;

        var (headerMap, _) = await GetSheetDataAsync(sheetName);
        var rowValues = MapEntityToRow(entity, headerMap);

        var range = $"{EscapeSheetName(sheetName)}!A{rowIndex}:{GetColumnLetter(headerMap.Count)}{rowIndex}";
        var valueRange = new ValueRange { Values = [rowValues] };
        
        var request = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();

        return true;
    }

    /// <summary>
    /// Deletes an entity from the specified sheet by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(string id, string sheetName = "Sheet1")
    {
        var (rowIndex, sheetId) = await FindRowByIdAsync(id, sheetName);
        if (rowIndex == -1 || sheetId == null) 
            return false;

        var deleteRequest = new Request
        {
            DeleteDimension = new DeleteDimensionRequest
            {
                Range = new DimensionRange
                {
                    SheetId = sheetId,
                    Dimension = "ROWS",
                    StartIndex = rowIndex - 1,
                    EndIndex = rowIndex
                }
            }
        };

        var batchRequest = new BatchUpdateSpreadsheetRequest { Requests = [deleteRequest] };
        await _sheetsService.Spreadsheets.BatchUpdate(batchRequest, _spreadsheetId).ExecuteAsync();
        
        return true;
    }

    #endregion

    #region Private Helper Methods

    private static SheetsService CreateSheetsService(string credentials)
    {
        GoogleCredential credential = credentials.TrimStart().StartsWith('{')
            ? GoogleCredential.FromJson(credentials).CreateScoped(Scopes)
            : GoogleCredential.FromFile(credentials).CreateScoped(Scopes);

        return new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    private async Task<(Dictionary<string, int> HeaderMap, IList<IList<object>>? Rows)> GetSheetDataAsync(string sheetName)
    {
        var range = $"{EscapeSheetName(sheetName)}!A1:Z";
        var response = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
        var values = response.Values;

        if (values == null || values.Count == 0)
            return (new Dictionary<string, int>(), null);

        var headerRow = values[0];
        var headerMap = headerRow
            .Select((header, index) => (Header: header?.ToString() ?? "", Index: index))
            .Where(h => !string.IsNullOrEmpty(h.Header))
            .ToDictionary(
                h => MapHeaderToProperty(h.Header),
                h => h.Index);

        var dataRows = values.Skip(1).ToList();
        return (headerMap, dataRows);
    }

    private async Task<(int RowIndex, int? SheetId)> FindRowByIdAsync(string id, string sheetName)
    {
        var (headerMap, rows) = await GetSheetDataAsync(sheetName);
        
        if (!headerMap.TryGetValue("Id", out var idColumnIndex) || rows == null)
            return (-1, null);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (idColumnIndex < row.Count && row[idColumnIndex]?.ToString() == id)
            {
                var sheetMetadata = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var sheet = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
                
                // +2 accounts for 1-based indexing and header row
                return (i + 2, sheet?.Properties.SheetId);
            }
        }

        return (-1, null);
    }

    private static T MapRowToEntity<T>(IList<object> row, Dictionary<string, int> headerMap, Dictionary<string, PropertyInfo> properties)
        where T : class, IEntityWithId, new()
    {
        var entity = new T();

        foreach (var (propertyName, columnIndex) in headerMap)
        {
            if (!properties.TryGetValue(propertyName, out var property))
                continue;
            
            if (columnIndex >= row.Count)
                continue;

            var cellValue = row[columnIndex]?.ToString();
            SetPropertyValue(entity, property, cellValue);
        }

        return entity;
    }

    private static List<object> MapEntityToRow<T>(T entity, Dictionary<string, int> headerMap)
        where T : class, IEntityWithId
    {
        var row = new object[headerMap.Count];
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            if (headerMap.TryGetValue(property.Name, out var columnIndex))
            {
                row[columnIndex] = FormatPropertyValue(property.GetValue(entity));
            }
        }

        return [.. row];
    }

    private static void SetPropertyValue<T>(T entity, PropertyInfo property, string? value)
        where T : class, IEntityWithId
    {
        if (string.IsNullOrEmpty(value))
            return;

        try
        {
            object? convertedValue = property.PropertyType switch
            {
                var t when t == typeof(List<string>) => ParseStringList(value),
                var t when t == typeof(bool) => ParseBoolean(value),
                _ => ConvertValue(property.PropertyType, value)
            };

            if (convertedValue != null)
                property.SetValue(entity, convertedValue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set property '{property.Name}' with value '{value}': {ex.Message}");
        }
    }

    private static object FormatPropertyValue(object? value) => value switch
    {
        List<string> list => string.Join(ListDelimiter, list),
        bool boolValue => boolValue ? "Yes" : "No",
        null => string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static List<string> ParseStringList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

    private static bool ParseBoolean(string value) =>
        value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("True", StringComparison.OrdinalIgnoreCase);

    private static object? ConvertValue(Type targetType, string value)
    {
        var converter = TypeDescriptor.GetConverter(targetType);
        return converter.CanConvertFrom(typeof(string)) 
            ? converter.ConvertFromString(value) 
            : null;
    }

    private static string MapHeaderToProperty(string header) =>
        HeaderToPropertyMap.TryGetValue(header, out var propertyName) 
            ? propertyName 
            : header;

    private static string EscapeSheetName(string sheetName) => $"'{sheetName}'";

    private static string GetColumnLetter(int columnNumber)
    {
        var result = "";
        while (columnNumber > 0)
        {
            var remainder = (columnNumber - 1) % 26;
            result = (char)('A' + remainder) + result;
            columnNumber = (columnNumber - 1) / 26;
        }
        return result;
    }

    #endregion
}
