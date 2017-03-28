﻿namespace MigAz.Forms
{
    partial class MigAzForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MigAzForm));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabMessages = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tabRest = new System.Windows.Forms.TabPage();
            this.txtRest = new System.Windows.Forms.TextBox();
            this.propertyPanel1 = new MigAz.UserControls.PropertyPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.txtDestinationFolder = new System.Windows.Forms.TextBox();
            this.btnChoosePath = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newMigrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aSMToARMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aRMToARMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aWSToARMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeMigrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.visitMigAzOnGithubToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportAnIssueOnGithubToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabMessages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabLog.SuspendLayout();
            this.tabRest.SuspendLayout();
            this.panel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "ResourceGroup");
            this.imageList1.Images.SetKeyName(1, "Disk");
            this.imageList1.Images.SetKeyName(2, "LoadBalancer");
            this.imageList1.Images.SetKeyName(3, "NetworkInterface.png");
            this.imageList1.Images.SetKeyName(4, "NetworkSecurityGroup");
            this.imageList1.Images.SetKeyName(5, "PublicIPAddress.png");
            this.imageList1.Images.SetKeyName(6, "StorageAccount");
            this.imageList1.Images.SetKeyName(7, "VirtualMachine");
            this.imageList1.Images.SetKeyName(8, "AvailabilitySet");
            this.imageList1.Images.SetKeyName(9, "VirtualNetwork");
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 931);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1993, 37);
            this.statusStrip1.TabIndex = 56;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(79, 32);
            this.toolStripStatusLabel1.Text = "Ready";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(1899, 32);
            this.toolStripStatusLabel2.Spring = true;
            this.toolStripStatusLabel2.Text = "http://aka.ms/MigAz";
            this.toolStripStatusLabel2.Click += new System.EventHandler(this.toolStripStatusLabel2_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 40);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyPanel1);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Panel2.Resize += new System.EventHandler(this.splitContainer1_Panel2_Resize);
            this.splitContainer1.Size = new System.Drawing.Size(1993, 891);
            this.splitContainer1.SplitterDistance = 1276;
            this.splitContainer1.TabIndex = 57;
            // 
            // splitContainer2
            // 
            this.splitContainer2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.AutoScroll = true;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer2.Panel2.Resize += new System.EventHandler(this.splitContainer2_Panel2_Resize);
            this.splitContainer2.Size = new System.Drawing.Size(1276, 891);
            this.splitContainer2.SplitterDistance = 600;
            this.splitContainer2.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabMessages);
            this.tabControl1.Controls.Add(this.tabLog);
            this.tabControl1.Controls.Add(this.tabRest);
            this.tabControl1.Location = new System.Drawing.Point(3, 3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(922, 289);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.Resize += new System.EventHandler(this.tabControl1_Resize);
            // 
            // tabMessages
            // 
            this.tabMessages.Controls.Add(this.dataGridView1);
            this.tabMessages.Location = new System.Drawing.Point(8, 39);
            this.tabMessages.Name = "tabMessages";
            this.tabMessages.Padding = new System.Windows.Forms.Padding(3);
            this.tabMessages.Size = new System.Drawing.Size(906, 242);
            this.tabMessages.TabIndex = 0;
            this.tabMessages.Text = "Messages";
            this.tabMessages.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 33;
            this.dataGridView1.Size = new System.Drawing.Size(890, 236);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.txtLog);
            this.tabLog.Location = new System.Drawing.Point(8, 39);
            this.tabLog.Name = "tabLog";
            this.tabLog.Size = new System.Drawing.Size(906, 242);
            this.tabLog.TabIndex = 1;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Margin = new System.Windows.Forms.Padding(0);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(903, 239);
            this.txtLog.TabIndex = 0;
            // 
            // tabRest
            // 
            this.tabRest.Controls.Add(this.txtRest);
            this.tabRest.Location = new System.Drawing.Point(8, 39);
            this.tabRest.Name = "tabRest";
            this.tabRest.Size = new System.Drawing.Size(906, 242);
            this.tabRest.TabIndex = 2;
            this.tabRest.Text = "REST";
            this.tabRest.UseVisualStyleBackColor = true;
            // 
            // txtRest
            // 
            this.txtRest.Location = new System.Drawing.Point(2, 2);
            this.txtRest.Margin = new System.Windows.Forms.Padding(0);
            this.txtRest.Multiline = true;
            this.txtRest.Name = "txtRest";
            this.txtRest.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRest.Size = new System.Drawing.Size(903, 239);
            this.txtRest.TabIndex = 1;
            // 
            // propertyPanel1
            // 
            this.propertyPanel1.Location = new System.Drawing.Point(3, 3);
            this.propertyPanel1.Name = "propertyPanel1";
            this.propertyPanel1.PropertyDetailControl = null;
            this.propertyPanel1.ResourceImage = null;
            this.propertyPanel1.ResourceText = "lblAzureObjectName";
            this.propertyPanel1.Size = new System.Drawing.Size(594, 410);
            this.propertyPanel1.TabIndex = 8;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.btnExport);
            this.panel1.Controls.Add(this.txtDestinationFolder);
            this.panel1.Controls.Add(this.btnChoosePath);
            this.panel1.Location = new System.Drawing.Point(1, 734);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(711, 156);
            this.panel1.TabIndex = 7;
            this.panel1.Resize += new System.EventHandler(this.panel1_Resize);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(17, 112);
            this.button1.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(676, 44);
            this.button1.TabIndex = 10;
            this.button1.Text = "Options";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnExport
            // 
            this.btnExport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExport.Location = new System.Drawing.Point(17, 56);
            this.btnExport.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(676, 44);
            this.btnExport.TabIndex = 9;
            this.btnExport.Text = "&Export 0 objects";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click_1);
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Location = new System.Drawing.Point(17, 7);
            this.txtDestinationFolder.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.Size = new System.Drawing.Size(597, 31);
            this.txtDestinationFolder.TabIndex = 7;
            // 
            // btnChoosePath
            // 
            this.btnChoosePath.Location = new System.Drawing.Point(634, 0);
            this.btnChoosePath.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnChoosePath.Name = "btnChoosePath";
            this.btnChoosePath.Size = new System.Drawing.Size(59, 44);
            this.btnChoosePath.TabIndex = 8;
            this.btnChoosePath.Text = "...";
            this.btnChoosePath.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1993, 40);
            this.menuStrip1.TabIndex = 58;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newMigrationToolStripMenuItem,
            this.closeMigrationToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(64, 36);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newMigrationToolStripMenuItem
            // 
            this.newMigrationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aSMToARMToolStripMenuItem,
            this.aRMToARMToolStripMenuItem,
            this.aWSToARMToolStripMenuItem});
            this.newMigrationToolStripMenuItem.Name = "newMigrationToolStripMenuItem";
            this.newMigrationToolStripMenuItem.Size = new System.Drawing.Size(284, 38);
            this.newMigrationToolStripMenuItem.Text = "&New Migration";
            // 
            // aSMToARMToolStripMenuItem
            // 
            this.aSMToARMToolStripMenuItem.Name = "aSMToARMToolStripMenuItem";
            this.aSMToARMToolStripMenuItem.Size = new System.Drawing.Size(256, 38);
            this.aSMToARMToolStripMenuItem.Text = "ASM To ARM";
            this.aSMToARMToolStripMenuItem.Click += new System.EventHandler(this.aSMToARMToolStripMenuItem_Click);
            // 
            // aRMToARMToolStripMenuItem
            // 
            this.aRMToARMToolStripMenuItem.Name = "aRMToARMToolStripMenuItem";
            this.aRMToARMToolStripMenuItem.Size = new System.Drawing.Size(256, 38);
            this.aRMToARMToolStripMenuItem.Text = "ARM To ARM";
            this.aRMToARMToolStripMenuItem.Click += new System.EventHandler(this.aRMToARMToolStripMenuItem_Click);
            // 
            // aWSToARMToolStripMenuItem
            // 
            this.aWSToARMToolStripMenuItem.Enabled = false;
            this.aWSToARMToolStripMenuItem.Name = "aWSToARMToolStripMenuItem";
            this.aWSToARMToolStripMenuItem.Size = new System.Drawing.Size(256, 38);
            this.aWSToARMToolStripMenuItem.Text = "AWS To ARM";
            this.aWSToARMToolStripMenuItem.Visible = false;
            this.aWSToARMToolStripMenuItem.Click += new System.EventHandler(this.aWSToARMToolStripMenuItem_Click);
            // 
            // closeMigrationToolStripMenuItem
            // 
            this.closeMigrationToolStripMenuItem.Enabled = false;
            this.closeMigrationToolStripMenuItem.Name = "closeMigrationToolStripMenuItem";
            this.closeMigrationToolStripMenuItem.Size = new System.Drawing.Size(284, 38);
            this.closeMigrationToolStripMenuItem.Text = "&Close Migration";
            this.closeMigrationToolStripMenuItem.Click += new System.EventHandler(this.closeMigrationToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(281, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(284, 38);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.visitMigAzOnGithubToolStripMenuItem,
            this.reportAnIssueOnGithubToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(77, 36);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // visitMigAzOnGithubToolStripMenuItem
            // 
            this.visitMigAzOnGithubToolStripMenuItem.Name = "visitMigAzOnGithubToolStripMenuItem";
            this.visitMigAzOnGithubToolStripMenuItem.Size = new System.Drawing.Size(392, 38);
            this.visitMigAzOnGithubToolStripMenuItem.Text = "Visit MigAz on Github";
            this.visitMigAzOnGithubToolStripMenuItem.Click += new System.EventHandler(this.visitMigAzOnGithubToolStripMenuItem_Click);
            // 
            // reportAnIssueOnGithubToolStripMenuItem
            // 
            this.reportAnIssueOnGithubToolStripMenuItem.Name = "reportAnIssueOnGithubToolStripMenuItem";
            this.reportAnIssueOnGithubToolStripMenuItem.Size = new System.Drawing.Size(392, 38);
            this.reportAnIssueOnGithubToolStripMenuItem.Text = "Report an issue on Github";
            this.reportAnIssueOnGithubToolStripMenuItem.Click += new System.EventHandler(this.reportAnIssueOnGithubToolStripMenuItem_Click);
            // 
            // MigAzForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1993, 968);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.MaximumSize = new System.Drawing.Size(5000, 5000);
            this.MinimumSize = new System.Drawing.Size(2000, 1000);
            this.Name = "MigAzForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MigAz";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MigAzForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabMessages.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.tabRest.ResumeLayout(false);
            this.tabRest.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabMessages;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newMigrationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aSMToARMToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aRMToARMToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aWSToARMToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ToolStripMenuItem closeMigrationToolStripMenuItem;
        private System.Windows.Forms.TabPage tabRest;
        private System.Windows.Forms.TextBox txtRest;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.TextBox txtDestinationFolder;
        private System.Windows.Forms.Button btnChoosePath;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reportAnIssueOnGithubToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem visitMigAzOnGithubToolStripMenuItem;
        private UserControls.PropertyPanel propertyPanel1;
    }
}
