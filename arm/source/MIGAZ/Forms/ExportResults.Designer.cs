namespace MIGAZ.Forms
{
    partial class ExportResults
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnViewTemplate = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txtMessages = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.cboTenants = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnGenerateInstructions = new System.Windows.Forms.Button();
            this.txtRGName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cboRGLocation = new System.Windows.Forms.ComboBox();
            this.cboSubscription = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(11, 10);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(640, 337);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnViewTemplate);
            this.tabPage1.Controls.Add(this.pictureBox1);
            this.tabPage1.Controls.Add(this.txtMessages);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage1.Size = new System.Drawing.Size(632, 308);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Status";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnViewTemplate
            // 
            this.btnViewTemplate.Location = new System.Drawing.Point(24, 261);
            this.btnViewTemplate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnViewTemplate.Name = "btnViewTemplate";
            this.btnViewTemplate.Size = new System.Drawing.Size(150, 35);
            this.btnViewTemplate.TabIndex = 3;
            this.btnViewTemplate.Text = "View Template";
            this.btnViewTemplate.UseVisualStyleBackColor = true;
            this.btnViewTemplate.Click += new System.EventHandler(this.btnViewTemplate_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::MIGAZ.Properties.Resources.Resource_group;
            this.pictureBox1.Location = new System.Drawing.Point(15, 12);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(92, 86);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // txtMessages
            // 
            this.txtMessages.Location = new System.Drawing.Point(46, 150);
            this.txtMessages.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtMessages.Multiline = true;
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ReadOnly = true;
            this.txtMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessages.Size = new System.Drawing.Size(480, 90);
            this.txtMessages.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 123);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(469, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "Please note the following messages arising from the template generation:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(122, 47);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(497, 17);
            this.label7.TabIndex = 0;
            this.label7.Text = "Click the Next Steps tab to learn how to deploy it to Azure Resource Manager.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(122, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(319, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Your template has been generated successfully!  ";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.tabPage2.Controls.Add(this.cboTenants);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.btnGenerateInstructions);
            this.tabPage2.Controls.Add(this.txtRGName);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.cboRGLocation);
            this.tabPage2.Controls.Add(this.cboSubscription);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabPage2.Size = new System.Drawing.Size(632, 308);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Next Steps";
            this.tabPage2.Click += new System.EventHandler(this.tabPage2_Click);
            // 
            // cboTenants
            // 
            this.cboTenants.DisplayMember = "SubscriptionName";
            this.cboTenants.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTenants.FormattingEnabled = true;
            this.cboTenants.Location = new System.Drawing.Point(175, 39);
            this.cboTenants.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboTenants.Name = "cboTenants";
            this.cboTenants.Size = new System.Drawing.Size(390, 24);
            this.cboTenants.TabIndex = 9;
            this.cboTenants.SelectedIndexChanged += new System.EventHandler(this.cboTenants_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(18, 46);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 17);
            this.label8.TabIndex = 8;
            this.label8.Text = "Tenants:";
            // 
            // btnGenerateInstructions
            // 
            this.btnGenerateInstructions.Location = new System.Drawing.Point(175, 209);
            this.btnGenerateInstructions.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnGenerateInstructions.Name = "btnGenerateInstructions";
            this.btnGenerateInstructions.Size = new System.Drawing.Size(178, 38);
            this.btnGenerateInstructions.TabIndex = 7;
            this.btnGenerateInstructions.Text = "Show Instructions";
            this.btnGenerateInstructions.UseVisualStyleBackColor = true;
            this.btnGenerateInstructions.Click += new System.EventHandler(this.btnGenerateInstructions_Click);
            // 
            // txtRGName
            // 
            this.txtRGName.Location = new System.Drawing.Point(175, 117);
            this.txtRGName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtRGName.Name = "txtRGName";
            this.txtRGName.Size = new System.Drawing.Size(261, 22);
            this.txtRGName.TabIndex = 4;
            this.txtRGName.Text = "MigratedResources";
            this.txtRGName.Leave += new System.EventHandler(this.txtRGName_Leave);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 160);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 17);
            this.label6.TabIndex = 5;
            this.label6.Text = "Location:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 122);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(148, 17);
            this.label5.TabIndex = 3;
            this.label5.Text = "New Resource Group:";
            // 
            // cboRGLocation
            // 
            this.cboRGLocation.FormattingEnabled = true;
            this.cboRGLocation.Items.AddRange(new object[] {
            "East US",
            "East US 2",
            "West US",
            "Central US",
            "North Central US",
            "South Central US",
            "North Europe",
            "West Europe",
            "East Asia",
            "Southeast Asia",
            "Japan East",
            "Japan West",
            "Australia East",
            "Australia Southeast",
            "Brazil South",
            "South India",
            "Central India",
            "West India",
            "Canada Central",
            "Canada East",
            "West US 2",
            "West Central US",
            "UK South",
            "UK West"});
            this.cboRGLocation.Location = new System.Drawing.Point(175, 157);
            this.cboRGLocation.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboRGLocation.Name = "cboRGLocation";
            this.cboRGLocation.Size = new System.Drawing.Size(261, 24);
            this.cboRGLocation.TabIndex = 6;
            // 
            // cboSubscription
            // 
            this.cboSubscription.DisplayMember = "SubscriptionName";
            this.cboSubscription.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSubscription.FormattingEnabled = true;
            this.cboSubscription.Location = new System.Drawing.Point(175, 79);
            this.cboSubscription.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboSubscription.Name = "cboSubscription";
            this.cboSubscription.Size = new System.Drawing.Size(390, 24);
            this.cboSubscription.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 81);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 17);
            this.label4.TabIndex = 1;
            this.label4.Text = "Subscription:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(419, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "Now let’s get this template deployed to Azure Resource Manager!\r\n";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(525, 360);
            this.btnOK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(125, 33);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // ExportResults
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 408);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportResults";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Export Complete";
            this.Load += new System.EventHandler(this.ExportResults_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtMessages;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRGName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboSubscription;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboRGLocation;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnViewTemplate;
        private System.Windows.Forms.Button btnGenerateInstructions;
        private System.Windows.Forms.ComboBox cboTenants;
        private System.Windows.Forms.Label label8;
    }
}