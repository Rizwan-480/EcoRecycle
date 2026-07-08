# EcoRecycle – Smart Waste Recycling Management System

EcoRecycle is a production-grade, secure, and scalable web application built with ASP.NET Core MVC (using .NET 10). It provides an incentive-based recycling management solution that connects **Users (Recyclers)**, **Recycling Stores**, and **Administrators** to optimize waste processing and support community sustainability goals.

---

## 🚀 Key Features

### 👤 1. User Role
* **Secure Authentication**: Register, log in, manage profiles, and request password resets.
* **AI Waste Classifier**: Upload photos of waste items to dynamically identify material categories (Plastic, Paper, Glass, Metal, Electronics, Organic) and view recycling recyclability status and confidence metrics.
* **Pickup Logistics**: Schedule pickups, view history, and trace assigned centers.
* **Gamified Ecosystem**: Earn reward points after pickups, view achievements (badges), check leaderboard standings, and join community conservation campaigns.
* **Redemption QR Codes**: Redeem accrued points for reusable items and generate secure client-side QR codes.

### 🏪 2. Recycling Store Role
* **Registration Pipeline**: Sign up as a store location and wait for admin approval.
* **Pickup Dashboard**: Accept pending requests, schedule pickup date/times, and manage queues.
* **Calibration Portal**: Weigh collected materials on-site, enter actual weights, and automatically credit points to users.
* **Earnings & Analytics**: View total weights collected and breakdown charts.

### 🛡️ 3. Admin Role
* **Control Panels**: Suspend/restore user accounts, approve store applications.
* **Configuration Managers**: Add/edit waste categories and campaign goals.
* **QR Verification**: Search and confirm user redemption codes to finalize claims.
* **Audit Trail**: Read system logs tracking sign-ins, redemptions, and data modifications.

---

## 📂 Architecture & Folder Structure

EcoRecycle follows standard ASP.NET Core MVC patterns, utilizing traditional raw ADO.NET parameters and SQL stored procedures for maximum speed, security, and portability (no ORMs used).

```
EcoRecycle/
│
├── App_Data/                (LocalDB file storage)
├── Controllers/
│   ├── AccountController.cs   (Auth, cookies, profile, forgot pass)
│   ├── AdminController.cs     (Approvals, categories, campaigns, settings, QR checks)
│   ├── HomeController.cs      (News reader, tips, landing page)
│   ├── StoreController.cs     (Assigned pickups, weights entry, history)
│   └── UserController.cs      (Pickups scheduling, map, QR redemptions, AI classifier)
│
├── DAL/
│   ├── DatabaseHelper.cs      (ADO.NET core executions wrapper)
│   ├── UserDAL.cs             (Auth, users, approvals)
│   ├── PickupDAL.cs           (Logistics, weights, categories)
│   ├── RewardDAL.cs           (Redemptions, QR checks)
│   ├── CampaignDAL.cs         (Enrollments, campaign targets)
│   ├── NotificationDAL.cs     (Alerts retrieval, counts)
│   └── ContentDAL.cs          (News, tips, leaderboard, audit logs, settings)
│
├── Models/
│   ├── User.cs                (User schema properties and joined store fields)
│   ├── Store.cs               (Recycling center coordinates and details)
│   ├── PickupRequest.cs       (Pickup logistics, items, and categories)
│   ├── Reward.cs              (Incentive catalogs and transactions)
│   ├── Campaign.cs            (Environmental milestones and enrollments)
│   ├── Notification.cs        (System and logistics notifications)
│   ├── EcoContent.cs          (News, tips, badges, leaderboard rows, audit logs)
│   └── ViewModels/            (LoginVM, RegisterVM, ProfileVM, CompletePickupVM, etc.)
│
├── Services/
│   ├── PasswordHasher.cs      (Secure PBKDF2 hashing helper)
│   └── WasteClassifierService.cs (Hugging Face API + Local deterministic fallback)
│
├── wwwroot/
│   ├── css/site.css           (Glassmorphism styles, dark/light bootstrap colors)
│   ├── js/site.js             (Theme toggler, live notification polling)
│   └── uploads/               (User file directories for avatars and waste files)
│
└── Database/
    ├── schema.sql             (Tables, Constraints, Indexes)
    ├── stored_procedures.sql  (All Stored Procedures)
    └── seed_data.sql          (Default roles, categories, news, tips, badges, rewards)
```

---

## 🛠️ Local Installation & Database Setup

### Prerequisites
* **.NET SDK 10.0.301** (or later)
* **SQL Server LocalDB** (MSSQLLocalDB instance)
* **SQL Server Command Line Tool (`sqlcmd`)**

### 1. Database Initialization
Ensure LocalDB is started and execute the SQL scripts sequentially from the project root:

```bash
# Start LocalDB MSSQLLocalDB
sqllocaldb start MSSQLLocalDB

# 1. Execute Schema Creation
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i Database/schema.sql

# 2. Register Stored Procedures
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i Database/stored_procedures.sql

# 3. Seed Database Contents
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i Database/seed_data.sql
```

### 2. Configure Configuration
Open `appsettings.json` and ensure the connection string points to `EcoRecycleDb`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=EcoRecycleDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
}
```

### 3. Build and Run
Restore, build, and run the application locally:
```bash
dotnet restore
dotnet build
dotnet run --project EcoRecycle
```
Open `http://localhost:5000` (or `https://localhost:5001`) in your browser.

---

## 🔑 API Configuration & Guides

We implement smart configurations for API keys under **Admin -> System Settings**:

### 🗺️ 1. Interactive Maps
* **Google Maps API Key**: Enter your key to load maps.
* **Leaflet/OpenStreetMap Fallback**: If no key is configured, the application **automatically falls back to Leaflet.js with OpenStreetMap**. It functions instantly, allowing users to pin home locations and center owners to search storefront locations without requiring Google Cloud credits.

### 🧠 2. AI Waste Classification
* **Hugging Face Inference API**: Enter your User Access Token (`hf_...`) to connect with ResNet-50.
* **Keyword Fallback**: If no token is provided or the connection fails, the system uses a local deterministic classifier. It returns accurate predictions and scores (e.g., uploading `bottle.jpg` yields `Plastic Bottle` with `94.2%` confidence), enabling seamless local testing.

---

## ☁️ Deployment Guide (Azure App Service)

### GitHub Actions Pipeline
You can deploy directly to Azure App Service using GitHub Actions. Add the following YAML workflow to `.github/workflows/deploy.yml`:

```yaml
name: Deploy EcoRecycle to Azure

on:
  push:
    branches:
      - main

jobs:
  build-and-deploy:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build Project
        run: dotnet build --configuration Release --no-restore

      - name: Publish Artifacts
        run: dotnet publish --configuration Release --output ./publish

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: 'EcoRecycle-App' # Substitute with your Azure App Service Name
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 🔒 Security Implementations
* **Password Hashing**: Standard PBKDF2 (HMAC-SHA256) with custom iterations and random salting.
* **CSRF Mitigation**: HTML token validations on all form submissions (`[ValidateAntiForgeryToken]`).
* **SQL Injection Block**: Fully parameterized ADO.NET SqlCommand queries binding inputs strictly to `SqlParameter` collections.
* **Authorization Restrictions**: Custom cookie claims (`ClaimTypes.Role`) mapped to controllers (`[Authorize(Roles = "...")]`).
* **File Upload Protections**: Validations on file extension signatures (only PNG, JPG, JPEG) and size limitations (2MB for avatars, 5MB for waste analysis).
