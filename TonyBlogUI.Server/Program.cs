using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TonyBlogUI.Server.Data;
using TonyBlogUI.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure OpenAPI with JWT Bearer authentication
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Tony Blog UI API";
        document.Info.Version = "v1";
        document.Info.Description = "API for managing keyword clusters with JWT authentication. Use POST /api/auth/login to get a token.";

        document.Components ??= new OpenApiComponents();
        document.AddComponent(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Description = "Access token from login endpoint",
            }
        );

        document.Security ??= [];
        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document), [] },
            }
        );

        return Task.CompletedTask;
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Window Rate Limiter
    options.AddFixedWindowLimiter("Fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 0;
        opt.AutoReplenishment = true;
    });

    // Sliding Window Rate Limiter
    options.AddSlidingWindowLimiter("Sliding", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 4;
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.SegmentsPerWindow = 2;

    });

    // Token Bucket Rate Limiter
    options.AddTokenBucketLimiter("Token", opt =>
    {
        opt.TokenLimit = 4;
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.TokensPerPeriod = 4;
        opt.AutoReplenishment = true;

    });

    //Concurrency Limiter
    options.AddConcurrencyLimiter("Concurrency", opt =>
    {
        opt.PermitLimit = 10;
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;

    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int) retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

            await context.HttpContext.Response.WriteAsync(
                $"Too many requests. Please try again after {retryAfter.TotalMinutes} minute(s). " +
                $"Read more about our rate limits at https://www.radendpoint.com/faq/.", cancellationToken: token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later. " +
                "Read more about our rate limits at https://www.radendpoint.com/faq/.", cancellationToken: token);
        }
    };
});

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=TonyBlogUI.db"));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TonyBlogUI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TonyBlogUI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

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

// Ensure database is created with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Delete and recreate database to ensure seed data is applied
    // Remove these lines in production and use migrations instead
    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Tony Blog UI API")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
               .WithPreferredScheme("Bearer")
               .WithHttpBearerAuthentication(bearer =>
               {
                   bearer.Token = "";
               });
    });
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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
