using Scalar.AspNetCore;
using TonyBlogUI.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

// Register GoogleSheetsService as a singleton
builder.Services.AddSingleton<GoogleSheetsService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var credentials = configuration["GoogleSheets:Credentials"] 
        ?? throw new InvalidOperationException("GoogleSheets:Credentials is not configured. Add it to User Secrets or appsettings.json");
    var spreadsheetId = configuration["GoogleSheets:SpreadsheetId"] 
        ?? throw new InvalidOperationException("GoogleSheets:SpreadsheetId is not configured. Add it to User Secrets or appsettings.json");
        
    return new GoogleSheetsService(credentials, spreadsheetId);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
