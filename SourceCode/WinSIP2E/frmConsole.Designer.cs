namespace WinSIP2E
{
    partial class frmConsole
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
            this.txtTerminal = new System.Windows.Forms.TextBox();
            this.cboTraceLevel = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmdCopy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtTerminal
            // 
            this.txtTerminal.Font = new System.Drawing.Font("MS Reference Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTerminal.Location = new System.Drawing.Point(14, 12);
            this.txtTerminal.Multiline = true;
            this.txtTerminal.Name = "txtTerminal";
            this.txtTerminal.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTerminal.Size = new System.Drawing.Size(557, 572);
            this.txtTerminal.TabIndex = 24;
            // 
            // cboTraceLevel
            // 
            this.cboTraceLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTraceLevel.FormattingEnabled = true;
            this.cboTraceLevel.Location = new System.Drawing.Point(14, 614);
            this.cboTraceLevel.Name = "cboTraceLevel";
            this.cboTraceLevel.Size = new System.Drawing.Size(139, 21);
            this.cboTraceLevel.TabIndex = 25;
            this.cboTraceLevel.SelectedIndexChanged += new System.EventHandler(this.cboTraceLevel_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 598);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Log message level:";
            // 
            // cmdCopy
            // 
            this.cmdCopy.Location = new System.Drawing.Point(443, 598);
            this.cmdCopy.Name = "cmdCopy";
            this.cmdCopy.Size = new System.Drawing.Size(129, 37);
            this.cmdCopy.TabIndex = 27;
            this.cmdCopy.Text = "Copy to Clipboard";
            this.cmdCopy.UseVisualStyleBackColor = true;
            this.cmdCopy.Click += new System.EventHandler(this.cmdCopy_Click);
            // 
            // frmConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 661);
            this.Controls.Add(this.cmdCopy);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cboTraceLevel);
            this.Controls.Add(this.txtTerminal);
            this.Name = "frmConsole";
            this.Text = "Console";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmConsole_Closing);
            this.Load += new System.EventHandler(this.Console_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtTerminal;
        private System.Windows.Forms.ComboBox cboTraceLevel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button cmdCopy;
    }
}