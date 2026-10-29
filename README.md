# PetSpa Management System

A pet care spa service booking and management system built with ASP.NET Core Web API and SQLite.

## Features

- Customer portal for managing pets and booking services
- Dynamic deposit calculation with weekday and weekend pricing strategies
- Real-time status synchronization between customer and staff portals
- Staff dashboard for appointments, status tracking, and checkout billing
- Service ticket and invoice export using Bridge Pattern (Online HTML & Offline POS formats)

## Tech Stack

- Backend: .NET 9.0 Web API
- Database: SQLite (Microsoft.Data.Sqlite)
- Architecture Patterns: Singleton (DatabaseManager), Strategy (PricingStrategy), Bridge (ExportBridge)
- Frontend: HTML5, CSS3, JavaScript (Fetch API, Bootstrap 5)

## Getting Started

### Prerequisites

- .NET 9.0 SDK

### Run the Application

```bash
dotnet restore
dotnet run
```

Access the application in your browser:
- Customer / Staff Login: http://localhost:5000/login.html
