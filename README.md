# 📝 Tony Blog UI

A modern Blazor WebAssembly application for managing keyword clusters, powered by Google Sheets as a backend database with JWT authentication.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat-square&logo=blazor)
![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite)
![Google Sheets](https://img.shields.io/badge/Backend-Google%20Sheets-34A853?style=flat-square&logo=googlesheets)

## ✨ Features

- 🔐 **JWT Authentication** - Secure login, registration, and password management
- 📊 **Google Sheets Integration** - Use Google Sheets as your database with full CRUD operations
- 🗄️ **SQLite Database** - Local user authentication and identity storage
- 👥 **User Management** - Admin users can create, edit, and delete user accounts
- 📥 **Import/Export** - Import and export data via CSV and Excel files
- 🎨 **Modern UI** - Clean, responsive design with smooth animations
- ⚡ **Blazor WebAssembly** - Fast, client-side rendering with C#
- 🔒 **Secure Configuration** - Credentials stored safely in User Secrets
- 📱 **Responsive Design** - Works seamlessly on desktop and mobile
- 👤 **User Roles** - Admin and User roles with role-based authorization
- 🚦 **Rate Limiting** - Built-in API rate limiting with multiple policies
- 📄 **OpenAPI Documentation** - Interactive API docs with Scalar UI

## 🏗️ Architecture

```
TonyBlogUI/
├── TonyBlogUI.Client/       # Blazor WebAssembly frontend
│   ├── Layout/              # Main layout and navigation
│   ├── Pages/               # Razor pages (Home, Blogs, Users, Login, Register, etc.)
│   ├── Services/            # Auth services and state management
│   └── wwwroot/             # Static assets and CSS
├── TonyBlogUI.Server/       # ASP.NET Core API backend
│   ├── Controllers/         # API endpoints (Auth, Blogs, Users)
│   ├── Data/                # Entity Framework DbContext and Identity
│   └── Services/            # Google Sheets service
└── TonyBlogUI.Shared/       # Shared models, DTOs, and interfaces
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Google Cloud Project](https://console.cloud.google.com/) with Sheets API enabled
- Service Account credentials (JSON key file)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/donpotts/tonyblogui.git
   cd tonyblogui
   ```

2. **Configure User Secrets**
   ```bash
   cd TonyBlogUI.Server
   dotnet user-secrets init
   dotnet user-secrets set "GoogleSheets:Credentials" "<your-json-credentials>"
   dotnet user-secrets set "GoogleSheets:SpreadsheetId" "<your-spreadsheet-id>"
   dotnet user-secrets set "Jwt:Key" "<your-secret-key-at-least-32-chars>"
   ```

3. **Run the application**
   ```bash
   dotnet run --project TonyBlogUI.Server
   ```

4. **Open in browser**
   ```
   https://localhost:5001
   ```

## 🔐 Authentication

### Default Users (Development)

The application seeds two default users for testing:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@example.com | testUser123! |
| User | user@example.com | testUser123! |

### JWT Token
- Tokens are stored in localStorage
- Token expiration: 30 days
- Tokens include user roles for authorization

## 👥 User Management

Admin users have access to manage user accounts.

### Features

- **View Users**: Admin can view all users and their roles.
- **Edit Users**: Admin can edit user details and roles.
- **Delete Users**: Admin can delete user accounts.
- **Password Reset**: Admin can reset passwords for users.

## 🗄️ SQLite Database

The application uses SQLite for storing user authentication data via ASP.NET Core Identity.

### Database Location

The database file `TonyBlogUI.db` is created in the server project directory.

### Database Schema

The SQLite database stores:
- **Users** - User accounts with hashed passwords
- **Roles** - User roles (Admin, User)
- **User Roles** - User-to-role mappings
- **User Claims** - Additional user claims (first name, last name)

### Entity Framework Core

The application uses Entity Framework Core with SQLite provider:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Database Initialization

In development mode, the database is automatically recreated on each startup to apply seed data:

```csharp
if (app.Environment.IsDevelopment())
{
    dbContext.Database.EnsureDeleted();
    dbContext.Database.EnsureCreated();
}
```

> **Note:** In production, use Entity Framework migrations instead of `EnsureDeleted()`/`EnsureCreated()`.

### Connection String

Configure the database location in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TonyBlogUI.db"
  }
}
```

## 📋 Google Sheet Setup

Create a Google Sheet with the following columns in a sheet named `Pillar #1`:

| Column | Description |
|--------|-------------|
| Cluster Name | Name of the keyword cluster |
| Intent | Search intent (Informational, Transactional, etc.) |
| Keywords | Comma-separated list of keywords |
| Primary Keyword | Main target keyword |
| Completed | Status (Yes/No) |
| Url | Published blog URL |
| Id | Auto-generated GUID |

> **Note:** Share your Google Sheet with the service account email address.

## 📥 Import/Export

### Supported Formats
- **CSV** - Comma-separated values
- **Excel** - .xlsx files (using ClosedXML)

### Export Columns
Exports include all fields with Id as the first column:
1. Id
2. ClusterName
3. Intent
4. Keywords (semicolon-separated)
5. PrimaryKeyword
6. Completed
7. Url

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TonyBlogUI.db"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "TonyBlogUI",
    "Audience": "TonyBlogUI",
    "ExpireDays": "30"
  },
  "GoogleSheets": {
    "Credentials": "",
    "SpreadsheetId": ""
  }
}
```

### Development (User Secrets)

```json
{
  "GoogleSheets": {
    "Credentials": "{...service account JSON...}",
    "SpreadsheetId": "your-spreadsheet-id"
  },
  "Jwt": {
    "Key": "your-super-secret-key-for-jwt-tokens"
  }
}
```

### Production (Environment Variables)

```bash
GoogleSheets__Credentials="{...}"
GoogleSheets__SpreadsheetId="your-spreadsheet-id"
Jwt__Key="your-production-secret-key"
```

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| **Blazor WebAssembly** | Frontend framework |
| **ASP.NET Core** | Backend API |
| **ASP.NET Core Identity** | Authentication & user management |
| **JWT Bearer** | Token-based authentication |
| **Entity Framework Core** | ORM for SQLite database |
| **SQLite** | User database storage |
| **Google Sheets API** | Blog data storage |
| **CsvHelper** | CSV import/export |
| **ClosedXML** | Excel import/export |
| **Bootstrap 5** | UI components |
| **Bootstrap Icons** | Icon library |
| **Blazored.LocalStorage** | Client-side storage |

## 📡 API Endpoints

### Authentication

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/auth/register` | Register new user | No |
| `POST` | `/api/auth/login` | Login and get JWT token | No |
| `POST` | `/api/auth/change-password` | Change password | Yes |
| `GET` | `/api/auth/me` | Get current user info | Yes |

### Users (Admin Only)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/users` | Get all users | Yes (Admin) |
| `GET` | `/api/users/{id}` | Get user by ID | Yes (Admin) |
| `GET` | `/api/users/roles` | Get available roles | Yes (Admin) |
| `POST` | `/api/users` | Create new user | Yes (Admin) |
| `PUT` | `/api/users/{id}` | Update user | Yes (Admin) |
| `DELETE` | `/api/users/{id}` | Delete user | Yes (Admin) |

### Blogs

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/blogs` | Get all keyword clusters | Yes |
| `GET` | `/api/blogs/{id}` | Get cluster by ID | Yes |
| `POST` | `/api/blogs` | Create new cluster | Yes |
| `PUT` | `/api/blogs/{id}` | Update cluster | Yes |
| `DELETE` | `/api/blogs/{id}` | Delete cluster | Yes |

## 📖 API Documentation

Scalar API documentation is available in development mode:
- **Scalar UI**: `https://localhost:5001/scalar/v1`
- **OpenAPI Spec**: `https://localhost:5001/openapi/v1.json`

The API documentation includes:
- JWT Bearer authentication support
- Interactive "Try it out" functionality
- All endpoint schemas and examples

## 🚦 Rate Limiting

The API includes built-in rate limiting to protect against abuse. Four rate limiting policies are available:

| Policy | Description | Limits |
|--------|-------------|--------|
| **Fixed** | Fixed window rate limiter | 100 requests per minute |
| **Sliding** | Sliding window rate limiter | 4 requests per 10 seconds |
| **Token** | Token bucket rate limiter | 4 tokens, replenishes 4 tokens every 10 seconds |
| **Concurrency** | Concurrent request limiter | 10 concurrent requests |

### Applying Rate Limits

Use the `[EnableRateLimiting]` attribute on controllers or actions:

```csharp
[EnableRateLimiting("Fixed")]
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    // ...
}
```

### Rate Limit Response

When rate limited, the API returns:
- **Status Code**: `429 Too Many Requests`
- **Header**: `Retry-After` with seconds to wait
- **Body**: User-friendly message with retry information

## 🎯 Customization

### Theming

Edit CSS variables in `TonyBlogUI.Client/wwwroot/css/app.css`:

```css
:root {
    --primary: #6366f1;
    --primary-hover: #4f46e5;
    --success: #10b981;
    --danger: #ef4444;
    /* ... more variables */
}
```

### Adding New Sheet Mappings

Update `HeaderToPropertyMap` in `GoogleSheetsService.cs`:

```csharp
private static readonly Dictionary<string, string> HeaderToPropertyMap = new()
{
    { "Your Column Name", "PropertyName" }
};
```

## 🔒 Security Notes

- Change the default JWT key in production
- Use HTTPS in production
- Consider using Azure Key Vault or similar for secrets management
- The SQLite database stores user credentials (hashed passwords)
- In development, the database is recreated on each startup to apply seed data
- Rate limiting helps protect against brute force and DDoS attacks

---

## 📬 Contact

Don Potts - Don.Potts@DonPotts.com

---

<p align="center">
  Made with ❤️ using Blazor and .NET
</p>
