namespace WinSIP2E
{
    partial class Settings
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtServerAddress = new System.Windows.Forms.TextBox();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.gbConnSettings = new System.Windows.Forms.GroupBox();
            this.txtCertExpires = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.chkOverrideName = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtManualServerName = new System.Windows.Forms.TextBox();
            this.gbClientSettings = new System.Windows.Forms.GroupBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cmdLoadCertificate = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtMachineID = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtPinCode = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.gbConnSettings.SuspendLayout();
            this.gbClientSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Address:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(85, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Port:";
            // 
            // txtServerAddress
            // 
            this.txtServerAddress.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "ServerAddress", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtServerAddress.Location = new System.Drawing.Point(120, 22);
            this.txtServerAddress.Name = "txtServerAddress";
            this.txtServerAddress.Size = new System.Drawing.Size(340, 20);
            this.txtServerAddress.TabIndex = 5;
            this.txtServerAddress.Text = global::WinSIP2E.Properties.Settings.Default.ServerAddress;
            this.txtServerAddress.TextChanged += new System.EventHandler(this.txtServerAddress_TextChanged);
            // 
            // txtServerPort
            // 
            this.txtServerPort.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "ServerPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtServerPort.Location = new System.Drawing.Point(120, 48);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(62, 20);
            this.txtServerPort.TabIndex = 6;
            this.txtServerPort.Text = global::WinSIP2E.Properties.Settings.Default.ServerPort;
            // 
            // gbConnSettings
            // 
            this.gbConnSettings.Controls.Add(this.txtCertExpires);
            this.gbConnSettings.Controls.Add(this.label9);
            this.gbConnSettings.Controls.Add(this.textBox2);
            this.gbConnSettings.Controls.Add(this.label4);
            this.gbConnSettings.Controls.Add(this.chkOverrideName);
            this.gbConnSettings.Controls.Add(this.label3);
            this.gbConnSettings.Controls.Add(this.txtManualServerName);
            this.gbConnSettings.Controls.Add(this.txtServerAddress);
            this.gbConnSettings.Controls.Add(this.label2);
            this.gbConnSettings.Controls.Add(this.txtServerPort);
            this.gbConnSettings.Controls.Add(this.label1);
            this.gbConnSettings.Location = new System.Drawing.Point(12, 6);
            this.gbConnSettings.Name = "gbConnSettings";
            this.gbConnSettings.Size = new System.Drawing.Size(523, 155);
            this.gbConnSettings.TabIndex = 7;
            this.gbConnSettings.TabStop = false;
            this.gbConnSettings.Text = "Server Settings";
            // 
            // txtCertExpires
            // 
            this.txtCertExpires.Enabled = false;
            this.txtCertExpires.Location = new System.Drawing.Point(121, 126);
            this.txtCertExpires.Name = "txtCertExpires";
            this.txtCertExpires.Size = new System.Drawing.Size(213, 20);
            this.txtCertExpires.TabIndex = 11;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 129);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(106, 13);
            this.label9.TabIndex = 12;
            this.label9.Text = "Certificate Expiration:";
            // 
            // textBox2
            // 
            this.textBox2.Enabled = false;
            this.textBox2.Location = new System.Drawing.Point(120, 100);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(213, 20);
            this.textBox2.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(43, 103);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Certificate ID:";
            // 
            // chkOverrideName
            // 
            this.chkOverrideName.AutoSize = true;
            this.chkOverrideName.Checked = global::WinSIP2E.Properties.Settings.Default.ManuallySetCN;
            this.chkOverrideName.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::WinSIP2E.Properties.Settings.Default, "ManuallySetCN", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkOverrideName.Location = new System.Drawing.Point(354, 77);
            this.chkOverrideName.Name = "chkOverrideName";
            this.chkOverrideName.Size = new System.Drawing.Size(146, 17);
            this.chkOverrideName.TabIndex = 9;
            this.chkOverrideName.Text = "Manually set server name";
            this.chkOverrideName.UseVisualStyleBackColor = true;
            this.chkOverrideName.CheckedChanged += new System.EventHandler(this.chkOverrideName_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(76, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Name:";
            // 
            // txtManualServerName
            // 
            this.txtManualServerName.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "ServerCN", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtManualServerName.Enabled = false;
            this.txtManualServerName.Location = new System.Drawing.Point(120, 74);
            this.txtManualServerName.Name = "txtManualServerName";
            this.txtManualServerName.Size = new System.Drawing.Size(214, 20);
            this.txtManualServerName.TabIndex = 7;
            this.txtManualServerName.Text = global::WinSIP2E.Properties.Settings.Default.ServerCN;
            // 
            // gbClientSettings
            // 
            this.gbClientSettings.Controls.Add(this.textBox4);
            this.gbClientSettings.Controls.Add(this.label8);
            this.gbClientSettings.Controls.Add(this.cmdLoadCertificate);
            this.gbClientSettings.Controls.Add(this.button1);
            this.gbClientSettings.Controls.Add(this.textBox3);
            this.gbClientSettings.Controls.Add(this.label7);
            this.gbClientSettings.Controls.Add(this.txtMachineID);
            this.gbClientSettings.Controls.Add(this.label6);
            this.gbClientSettings.Controls.Add(this.txtPinCode);
            this.gbClientSettings.Controls.Add(this.label5);
            this.gbClientSettings.Location = new System.Drawing.Point(12, 167);
            this.gbClientSettings.Name = "gbClientSettings";
            this.gbClientSettings.Size = new System.Drawing.Size(523, 146);
            this.gbClientSettings.TabIndex = 8;
            this.gbClientSettings.TabStop = false;
            this.gbClientSettings.Text = "Generate WinSIP Certificate";
            // 
            // textBox4
            // 
            this.textBox4.BackColor = System.Drawing.SystemColors.Control;
            this.textBox4.Enabled = false;
            this.textBox4.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox4.Location = new System.Drawing.Point(120, 97);
            this.textBox4.Multiline = true;
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(213, 43);
            this.textBox4.TabIndex = 17;
            this.textBox4.Text = "To request a certificate, enter a pin code and press `Request Certificate`";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(74, 108);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(40, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Status:";
            // 
            // cmdLoadCertificate
            // 
            this.cmdLoadCertificate.Location = new System.Drawing.Point(400, 116);
            this.cmdLoadCertificate.Name = "cmdLoadCertificate";
            this.cmdLoadCertificate.Size = new System.Drawing.Size(117, 24);
            this.cmdLoadCertificate.TabIndex = 14;
            this.cmdLoadCertificate.Text = "Download Response";
            this.cmdLoadCertificate.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(400, 85);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(117, 25);
            this.button1.TabIndex = 13;
            this.button1.Text = "Request Certificate";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // textBox3
            // 
            this.textBox3.Enabled = false;
            this.textBox3.Location = new System.Drawing.Point(120, 69);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(214, 20);
            this.textBox3.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(18, 72);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "New Certificate ID:";
            // 
            // txtMachineID
            // 
            this.txtMachineID.Enabled = false;
            this.txtMachineID.Location = new System.Drawing.Point(120, 43);
            this.txtMachineID.Name = "txtMachineID";
            this.txtMachineID.Size = new System.Drawing.Size(214, 20);
            this.txtMachineID.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(49, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Machine ID:";
            // 
            // txtPinCode
            // 
            this.txtPinCode.Location = new System.Drawing.Point(120, 17);
            this.txtPinCode.Name = "txtPinCode";
            this.txtPinCode.Size = new System.Drawing.Size(62, 20);
            this.txtPinCode.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(62, 20);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Pin code:";
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 325);
            this.Controls.Add(this.gbClientSettings);
            this.Controls.Add(this.gbConnSettings);
            this.Name = "Settings";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            this.gbConnSettings.ResumeLayout(false);
            this.gbConnSettings.PerformLayout();
            this.gbClientSettings.ResumeLayout(false);
            this.gbClientSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtServerAddress;
        private System.Windows.Forms.TextBox txtServerPort;
        private System.Windows.Forms.GroupBox gbConnSettings;
        private System.Windows.Forms.TextBox txtCertExpires;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkOverrideName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtManualServerName;
        private System.Windows.Forms.GroupBox gbClientSettings;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button cmdLoadCertificate;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtMachineID;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtPinCode;
        private System.Windows.Forms.Label label5;
    }
}