using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register GoogleSheetsService as a singleton
builder.Services.AddSingleton<GenericGoogleSheetsService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var credentials = configuration["GoogleSheets:Credentials"] ?? Constant.SecretKey;
    var spreadsheetId = configuration["GoogleSheets:SpreadsheetId"] ?? Constant.SpreadsheetId;
    return new GenericGoogleSheetsService(credentials, spreadsheetId);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
