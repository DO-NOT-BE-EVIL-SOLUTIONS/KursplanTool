using System.Windows.Forms;
using System.Drawing;

namespace Kursplan;

partial class Kursplaner
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1280, 720);
        this.Name = "Kursplaner";
        this.Text = "Kursplaner";

        // --- Tool Strip (top) ---
        toolStrip = new ToolStrip();
        btnRefresh = new ToolStripButton("Refresh", null, BtnRefresh_Click) { Enabled = false };
        toolStrip.Items.Add(btnRefresh);
        this.Controls.Add(toolStrip);

        // --- Bottom Panel for Save Button ---
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(5) };
        btnSave = new Button { Text = "Save", Dock = DockStyle.Right, Width = 120, Enabled = false };
        btnSave.Click += BtnSave_Click;
        bottomPanel.Controls.Add(btnSave);
        this.Controls.Add(bottomPanel);

        // --- Status Strip (bottom) ---
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("Ready");
        lblFilePath = new ToolStripStatusLabel() { Spring = true, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        statusStrip.Items.Add(lblStatus);
        statusStrip.Items.Add(lblFilePath);
        this.Controls.Add(statusStrip);

        // --- Split Container (middle) ---
        splitContainer = new SplitContainer()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 20
        };
        this.Controls.Add(splitContainer);

        // --- TreeView (Left Panel) ---
        tvTables = new TreeView()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10)
        };
        splitContainer.Panel1.Controls.Add(tvTables);

        // --- DataGridView (Right Panel) ---
        dgvData = new DataGridView()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersVisible = true,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = SystemColors.Control,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = SystemColors.WindowText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText
            }
        };
        splitContainer.Panel2.Controls.Add(dgvData);
        
        // --- Bring toolbars to front ---
        splitContainer.BringToFront();
        //toolStrip.BringToFront();
        this.ResumeLayout(false);
    }

    #endregion

    // --- UI Controls ---
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private ToolStripStatusLabel lblFilePath;
    private ToolStrip toolStrip;
    private Button btnSave;
    private ToolStripButton btnRefresh;
    private SplitContainer splitContainer;
    private TreeView tvTables;
    private DataGridView dgvData;
}