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
            this.gbConnSettings = new System.Windows.Forms.GroupBox();
            this.chkOverridePort = new System.Windows.Forms.CheckBox();
            this.txtCertExpires = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.chkOverrideName = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtManualServerName = new System.Windows.Forms.TextBox();
            this.txtServerAddress = new System.Windows.Forms.TextBox();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.gbClientSettings = new System.Windows.Forms.GroupBox();
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
            this.txtNewCertID = new System.Windows.Forms.TextBox();
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
            // gbConnSettings
            // 
            this.gbConnSettings.Controls.Add(this.chkOverridePort);
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
            this.gbConnSettings.Size = new System.Drawing.Size(590, 155);
            this.gbConnSettings.TabIndex = 7;
            this.gbConnSettings.TabStop = false;
            this.gbConnSettings.Text = "Server Settings";
            // 
            // chkOverridePort
            // 
            this.chkOverridePort.AutoSize = true;
            this.chkOverridePort.Checked = global::WinSIP2E.Properties.Settings.Default.ManuallySetPort;
            this.chkOverridePort.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::WinSIP2E.Properties.Settings.Default, "ManuallySetPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkOverridePort.Location = new System.Drawing.Point(354, 50);
            this.chkOverridePort.Name = "chkOverridePort";
            this.chkOverridePort.Size = new System.Drawing.Size(144, 17);
            this.chkOverridePort.TabIndex = 13;
            this.chkOverridePort.Text = "Manually set port number";
            this.chkOverridePort.UseVisualStyleBackColor = true;
            this.chkOverridePort.CheckedChanged += new System.EventHandler(this.chkOverridePort_CheckedChanged);
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
            // gbClientSettings
            // 
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
            this.gbClientSettings.Controls.Add(this.txtNewCertID);
            this.gbClientSettings.Controls.Add(this.label7);
            this.gbClientSettings.Controls.Add(this.txtMachineID);
            this.gbClientSettings.Controls.Add(this.label6);
            this.gbClientSettings.Controls.Add(this.txtPinCode);
            this.gbClientSettings.Controls.Add(this.label5);
            this.gbClientSettings.Location = new System.Drawing.Point(12, 167);
            this.gbClientSettings.Name = "gbClientSettings";
            this.gbClientSettings.Size = new System.Drawing.Size(590, 185);
            this.gbClientSettings.TabIndex = 8;
            this.gbClientSettings.TabStop = false;
            this.gbClientSettings.Text = "Generate WinSIP Certificate";
            // 
            // txtCountry
            // 
            this.txtCountry.Location = new System.Drawing.Point(424, 95);
            this.txtCountry.Name = "txtCountry";
            this.txtCountry.Size = new System.Drawing.Size(160, 20);
            this.txtCountry.TabIndex = 25;
            this.txtCountry.Text = "US";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(372, 97);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(46, 13);
            this.label13.TabIndex = 24;
            this.label13.Text = "Country:";
            // 
            // txtState
            // 
            this.txtState.Location = new System.Drawing.Point(424, 69);
            this.txtState.Name = "txtState";
            this.txtState.Size = new System.Drawing.Size(160, 20);
            this.txtState.TabIndex = 23;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(383, 72);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(35, 13);
            this.label12.TabIndex = 22;
            this.label12.Text = "State:";
            // 
            // txtCity
            // 
            this.txtCity.Location = new System.Drawing.Point(424, 43);
            this.txtCity.Name = "txtCity";
            this.txtCity.Size = new System.Drawing.Size(160, 20);
            this.txtCity.TabIndex = 21;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(391, 46);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(27, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "City:";
            // 
            // txtCompany
            // 
            this.txtCompany.Location = new System.Drawing.Point(424, 17);
            this.txtCompany.Name = "txtCompany";
            this.txtCompany.Size = new System.Drawing.Size(160, 20);
            this.txtCompany.TabIndex = 19;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(364, 20);
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
            this.txtStatus.Location = new System.Drawing.Point(6, 97);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(328, 82);
            this.txtStatus.TabIndex = 17;
            this.txtStatus.Text = "To request a certificate, enter a pin code and press `Request Certificate`";
            // 
            // cmdLoadCertificate
            // 
            this.cmdLoadCertificate.Location = new System.Drawing.Point(462, 154);
            this.cmdLoadCertificate.Name = "cmdLoadCertificate";
            this.cmdLoadCertificate.Size = new System.Drawing.Size(117, 24);
            this.cmdLoadCertificate.TabIndex = 14;
            this.cmdLoadCertificate.Text = "Download Response";
            this.cmdLoadCertificate.UseVisualStyleBackColor = true;
            this.cmdLoadCertificate.Click += new System.EventHandler(this.cmdLoadCertificate_Click);
            // 
            // cmdRequestCert
            // 
            this.cmdRequestCert.Location = new System.Drawing.Point(343, 153);
            this.cmdRequestCert.Name = "cmdRequestCert";
            this.cmdRequestCert.Size = new System.Drawing.Size(117, 25);
            this.cmdRequestCert.TabIndex = 13;
            this.cmdRequestCert.Text = "Request Certificate";
            this.cmdRequestCert.UseVisualStyleBackColor = true;
            this.cmdRequestCert.Click += new System.EventHandler(this.cmdRequestCert_Click);
            // 
            // txtNewCertID
            // 
            this.txtNewCertID.Enabled = false;
            this.txtNewCertID.Location = new System.Drawing.Point(120, 69);
            this.txtNewCertID.Name = "txtNewCertID";
            this.txtNewCertID.Size = new System.Drawing.Size(214, 20);
            this.txtNewCertID.TabIndex = 11;
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
            this.ClientSize = new System.Drawing.Size(614, 357);
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
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button cmdLoadCertificate;
        private System.Windows.Forms.Button cmdRequestCert;
        private System.Windows.Forms.TextBox txtNewCertID;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtMachineID;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtPinCode;
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
    }
}