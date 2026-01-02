using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Kursplan.Services;
using Xunit;

namespace Kursplan.Tests;

public class UnitTest1
{
    private const string TestDbFileName = "Kursprogramm_V1.accdb";

    // Helper to get a fresh copy of the database for tests that modify data
    private string GetTempDbPath()
    {
        var originalPath = FindResourceFile("resources", TestDbFileName);
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{TestDbFileName}");
        File.Copy(originalPath, tempPath);
        return tempPath;
    }

    // --- 1. Connection Tests ---

    [Fact]
    public void Connect_Fails_With_Invalid_Path()
    {
        using var service = new DatabaseService();
        var (success, message) = service.Connect("C:\\Invalid\\Path\\To\\Nothing.accdb");
        
        Assert.False(success);
        Assert.Contains("Failed to open Access file", message);
    }

    [Fact]
    public void Connect_Fails_With_Non_Database_File()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using var service = new DatabaseService();
            var (success, message) = service.Connect(tempFile);

            Assert.False(success);
            Assert.Contains("Failed to open Access file", message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Connect_Succeeds_With_Valid_Database()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        var (success, message) = service.Connect(path);

        Assert.True(success, $"Connection failed: {message}");
        Assert.Empty(message);
    }

    // --- 2. Schema Validation Tests ---

    [Fact]
    public void ValidateSchema_Succeeds_For_Correct_Schema()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        service.Connect(path);

        var required = new List<string> { "LB_Stammdaten" };
        var (allExist, missing) = service.ValidateSchema(required);

        Assert.True(allExist);
        Assert.Empty(missing);
    }

    [Fact]
    public void ValidateSchema_Fails_For_Incomplete_Schema()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        service.Connect(path);

        var required = new List<string> { "LB_Stammdaten", "NonExistentTable_XYZ" };
        var (allExist, missing) = service.ValidateSchema(required);

        Assert.False(allExist);
        Assert.Contains("NonExistentTable_XYZ", missing);
    }

    [Fact]
    public void ValidateSchema_Throws_Exception_If_Not_Connected()
    {
        using var service = new DatabaseService();
        Assert.Throws<InvalidOperationException>(() => service.ValidateSchema(new List<string>()));
    }

    // --- 3. Data Operations Tests ---

    [Fact]
    public void GetTable_Returns_DataTable_For_Existing_Table()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        service.Connect(path);

        var dt = service.GetTable("LB_Stammdaten");
        
        Assert.NotNull(dt);
        Assert.True(dt.Columns.Count > 0);
    }

    [Fact]
    public void GetTable_Throws_Exception_For_Nonexistent_Table()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        service.Connect(path);

        Assert.ThrowsAny<Exception>(() => service.GetTable("ThisTableDoesNotExist"));
    }

    [Fact]
    public void GetTable_Throws_Exception_If_Not_Connected()
    {
        using var service = new DatabaseService();
        Assert.Throws<InvalidOperationException>(() => service.GetTable("LB_Stammdaten"));
    }

    [Fact]
    public void SaveChanges_Persists_Changes_To_Database()
    {
        var dbPath = GetTempDbPath();
        try
        {
            string newValue = $"TestUpdate_{Guid.NewGuid()}";
            
            // 1. Modify
            using (var service = new DatabaseService())
            {
                service.Connect(dbPath);
                var dt = service.GetTable("LB_Stammdaten");
                
                if (dt.Rows.Count > 0)
                {
                    dt.Rows[0][1] = newValue; // Modify second column
                    var (success, msg) = service.SaveChanges(dt);
                    Assert.True(success, $"Save failed: {msg}");
                }
            }

            // 2. Verify
            using (var service = new DatabaseService())
            {
                service.Connect(dbPath);
                var dt = service.GetTable("LB_Stammdaten");
                if (dt.Rows.Count > 0)
                {
                    var val = dt.Rows[0][1].ToString();
                    Assert.Equal(newValue, val);
                }
            }
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void SaveChanges_Fails_If_Data_Not_Loaded()
    {
        var path = FindResourceFile("resources", TestDbFileName);
        using var service = new DatabaseService();
        service.Connect(path);

        var (success, msg) = service.SaveChanges(new DataTable());
        Assert.False(success);
        Assert.Contains("No data has been loaded", msg);
    }

    [Fact]
    public void SaveChanges_Fails_Gracefully_If_Database_Is_Locked()
    {
        var dbPath = GetTempDbPath();
        FileStream? lockStream = null;
        try
        {
            // Lock the file to allow reading but prevent writing (FileShare.Read)
            lockStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            using var service = new DatabaseService();
            var (connected, _) = service.Connect(dbPath);

            if (connected)
            {
                // If connected (likely Read-Only), SaveChanges should fail
                var dt = service.GetTable("LB_Stammdaten");
                if (dt.Rows.Count > 0) dt.Rows[0][1] = "Locked";
                
                var (success, msg) = service.SaveChanges(dt);
                
                Assert.False(success, "Save should fail when file is locked for writing");
                Assert.NotEmpty(msg);
            }
        }
        finally
        {
            lockStream?.Dispose();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    private static string FindResourceFile(string folder, string fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, folder, fileName);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", folder, fileName);
    }
}
