using System.Data;
using System.Data.OleDb;
using System.IO;

namespace Kursplan;

public partial class Form1 : Form
{
    private TextBox txtFilePath;
    private Button btnBrowse;
    private Button btnSave;
    private Button btnRefresh;
    private DataGridView dgv;

    private OleDbConnection? connection;
    private OleDbDataAdapter? adapter;
    private DataTable? dataTable;
    private string? currentTableName;

    public Form1()
    {
        InitializeComponent();
        InitializeRuntimeControls();
    }

    private void InitializeRuntimeControls()
    {
        // Top panel
        var panel = new Panel { Dock = DockStyle.Top, Height = 36 };

        txtFilePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(4) };
        btnBrowse = new Button { Text = "Browse...", Width = 90, Dock = DockStyle.Right };
        btnSave = new Button { Text = "Save", Width = 90, Dock = DockStyle.Right, Enabled = false };
        btnRefresh = new Button { Text = "Refresh", Width = 90, Dock = DockStyle.Right, Enabled = false };

        btnBrowse.Click += BtnBrowse_Click;
        btnSave.Click += BtnSave_Click;
        btnRefresh.Click += BtnRefresh_Click;

        panel.Controls.Add(txtFilePath);
        panel.Controls.Add(btnBrowse);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnRefresh);

        Controls.Add(panel);

        // Data grid
        dgv = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        Controls.Add(dgv);

        // If an Access file is next to the EXE, pick it by default (first .accdb/.mdb found)
        try
        {
            var startup = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            var candidates = Directory.EnumerateFiles(startup, "*.accdb").Concat(Directory.EnumerateFiles(startup, "*.mdb")).ToList();
            if (candidates.Any())
            {
                LoadDatabase(candidates.First());
            }
        }
        catch { /* ignore */ }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = "Access Files (*.accdb;*.mdb)|*.accdb;*.mdb|All files (*.*)|*.*";
        dlg.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadDatabase(dlg.FileName);
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtFilePath.Text)) LoadDatabase(txtFilePath.Text);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SaveChanges();
    }

    private void LoadDatabase(string path)
    {
        txtFilePath.Text = path;
        btnSave.Enabled = false;
        btnRefresh.Enabled = false;

        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }

        // Try providers (ACE then Jet)
        var providers = new[] { "Microsoft.ACE.OLEDB.12.0", "Microsoft.Jet.OLEDB.4.0" };
        bool opened = false;
        foreach (var prov in providers)
        {
            var connStr = $"Provider={prov};Data Source={path};Persist Security Info=False;";
            try
            {
                var test = new OleDbConnection(connStr);
                test.Open();
                test.Close();
                connection = new OleDbConnection(connStr);
                opened = true;
                break;
            }
            catch { /* try next */ }
        }

        if (!opened || connection == null)
        {
            MessageBox.Show("Failed to open Access file. Make sure the Access Database Engine is installed and the file is valid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            connection.Open();
            var tableName = GetFirstUserTableName(connection);
            if (tableName == null)
            {
                MessageBox.Show("No user tables found in the Access file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                connection.Close();
                return;
            }

            LoadTable(tableName);
            btnSave.Enabled = true;
            btnRefresh.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // keep connection open for updates via adapter; adapter will manage commands
        }
    }

    private string? GetFirstUserTableName(OleDbConnection conn)
    {
        var schema = conn.GetSchema("Tables");
        foreach (DataRow row in schema.Rows)
        {
            var tableType = row["TABLE_TYPE"]?.ToString();
            var tableName = row["TABLE_NAME"]?.ToString();
            if (string.Equals(tableType, "TABLE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(tableName))
            {
                if (!tableName.StartsWith("MSys", StringComparison.OrdinalIgnoreCase)) return tableName;
            }
        }
        return null;
    }

    private void LoadTable(string tableName)
    {
        if (connection == null) return;
        currentTableName = tableName;
        try
        {
            adapter?.Dispose();
            dataTable?.Dispose();

            adapter = new OleDbDataAdapter($"SELECT * FROM [{tableName}]", connection);
            dataTable = new DataTable();
            adapter.Fill(dataTable);
            dgv.DataSource = dataTable;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load table '{tableName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveChanges()
    {
        if (adapter == null || dataTable == null)
        {
            MessageBox.Show("Nothing to save.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var builder = new OleDbCommandBuilder(adapter);
            adapter.Update(dataTable);
            MessageBox.Show("Changes saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // refresh to reflect DB-level default values etc.
            if (!string.IsNullOrEmpty(txtFilePath.Text)) LoadDatabase(txtFilePath.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
