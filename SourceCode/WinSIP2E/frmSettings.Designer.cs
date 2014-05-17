namespace WinSIP2E
{
    partial class frmSettings
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
            this.gbConnSettings = new System.Windows.Forms.GroupBox();
            this.txtCertificateID = new System.Windows.Forms.MaskedTextBox();
            this.chkOverridePort = new System.Windows.Forms.CheckBox();
            this.txtCertExpires = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.chkOverrideName = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtManualServerName = new System.Windows.Forms.TextBox();
            this.txtServerAddress = new System.Windows.Forms.TextBox();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.gbClientSettings = new System.Windows.Forms.GroupBox();
            this.txtPinCode = new System.Windows.Forms.MaskedTextBox();
            this.txtNewCertID = new System.Windows.Forms.MaskedTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtCountry = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtState = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtCity = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtCompany = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.cmdLoadCertificate = new System.Windows.Forms.Button();
            this.cmdRequestCert = new System.Windows.Forms.Button();
            this.txtMachineID = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
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
            // gbConnSettings
            // 
            this.gbConnSettings.Controls.Add(this.txtCertificateID);
            this.gbConnSettings.Controls.Add(this.chkOverridePort);
            this.gbConnSettings.Controls.Add(this.txtCertExpires);
            this.gbConnSettings.Controls.Add(this.label9);
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
            this.gbConnSettings.Size = new System.Drawing.Size(590, 155);
            this.gbConnSettings.TabIndex = 7;
            this.gbConnSettings.TabStop = false;
            this.gbConnSettings.Text = "Server Settings";
            // 
            // txtCertificateID
            // 
            this.txtCertificateID.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "CertificateID", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtCertificateID.Location = new System.Drawing.Point(121, 100);
            this.txtCertificateID.Mask = "&&&&-&&&&-&&&&";
            this.txtCertificateID.Name = "txtCertificateID";
            this.txtCertificateID.ReadOnly = true;
            this.txtCertificateID.Size = new System.Drawing.Size(144, 20);
            this.txtCertificateID.TabIndex = 29;
            this.txtCertificateID.Text = global::WinSIP2E.Properties.Settings.Default.CertificateID;
            // 
            // chkOverridePort
            // 
            this.chkOverridePort.AutoSize = true;
            this.chkOverridePort.Checked = global::WinSIP2E.Properties.Settings.Default.ManuallySetPort;
            this.chkOverridePort.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::WinSIP2E.Properties.Settings.Default, "ManuallySetPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkOverridePort.Location = new System.Drawing.Point(282, 50);
            this.chkOverridePort.Name = "chkOverridePort";
            this.chkOverridePort.Size = new System.Drawing.Size(144, 17);
            this.chkOverridePort.TabIndex = 2;
            this.chkOverridePort.Text = "Manually set port number";
            this.chkOverridePort.UseVisualStyleBackColor = true;
            this.chkOverridePort.CheckedChanged += new System.EventHandler(this.chkOverridePort_CheckedChanged);
            // 
            // txtCertExpires
            // 
            this.txtCertExpires.Enabled = false;
            this.txtCertExpires.Location = new System.Drawing.Point(121, 126);
            this.txtCertExpires.Name = "txtCertExpires";
            this.txtCertExpires.Size = new System.Drawing.Size(314, 20);
            this.txtCertExpires.TabIndex = 11;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 129);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(90, 13);
            this.label9.TabIndex = 12;
            this.label9.Text = "Certificate Status:";
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
            this.chkOverrideName.Location = new System.Drawing.Point(282, 77);
            this.chkOverrideName.Name = "chkOverrideName";
            this.chkOverrideName.Size = new System.Drawing.Size(146, 17);
            this.chkOverrideName.TabIndex = 4;
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
            this.txtManualServerName.Size = new System.Drawing.Size(145, 20);
            this.txtManualServerName.TabIndex = 5;
            this.txtManualServerName.Text = global::WinSIP2E.Properties.Settings.Default.ServerCN;
            // 
            // txtServerAddress
            // 
            this.txtServerAddress.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "ServerAddress", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtServerAddress.Location = new System.Drawing.Point(120, 22);
            this.txtServerAddress.Name = "txtServerAddress";
            this.txtServerAddress.Size = new System.Drawing.Size(315, 20);
            this.txtServerAddress.TabIndex = 1;
            this.txtServerAddress.Text = global::WinSIP2E.Properties.Settings.Default.ServerAddress;
            this.txtServerAddress.TextChanged += new System.EventHandler(this.txtServerAddress_TextChanged);
            // 
            // txtServerPort
            // 
            this.txtServerPort.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::WinSIP2E.Properties.Settings.Default, "ServerPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtServerPort.Location = new System.Drawing.Point(120, 48);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(48, 20);
            this.txtServerPort.TabIndex = 3;
            this.txtServerPort.Text = global::WinSIP2E.Properties.Settings.Default.ServerPort;
            // 
            // gbClientSettings
            // 
            this.gbClientSettings.Controls.Add(this.txtPinCode);
            this.gbClientSettings.Controls.Add(this.txtNewCertID);
            this.gbClientSettings.Controls.Add(this.label7);
            this.gbClientSettings.Controls.Add(this.txtCountry);
            this.gbClientSettings.Controls.Add(this.label13);
            this.gbClientSettings.Controls.Add(this.txtState);
            this.gbClientSettings.Controls.Add(this.label12);
            this.gbClientSettings.Controls.Add(this.txtCity);
            this.gbClientSettings.Controls.Add(this.label11);
            this.gbClientSettings.Controls.Add(this.txtCompany);
            this.gbClientSettings.Controls.Add(this.label10);
            this.gbClientSettings.Controls.Add(this.txtStatus);
            this.gbClientSettings.Controls.Add(this.cmdLoadCertificate);
            this.gbClientSettings.Controls.Add(this.cmdRequestCert);
            this.gbClientSettings.Controls.Add(this.txtMachineID);
            this.gbClientSettings.Controls.Add(this.label6);
            this.gbClientSettings.Controls.Add(this.label5);
            this.gbClientSettings.Location = new System.Drawing.Point(12, 167);
            this.gbClientSettings.Name = "gbClientSettings";
            this.gbClientSettings.Size = new System.Drawing.Size(590, 185);
            this.gbClientSettings.TabIndex = 8;
            this.gbClientSettings.TabStop = false;
            this.gbClientSettings.Text = "Generate WinSIP Certificate";
            // 
            // txtPinCode
            // 
            this.txtPinCode.Location = new System.Drawing.Point(121, 17);
            this.txtPinCode.Mask = "000000";
            this.txtPinCode.Name = "txtPinCode";
            this.txtPinCode.Size = new System.Drawing.Size(47, 20);
            this.txtPinCode.TabIndex = 6;
            this.txtPinCode.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.txtPinCode_MaskInputRejected);
            this.txtPinCode.TextChanged += new System.EventHandler(this.txtPinCode_TextChanged);
            // 
            // txtNewCertID
            // 
            this.txtNewCertID.Location = new System.Drawing.Point(467, 159);
            this.txtNewCertID.Mask = "&&&&-&&&&-&&&&";
            this.txtNewCertID.Name = "txtNewCertID";
            this.txtNewCertID.Size = new System.Drawing.Size(117, 20);
            this.txtNewCertID.TabIndex = 28;
            this.txtNewCertID.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.txtNewCertID_MaskInputRejected);
            this.txtNewCertID.TextChanged += new System.EventHandler(this.txtNewCertID_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(351, 162);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(114, 13);
            this.label7.TabIndex = 27;
            this.label7.Text = "Request Certificate ID:";
            // 
            // txtCountry
            // 
            this.txtCountry.Location = new System.Drawing.Point(441, 95);
            this.txtCountry.Name = "txtCountry";
            this.txtCountry.Size = new System.Drawing.Size(24, 20);
            this.txtCountry.TabIndex = 10;
            this.txtCountry.TextChanged += new System.EventHandler(this.txtCountry_TextChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(389, 97);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(46, 13);
            this.label13.TabIndex = 24;
            this.label13.Text = "Country:";
            // 
            // txtState
            // 
            this.txtState.Location = new System.Drawing.Point(441, 69);
            this.txtState.Name = "txtState";
            this.txtState.Size = new System.Drawing.Size(143, 20);
            this.txtState.TabIndex = 9;
            this.txtState.TextChanged += new System.EventHandler(this.txtState_TextChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(400, 72);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(35, 13);
            this.label12.TabIndex = 22;
            this.label12.Text = "State:";
            // 
            // txtCity
            // 
            this.txtCity.Location = new System.Drawing.Point(441, 43);
            this.txtCity.Name = "txtCity";
            this.txtCity.Size = new System.Drawing.Size(143, 20);
            this.txtCity.TabIndex = 8;
            this.txtCity.TextChanged += new System.EventHandler(this.txtCity_TextChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(408, 46);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(27, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "City:";
            // 
            // txtCompany
            // 
            this.txtCompany.Location = new System.Drawing.Point(441, 17);
            this.txtCompany.Name = "txtCompany";
            this.txtCompany.Size = new System.Drawing.Size(143, 20);
            this.txtCompany.TabIndex = 7;
            this.txtCompany.TextChanged += new System.EventHandler(this.txtCompany_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(381, 20);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(54, 13);
            this.label10.TabIndex = 18;
            this.label10.Text = "Company:";
            // 
            // txtStatus
            // 
            this.txtStatus.BackColor = System.Drawing.SystemColors.Control;
            this.txtStatus.Enabled = false;
            this.txtStatus.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStatus.Location = new System.Drawing.Point(6, 72);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(327, 107);
            this.txtStatus.TabIndex = 17;
            this.txtStatus.Text = "To request a certificate, enter a pin code and press `Request Certificate`";
            // 
            // cmdLoadCertificate
            // 
            this.cmdLoadCertificate.Location = new System.Drawing.Point(467, 129);
            this.cmdLoadCertificate.Name = "cmdLoadCertificate";
            this.cmdLoadCertificate.Size = new System.Drawing.Size(117, 24);
            this.cmdLoadCertificate.TabIndex = 12;
            this.cmdLoadCertificate.Text = "Download Response";
            this.cmdLoadCertificate.UseVisualStyleBackColor = true;
            this.cmdLoadCertificate.Click += new System.EventHandler(this.cmdLoadCertificate_Click);
            // 
            // cmdRequestCert
            // 
            this.cmdRequestCert.Location = new System.Drawing.Point(348, 128);
            this.cmdRequestCert.Name = "cmdRequestCert";
            this.cmdRequestCert.Size = new System.Drawing.Size(117, 25);
            this.cmdRequestCert.TabIndex = 11;
            this.cmdRequestCert.Text = "Request Certificate";
            this.cmdRequestCert.UseVisualStyleBackColor = true;
            this.cmdRequestCert.Click += new System.EventHandler(this.cmdRequestCert_Click);
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
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(62, 20);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Pin code:";
            // 
            // frmSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(614, 357);
            this.Controls.Add(this.gbClientSettings);
            this.Controls.Add(this.gbConnSettings);
            this.Name = "frmSettings";
            this.Text = "Advanced Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmSettings_FormClosing);
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
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkOverrideName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtManualServerName;
        private System.Windows.Forms.GroupBox gbClientSettings;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button cmdLoadCertificate;
        private System.Windows.Forms.Button cmdRequestCert;
        private System.Windows.Forms.TextBox txtMachineID;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtCity;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtCompany;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtState;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtCountry;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox chkOverridePort;
        private System.Windows.Forms.MaskedTextBox txtPinCode;
        private System.Windows.Forms.MaskedTextBox txtNewCertID;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.MaskedTextBox txtCertificateID;
    }
}