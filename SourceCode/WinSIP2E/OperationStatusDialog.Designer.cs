namespace WinSIP2E
{
    partial class OperationStatusDialog
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
            this.prgProgressBar = new System.Windows.Forms.ProgressBar();
            this.cmdOKCancel = new System.Windows.Forms.Button();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.tmrCheckStatus = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // prgProgressBar
            // 
            this.prgProgressBar.Location = new System.Drawing.Point(28, 40);
            this.prgProgressBar.Name = "prgProgressBar";
            this.prgProgressBar.Size = new System.Drawing.Size(382, 21);
            this.prgProgressBar.TabIndex = 0;
            // 
            // cmdOKCancel
            // 
            this.cmdOKCancel.Location = new System.Drawing.Point(175, 208);
            this.cmdOKCancel.Name = "cmdOKCancel";
            this.cmdOKCancel.Size = new System.Drawing.Size(100, 23);
            this.cmdOKCancel.TabIndex = 1;
            this.cmdOKCancel.Text = "Cancel";
            this.cmdOKCancel.UseVisualStyleBackColor = true;
            this.cmdOKCancel.Click += new System.EventHandler(this.cmdOKCancel_Click);
            // 
            // txtStatus
            // 
            this.txtStatus.BackColor = System.Drawing.SystemColors.Control;
            this.txtStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtStatus.Location = new System.Drawing.Point(28, 86);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(381, 102);
            this.txtStatus.TabIndex = 2;
            this.txtStatus.TextChanged += new System.EventHandler(this.txtStatus_TextChanged);
            // 
            // tmrCheckStatus
            // 
            this.tmrCheckStatus.Tick += new System.EventHandler(this.tmrCheckStatus_Tick);
            // 
            // OperationStatusDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 243);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.cmdOKCancel);
            this.Controls.Add(this.prgProgressBar);
            this.Name = "OperationStatusDialog";
            this.Text = "OperationStatusDialog";
            this.Load += new System.EventHandler(this.OperationStatusDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar prgProgressBar;
        private System.Windows.Forms.Button cmdOKCancel;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Timer tmrCheckStatus;
    }
}