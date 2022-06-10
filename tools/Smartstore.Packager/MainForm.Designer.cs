namespace Smartstore.Packager
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.btnCreatePackages = new System.Windows.Forms.Button();
            this.txtRootPath = new System.Windows.Forms.TextBox();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.lblRootPath = new System.Windows.Forms.Label();
            this.lblOutputPath = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.Plugins = new System.Windows.Forms.TabPage();
            this.lstModules = new System.Windows.Forms.ListBox();
            this.Themes = new System.Windows.Forms.TabPage();
            this.lstThemes = new System.Windows.Forms.ListBox();
            this.btnReadDescriptions = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnClose = new System.Windows.Forms.Button();
            this.fb = new System.Windows.Forms.FolderBrowserDialog();
            this.btnBrowseRootPath = new System.Windows.Forms.Button();
            this.btnBrowseOutputPath = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.tabMain.SuspendLayout();
            this.Plugins.SuspendLayout();
            this.Themes.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCreatePackages
            // 
            this.btnCreatePackages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCreatePackages.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnCreatePackages.Location = new System.Drawing.Point(10, 530);
            this.btnCreatePackages.Name = "btnCreatePackages";
            this.btnCreatePackages.Size = new System.Drawing.Size(169, 32);
            this.btnCreatePackages.TabIndex = 0;
            this.btnCreatePackages.Text = "Create Package(s)";
            this.btnCreatePackages.UseVisualStyleBackColor = true;
            this.btnCreatePackages.Click += new System.EventHandler(this.btnCreatePackages_Click);
            // 
            // txtRootPath
            // 
            this.txtRootPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRootPath.Location = new System.Drawing.Point(10, 27);
            this.txtRootPath.Name = "txtRootPath";
            this.txtRootPath.Size = new System.Drawing.Size(383, 23);
            this.txtRootPath.TabIndex = 1;
            // 
            // txtOutputPath
            // 
            this.txtOutputPath.Location = new System.Drawing.Point(14, 482);
            this.txtOutputPath.Name = "txtOutputPath";
            this.txtOutputPath.Size = new System.Drawing.Size(379, 23);
            this.txtOutputPath.TabIndex = 2;
            // 
            // lblRootPath
            // 
            this.lblRootPath.AutoSize = true;
            this.lblRootPath.Location = new System.Drawing.Point(7, 9);
            this.lblRootPath.Name = "lblRootPath";
            this.lblRootPath.Size = new System.Drawing.Size(62, 15);
            this.lblRootPath.TabIndex = 3;
            this.lblRootPath.Text = "Root path:";
            // 
            // lblOutputPath
            // 
            this.lblOutputPath.AutoSize = true;
            this.lblOutputPath.Location = new System.Drawing.Point(11, 461);
            this.lblOutputPath.Name = "lblOutputPath";
            this.lblOutputPath.Size = new System.Drawing.Size(75, 15);
            this.lblOutputPath.TabIndex = 4;
            this.lblOutputPath.Text = "Output path:";
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.Plugins);
            this.tabMain.Controls.Add(this.Themes);
            this.tabMain.Location = new System.Drawing.Point(12, 135);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(458, 320);
            this.tabMain.TabIndex = 5;
            // 
            // Plugins
            // 
            this.Plugins.Controls.Add(this.lstModules);
            this.Plugins.Location = new System.Drawing.Point(4, 24);
            this.Plugins.Name = "Plugins";
            this.Plugins.Padding = new System.Windows.Forms.Padding(3);
            this.Plugins.Size = new System.Drawing.Size(450, 292);
            this.Plugins.TabIndex = 0;
            this.Plugins.Text = "Plugins";
            this.Plugins.UseVisualStyleBackColor = true;
            // 
            // lstModules
            // 
            this.lstModules.FormattingEnabled = true;
            this.lstModules.ItemHeight = 15;
            this.lstModules.Location = new System.Drawing.Point(10, 15);
            this.lstModules.Name = "lstModules";
            this.lstModules.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lstModules.Size = new System.Drawing.Size(426, 259);
            this.lstModules.TabIndex = 0;
            // 
            // Themes
            // 
            this.Themes.Controls.Add(this.lstThemes);
            this.Themes.Location = new System.Drawing.Point(4, 24);
            this.Themes.Name = "Themes";
            this.Themes.Padding = new System.Windows.Forms.Padding(3);
            this.Themes.Size = new System.Drawing.Size(450, 292);
            this.Themes.TabIndex = 1;
            this.Themes.Text = "Themes";
            this.Themes.UseVisualStyleBackColor = true;
            // 
            // lstThemes
            // 
            this.lstThemes.FormattingEnabled = true;
            this.lstThemes.ItemHeight = 15;
            this.lstThemes.Location = new System.Drawing.Point(10, 15);
            this.lstThemes.Name = "lstThemes";
            this.lstThemes.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lstThemes.Size = new System.Drawing.Size(426, 259);
            this.lstThemes.TabIndex = 0;
            // 
            // btnReadDescriptions
            // 
            this.btnReadDescriptions.Location = new System.Drawing.Point(10, 93);
            this.btnReadDescriptions.Name = "btnReadDescriptions";
            this.btnReadDescriptions.Size = new System.Drawing.Size(159, 33);
            this.btnReadDescriptions.TabIndex = 6;
            this.btnReadDescriptions.Text = "Read Extensions";
            this.btnReadDescriptions.UseVisualStyleBackColor = true;
            this.btnReadDescriptions.Click += new System.EventHandler(this.btnReadDescriptions_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(360, 530);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(113, 32);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "Quit";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnBrowseRootPath
            // 
            this.btnBrowseRootPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseRootPath.Location = new System.Drawing.Point(399, 26);
            this.btnBrowseRootPath.Name = "btnBrowseRootPath";
            this.btnBrowseRootPath.Size = new System.Drawing.Size(75, 24);
            this.btnBrowseRootPath.TabIndex = 8;
            this.btnBrowseRootPath.Text = "Browse...";
            this.btnBrowseRootPath.UseVisualStyleBackColor = true;
            this.btnBrowseRootPath.Click += new System.EventHandler(this.btnBrowseRootPath_Click);
            // 
            // btnBrowseOutputPath
            // 
            this.btnBrowseOutputPath.Location = new System.Drawing.Point(398, 482);
            this.btnBrowseOutputPath.Name = "btnBrowseOutputPath";
            this.btnBrowseOutputPath.Size = new System.Drawing.Size(75, 24);
            this.btnBrowseOutputPath.TabIndex = 9;
            this.btnBrowseOutputPath.Text = "Browse...";
            this.btnBrowseOutputPath.UseVisualStyleBackColor = true;
            this.btnBrowseOutputPath.Click += new System.EventHandler(this.btnBrowseOutputPath_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblInfo.Location = new System.Drawing.Point(10, 53);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(383, 37);
            this.lblInfo.TabIndex = 10;
            this.lblInfo.Text = "Point this to your root build web folder. Do NOT create packages from source code" +
    " folders.";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(482, 574);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnBrowseOutputPath);
            this.Controls.Add(this.btnBrowseRootPath);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnReadDescriptions);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.lblOutputPath);
            this.Controls.Add(this.lblRootPath);
            this.Controls.Add(this.txtOutputPath);
            this.Controls.Add(this.txtRootPath);
            this.Controls.Add(this.btnCreatePackages);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 500);
            this.Name = "MainForm";
            this.Text = " Smartstore Extension Packager";
            this.tabMain.ResumeLayout(false);
            this.Plugins.ResumeLayout(false);
            this.Themes.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

        #endregion

        private System.Windows.Forms.Button btnCreatePackages;
        private System.Windows.Forms.TextBox txtRootPath;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Label lblRootPath;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage Plugins;
        private System.Windows.Forms.ListBox lstModules;
        private System.Windows.Forms.TabPage Themes;
        private System.Windows.Forms.ListBox lstThemes;
        private System.Windows.Forms.Button btnReadDescriptions;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.FolderBrowserDialog fb;
        private System.Windows.Forms.Button btnBrowseRootPath;
        private System.Windows.Forms.Button btnBrowseOutputPath;
        private System.Windows.Forms.Label lblInfo;
    }
}