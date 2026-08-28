# Axon POS Enterprise System 🚀

An Enterprise Desktop Point of Sale (POS) & ERP System built with **.NET 10.0 WPF**, **CommunityToolkit MVVM**, and Entity Framework Core with **Dual Engine Architecture (SQL Server & Local SQLite Failover)**.

---

## 🌟 Key Features

- 🛒 **POS Sales Terminal**: Barcode scanner integration, fast cart management, subtotal/tax calculation, and percentage/fixed currency discounts (`ج.م`).
- 🖨️ **Thermal Receipt Printing**: Live Windows 11 print preview & thermal receipt printer integration.
- 📦 **Inventory & Stock Control**: Stock tracking, restocks, stock deductions, customer returns, low stock warnings, barcode generation, and unit of measure management.
- 📊 **Analytics & Reports**: Real-time DB aggregations, date range filtering, Excel CSV export (`UTF-8 BOM`), and PDF report generation.
- 🛡️ **Enterprise Security & Roles**: BCrypt salted password hashing, account lockout policy, and Role-Based Access Control (System Admin, Cashier, Inventory Manager).
- 🗄️ **Dual Database Engine**: Seamless SQL Server connectivity with automated local SQLite (`AxonPOS.db`) failover for 100% data persistence.
- 🪟 **Desktop Ergonomics**: Custom frameless dark theme, top titlebar controls (Window Drag, Minimize, Maximize/Restore, Close), and full Arabic RTL layout (`FlowDirection="RightToLeft"`).

---

## 🛠️ Technology Stack

- **Framework**: .NET 9 WPF
- **Language**: C# 13
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, UI)
- **MVVM Pattern**: CommunityToolkit.Mvvm 8.4
- **Database Engine**: Entity Framework Core 10 (SQL Server & SQLite)
- **Security**: BCrypt.Net-Next

---

## 🚀 Quick Start

1. Clone repository:
   ```bash
   git clone https://github.com/F3raon/Axon-Pos.git
   ```
2. Open solution `AxonPOS/Axon.slnx` in Visual Studio 2022+ or VS Code.
3. Build and Run:
   ```bash
   dotnet run --project AxonPOS/Axon.UI/Axon.UI.csproj
   ```
4. Default Admin Credentials:
   - **User ID**: `admin`
   - **Password**: `admin123`
