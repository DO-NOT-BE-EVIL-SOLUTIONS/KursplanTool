# Kursplan - Access viewer

This small WinForms app opens a Microsoft Access database (`.accdb` or `.mdb`), displays the first user table in a `DataGridView`, lets the user edit rows, and saves changes back to the Access file.

## Architecture Overview

The main idea of this C# tool is to provide a user-friendly interface for a Microsoft Access file. The application is designed with a specific architecture in mind:

- **Model-View-Service:** The application follows a service-oriented architecture.
- **Data Model:** The expected layout of the Access database, including all required tables, is defined in the `Kursplan/Data` directory.
- **Database Service:** A dedicated `DatabaseService` in `Kursplan/Services` handles all interactions with the Access file using OLE DB. This service is responsible for connecting, validating the schema, and performing data operations.
- **UI:** The user interface, a WinForms application, is kept separate from the data logic and interacts exclusively through the `DatabaseService`.

## How it works

- Download the newest release from the repository.
- Move the EXE next to an Access file (or click Browse to select a file).
- The app auto-selects the first non-system table and displays it for editing.
- Click `Save` to persist changes back to the Access file.

## Notes & prerequisites

- Running on Windows only (uses `System.Data.OleDb`).
- The Access Database Engine (ACE) provider may be required for `.accdb` files. If not present, the app falls back to the older Jet provider for `.mdb` files.
- Test with a copy of your Access file first to avoid accidental data loss.

## Manual to create EXE manually

Build & produce a self-contained EXE

From the `Kursplan` project folder run (example for 64-bit Windows):

```powershell
# produce a self-contained single-file EXE for x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o ./publish
```

Copy the produced EXE from `Kursplan\publish` to the shared drive next to the Access file. The app will try to auto-open the first `.accdb`/`.mdb` in the same folder.
