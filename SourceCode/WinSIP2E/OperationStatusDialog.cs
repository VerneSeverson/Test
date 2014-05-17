using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinSIP2E.Operations;

namespace WinSIP2E
{
    public partial class OperationStatusDialog : Form
    {
        private Operation _operation;
        public Operation operation
        {
            get { return _operation; }
            set
            {
                _operation = value;
                this.Text = operation.SubjectLine;

                prgProgressBar.Maximum = 100;
                tmrCheckStatus.Interval = operation.RefreshInterval;
                tmrCheckStatus.Enabled = true;
                _operation.Start();
            }
        }
        public OperationStatusDialog()
        {
            InitializeComponent();
        }

        private void txtStatus_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void OperationStatusDialog_Load(object sender, EventArgs e)
        {

        }

        private void tmrCheckStatus_Tick(object sender, EventArgs e)
        {
            this.Text = operation.SubjectLine;
            txtStatus.Text = operation.StatusMessage;
            prgProgressBar.Value = operation.StatusPercent;
            tmrCheckStatus.Interval = operation.RefreshInterval;

            if (operation.Status == Operation.CompletionCode.InProgress)
                cmdOKCancel.Enabled = operation.AllowCancel;

            if ((operation.Status == Operation.CompletionCode.FinishedSuccess)
                || (operation.Status == Operation.CompletionCode.UserCancelFinish)
                || (operation.Status == Operation.CompletionCode.FinishedError))
            {
                if (operation.RequireUserOK)
                {
                    cmdOKCancel.Enabled = true;
                    cmdOKCancel.Text = "OK";
                    tmrCheckStatus.Enabled = false;
                }
                else
                    this.Close();
            }
            
        }

        private void cmdOKCancel_Click(object sender, EventArgs e)
        {
            if (cmdOKCancel.Text == "OK")
                this.Close();
            else
            {
                cmdOKCancel.Enabled = false;
                operation.Cancel();
            }
        }
    }
}
