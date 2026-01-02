# Gemini Project Overview: KursplanTool

This document provides essential information for an AI assistant to understand and work with the `KursplanTool` project.

## Project Overview

`KursplanTool` is a .NET Windows Forms desktop application designed to interact with a Microsoft Access database (`.accdb`). Its primary purpose is to provide a modern user experience for planning courses (`Kursplan` is German for "course plan"). The application accesses an Access database file, view its tables, and save changes back to the file.

## Technology Stack

* **Language:** C#
* **Framework:** .NET 10.0 (SDK version `10.0.101` as per `global.json`)
* **UI:** Windows Forms (`UseWindowsForms`)
* **Testing:** xUnit
* **Database Access:** `System.Data.OleDb` for connecting to Microsoft Access databases.

## Project Structure

* `KursplanNT.sln`: The main solution file for Visual Studio.
* `Kursplan/`: The main Windows Forms application project.
  * `Kursplan.csproj`: The project file, defining dependencies and settings.
  * `Program.cs`: The application's main entry point.
  * `Kursplaner.cs`: The main form/window of the application.
  * `Services/DatabaseService.cs`: A dedicated class for handling all database connections, schema validation, and data manipulation (read/write).
  * `Data/RequiredTables.cs`: Likely defines the expected table schema for the database.
* `Kursplan.Tests/`: The test project.
  * `Kursplan.Tests.csproj`: The xUnit test project file.
  * `resources/Kursprogramm_V1.accdb`: A sample Access database used for testing.
* `.github/workflows/`: Contains CI/CD automation scripts.
  * `CI.yml`: Defines the continuous integration workflow for building and testing the project on Windows. It includes a crucial step to install the Access Database Engine.

## Build & Test

The build and test processes are defined in `.github/workflows/CI.yml` and can be executed locally using the .NET CLI.

* **Restore Dependencies:**

```shell
dotnet restore
```

* **Build the Project:**

```shell
dotnet build -c Release
```

* **Run Tests:**

```shell
dotnet test -c Release --no-build
```

**Note:** The application and tests require the **Microsoft Access Database Engine (x64)** to be installed to connect to `.accdb` files.

## Database

* **Type:** Microsoft Access (`.accdb`)
* **Connection:** The `DatabaseService` connects using an `OleDbConnection`. It dynamically searches for the correct OLE DB provider (`Microsoft.ACE.OLEDB.12.0` or `Microsoft.Jet.OLEDB.4.0`).
* **Schema:** The application validates the database schema against a list of required tables upon connection.
* **Data Handling:** Data is loaded into `DataTable` objects for manipulation within the application. Changes are saved back to the database using an `OleDbDataAdapter`.
