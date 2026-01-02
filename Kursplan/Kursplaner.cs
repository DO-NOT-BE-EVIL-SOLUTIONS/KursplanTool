using Kursplan.Services;
using System.Data;
using System.Drawing;

namespace Kursplan;

public partial class Kursplaner : Form
{
    private readonly DatabaseService _dbService;

    
    public Kursplaner()
    {
        _dbService = new DatabaseService();
        InitializeComponent();
        this.Load += Kursplaner_Load;
    }

    private void Kursplaner_Load(object? sender, EventArgs e)
    {
        // This needs to run after the form is shown, so we use BeginInvoke
        BeginInvoke(new Action(InitializeDatabaseConnection));
    }

    private void InitializeDatabaseConnection()
    {
        string? dbPath = FindDatabasePath();

        if (string.IsNullOrEmpty(dbPath))
        {
            MessageBox.Show("No database file selected. The application will now close.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
            return;
        }

        lblStatus.Text = "Connecting to database...";
        var (success, errorMessage) = _dbService.Connect(dbPath);
        if (!success)
        {
            MessageBox.Show($"Failed to connect to the database: {errorMessage}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
            return;
        }

        lblFilePath.Text = dbPath;
        lblStatus.Text = "Validating database schema...";

        var (allExist, missingTables) = _dbService.ValidateSchema(Data.RequiredTables.All);
        if (!allExist)
        {
            var missing = string.Join(", ", missingTables);
            MessageBox.Show($"The selected database is missing required tables: {missing}", "Invalid Schema", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            lblStatus.Text = "Warning: Missing tables.";
        }

        lblStatus.Text = "Loading data...";
        PopulateTableList(missingTables);
        
        // Select and load the default table
        var defaultNode = tvTables.Nodes.Find("LB_Stammdaten", false).FirstOrDefault();
        if (defaultNode != null)
        {
            tvTables.SelectedNode = defaultNode;
            LoadTable(defaultNode.Name);
        }
    }

    private string? FindDatabasePath()
    {
        try
        {
            var startup = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            var candidates = Directory.EnumerateFiles(startup, "*.accdb").Concat(Directory.EnumerateFiles(startup, "*.mdb")).ToList();

            if (candidates.Count == 1)
            {
                return candidates.First();
            }
            else
            {
                return ShowBrowseDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error searching for database file: {ex.Message}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return ShowBrowseDialog();
        }
    }

    private string? ShowBrowseDialog()
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = "Access Files (*.accdb;*.mdb)|*.accdb;*.mdb|All files (*.*)|*.*";
        dlg.Title = "Select Access Database";
        dlg.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            return dlg.FileName;
        }
        return null;
    }
    
    private void PopulateTableList(List<string> missingTables)
    {
        tvTables.Nodes.Clear();

        foreach (var table in Data.RequiredTables.All)
        {
            var display = table == "LB_Stammdaten" ? "Dozentenstamm" : table;
            var node = new TreeNode(display) { Name = table };
            if (missingTables != null && missingTables.Contains(table, StringComparer.OrdinalIgnoreCase))
            {
                node.ForeColor = Color.Red;
            }
            tvTables.Nodes.Add(node);
        }

        // Ensure we don't double-subscribe the handler
        tvTables.NodeMouseClick -= TvTables_NodeMouseClick;
        tvTables.NodeMouseClick += TvTables_NodeMouseClick;

        tvTables.ExpandAll();
    }

    private void TvTables_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node == null) return;
        tvTables.SelectedNode = e.Node;
        if (e.Node.ForeColor == Color.Red)
        {
            MessageBox.Show($"Table '{e.Node.Name}' was not found in the connected database.", "Missing Table", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        LoadTable(e.Node.Name);
    }


    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (dgvData.DataSource is DataTable dt)
        {
            var (success, errorMessage) = _dbService.SaveChanges(dt);
            if (success)
            {
                lblStatus.Text = "Changes saved successfully.";
                // Optionally refresh
                BtnRefresh_Click(sender, e);
            }
            else
            {
                MessageBox.Show(errorMessage, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Save failed.";
            }
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        if (tvTables.SelectedNode != null)
        {
            var tableName = tvTables.SelectedNode.Name;
            LoadTable(tableName);
        }
    }

    private void LoadTable(string tableName)
    {
        try
        {
            lblStatus.Text = $"Loading {tableName}...";
            var dt = _dbService.GetTable(tableName);
            dgvData.DataSource = dt;
            dgvData.ColumnHeadersVisible = true;
            dgvData.Refresh();
            lblStatus.Text = "Ready";
            btnSave.Enabled = true;
            btnRefresh.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load table '{tableName}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Error loading table.";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _dbService.Dispose();
        base.OnFormClosing(e);
    }
}