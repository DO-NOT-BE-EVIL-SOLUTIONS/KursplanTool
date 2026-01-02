using System;
using System.IO;
using System.Data.OleDb;
using Xunit;

namespace Kursplan.Tests;

public class UnitTest1
{
    [Fact]
    public void OpensAccessDatabase()
    {
        var fileName = "Kursprogramm_V1.accdb";
        var path = FindResourceFile("resources", fileName);
        Assert.True(File.Exists(path), $"Resource file not found at {path}");

        var providers = new[] { "Microsoft.ACE.OLEDB.12.0", "Microsoft.Jet.OLEDB.4.0" };
        Exception? lastEx = null;
        foreach (var prov in providers)
        {
            var connStr = $"Provider={prov};Data Source={path};Persist Security Info=False;";
            try
            {
                using var conn = new OleDbConnection(connStr);
                conn.Open();
                conn.Close();
                // success
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }
        }

        Assert.Fail($"Failed to open Access file with known providers. Last error: {lastEx}");
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
