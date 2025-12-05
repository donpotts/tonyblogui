# 📝 Tony Blog UI

A modern Blazor WebAssembly application for managing keyword clusters, powered by Google Sheets as a backend database.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat-square&logo=blazor)
![Google Sheets](https://img.shields.io/badge/Backend-Google%20Sheets-34A853?style=flat-square&logo=googlesheets)

## ✨ Features

- 📊 **Google Sheets Integration** - Use Google Sheets as your database with full CRUD operations
- 🎨 **Modern UI** - Clean, responsive design with smooth animations
- ⚡ **Blazor WebAssembly** - Fast, client-side rendering with C#
- 🔒 **Secure Configuration** - Credentials stored safely in User Secrets
- 📱 **Responsive Design** - Works seamlessly on desktop and mobile

## 🏗️ Architecture

```
TonyBlogUI/
├── TonyBlogUI.Client/       # Blazor WebAssembly frontend
│   ├── Layout/              # Main layout and navigation
│   ├── Pages/               # Razor pages (Home, Blogs)
│   └── wwwroot/             # Static assets and CSS
├── TonyBlogUI.Server/       # ASP.NET Core API backend
│   ├── Controllers/         # API endpoints
│   └── Services/            # Google Sheets service
└── TonyBlogUI.Shared/       # Shared models and interfaces
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
   ```

3. **Run the application**
   ```bash
   dotnet run --project TonyBlogUI.Server
   ```

4. **Open in browser**
   ```
   https://localhost:5001
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

## 🔧 Configuration

### Development (User Secrets)

```json
{
  "GoogleSheets": {
    "Credentials": "{...service account JSON...}",
    "SpreadsheetId": "your-spreadsheet-id"
  }
}
```

### Production (Environment Variables)

```bash
GoogleSheets__Credentials="{...}"
GoogleSheets__SpreadsheetId="your-spreadsheet-id"
```

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| **Blazor WebAssembly** | Frontend framework |
| **ASP.NET Core** | Backend API |
| **Google Sheets API** | Database storage |
| **Bootstrap 5** | UI components |
| **Bootstrap Icons** | Icon library |

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/blogs` | Get all keyword clusters |
| `GET` | `/api/blogs/{id}` | Get cluster by ID |
| `POST` | `/api/blogs` | Create new cluster |
| `PUT` | `/api/blogs/{id}` | Update cluster |
| `DELETE` | `/api/blogs/{id}` | Delete cluster |

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

---

<p align="center">
  Made with ❤️ using Blazor and .NET
</p>
