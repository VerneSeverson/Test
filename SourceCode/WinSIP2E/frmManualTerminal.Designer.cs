namespace WinSIP2E
{
    partial class frmManualTerminal
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtManualServerName = new System.Windows.Forms.TextBox();
            this.txtServerAddress = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rbCustomConnection = new System.Windows.Forms.RadioButton();
            this.rbDefaultConnection = new System.Windows.Forms.RadioButton();
            this.rbActiveConnection = new System.Windows.Forms.RadioButton();
            this.txtTerminal = new System.Windows.Forms.TextBox();
            this.txtAccumString = new System.Windows.Forms.TextBox();
            this.chkBase64Encode = new System.Windows.Forms.CheckBox();
            this.cmdAppendAndSend = new System.Windows.Forms.Button();
            this.cmdConnect = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.tmrKeepAlive = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtManualServerName);
            this.groupBox1.Controls.Add(this.txtServerAddress);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtServerPort);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.rbCustomConnection);
            this.groupBox1.Controls.Add(this.rbDefaultConnection);
            this.groupBox1.Controls.Add(this.rbActiveConnection);
            this.groupBox1.Location = new System.Drawing.Point(9, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(562, 124);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(364, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Name:";
            // 
            // txtManualServerName
            // 
            this.txtManualServerName.Enabled = false;
            this.txtManualServerName.Location = new System.Drawing.Point(408, 96);
            this.txtManualServerName.Name = "txtManualServerName";
            this.txtManualServerName.Size = new System.Drawing.Size(145, 20);
            this.txtManualServerName.TabIndex = 13;
            this.txtManualServerName.Text = "aws.naclogin.net";
            // 
            // txtServerAddress
            // 
            this.txtServerAddress.Enabled = false;
            this.txtServerAddress.Location = new System.Drawing.Point(408, 44);
            this.txtServerAddress.Name = "txtServerAddress";
            this.txtServerAddress.Size = new System.Drawing.Size(145, 20);
            this.txtServerAddress.TabIndex = 9;
            this.txtServerAddress.Text = "aws.naclogin.net";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(373, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Port:";
            // 
            // txtServerPort
            // 
            this.txtServerPort.Enabled = false;
            this.txtServerPort.Location = new System.Drawing.Point(408, 70);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(48, 20);
            this.txtServerPort.TabIndex = 10;
            this.txtServerPort.Text = "1102";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(354, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Address:";
            // 
            // rbCustomConnection
            // 
            this.rbCustomConnection.AutoSize = true;
            this.rbCustomConnection.Location = new System.Drawing.Point(388, 16);
            this.rbCustomConnection.Name = "rbCustomConnection";
            this.rbCustomConnection.Size = new System.Drawing.Size(158, 17);
            this.rbCustomConnection.TabIndex = 2;
            this.rbCustomConnection.TabStop = true;
            this.rbCustomConnection.Text = "Custom Connection Settings";
            this.rbCustomConnection.UseVisualStyleBackColor = true;
            this.rbCustomConnection.CheckedChanged += new System.EventHandler(this.rbCustomConnection_CheckedChanged);
            // 
            // rbDefaultConnection
            // 
            this.rbDefaultConnection.AutoSize = true;
            this.rbDefaultConnection.Location = new System.Drawing.Point(166, 16);
            this.rbDefaultConnection.Name = "rbDefaultConnection";
            this.rbDefaultConnection.Size = new System.Drawing.Size(157, 17);
            this.rbDefaultConnection.TabIndex = 1;
            this.rbDefaultConnection.TabStop = true;
            this.rbDefaultConnection.Text = "Default Connection Settings";
            this.rbDefaultConnection.UseVisualStyleBackColor = true;
            this.rbDefaultConnection.CheckedChanged += new System.EventHandler(this.rbDefaultConnection_CheckedChanged);
            // 
            // rbActiveConnection
            // 
            this.rbActiveConnection.AutoSize = true;
            this.rbActiveConnection.Location = new System.Drawing.Point(6, 16);
            this.rbActiveConnection.Name = "rbActiveConnection";
            this.rbActiveConnection.Size = new System.Drawing.Size(112, 17);
            this.rbActiveConnection.TabIndex = 0;
            this.rbActiveConnection.TabStop = true;
            this.rbActiveConnection.Text = "Active Connection";
            this.rbActiveConnection.UseVisualStyleBackColor = true;
            this.rbActiveConnection.CheckedChanged += new System.EventHandler(this.rbActiveConnection_CheckedChanged);
            // 
            // txtTerminal
            // 
            this.txtTerminal.Location = new System.Drawing.Point(9, 133);
            this.txtTerminal.MaxLength = 65534;
            this.txtTerminal.Multiline = true;
            this.txtTerminal.Name = "txtTerminal";
            this.txtTerminal.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTerminal.Size = new System.Drawing.Size(563, 554);
            this.txtTerminal.TabIndex = 15;
            this.txtTerminal.TextChanged += new System.EventHandler(this.txtTerminal_TextChanged);
            this.txtTerminal.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtTerminal_KeyDown);
            this.txtTerminal.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtTerminal_KeyPress);
            // 
            // txtAccumString
            // 
            this.txtAccumString.Location = new System.Drawing.Point(134, 700);
            this.txtAccumString.Name = "txtAccumString";
            this.txtAccumString.Size = new System.Drawing.Size(277, 20);
            this.txtAccumString.TabIndex = 27;
            // 
            // chkBase64Encode
            // 
            this.chkBase64Encode.AutoSize = true;
            this.chkBase64Encode.Location = new System.Drawing.Point(241, 736);
            this.chkBase64Encode.Name = "chkBase64Encode";
            this.chkBase64Encode.Size = new System.Drawing.Size(194, 17);
            this.chkBase64Encode.TabIndex = 29;
            this.chkBase64Encode.Text = "Base 64 Encode the Appended File";
            this.chkBase64Encode.UseVisualStyleBackColor = true;
            // 
            // cmdAppendAndSend
            // 
            this.cmdAppendAndSend.Location = new System.Drawing.Point(441, 729);
            this.cmdAppendAndSend.Name = "cmdAppendAndSend";
            this.cmdAppendAndSend.Size = new System.Drawing.Size(131, 29);
            this.cmdAppendAndSend.TabIndex = 28;
            this.cmdAppendAndSend.Text = "Append File and Send";
            this.cmdAppendAndSend.UseVisualStyleBackColor = true;
            this.cmdAppendAndSend.Click += new System.EventHandler(this.cmdAppendAndSend_Click);
            // 
            // cmdConnect
            // 
            this.cmdConnect.Location = new System.Drawing.Point(441, 695);
            this.cmdConnect.Name = "cmdConnect";
            this.cmdConnect.Size = new System.Drawing.Size(131, 28);
            this.cmdConnect.TabIndex = 30;
            this.cmdConnect.Text = "Connect";
            this.cmdConnect.UseVisualStyleBackColor = true;
            this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 703);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(122, 13);
            this.label4.TabIndex = 31;
            this.label4.Text = "Accumulated Command:";
            // 
            // tmrKeepAlive
            // 
            this.tmrKeepAlive.Interval = 10000;
            this.tmrKeepAlive.Tick += new System.EventHandler(this.tmrKeepAlive_Tick);
            // 
            // frmManualTerminal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 761);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cmdConnect);
            this.Controls.Add(this.chkBase64Encode);
            this.Controls.Add(this.cmdAppendAndSend);
            this.Controls.Add(this.txtAccumString);
            this.Controls.Add(this.txtTerminal);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmManualTerminal";
            this.Text = "Manual Terminal";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmManualTerminal_FormClosing);
            this.Load += new System.EventHandler(this.frmManualTerminal_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbCustomConnection;
        private System.Windows.Forms.RadioButton rbDefaultConnection;
        private System.Windows.Forms.RadioButton rbActiveConnection;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtManualServerName;
        private System.Windows.Forms.TextBox txtServerAddress;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtServerPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.TextBox txtAccumString;
        private System.Windows.Forms.CheckBox chkBase64Encode;
        private System.Windows.Forms.Button cmdAppendAndSend;
        private System.Windows.Forms.Button cmdConnect;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer tmrKeepAlive;
    }
}