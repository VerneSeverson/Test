using ForwardLibrary.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinSIP2E
{
    public partial class frmConsole : Form
    {
        private ConsoleTraceListener thisListener;

        //LOG CLASS FOR CONSOLE
        class ConsoleTraceListener : ForwardTraceListener
        {
            private frmConsole frm;  //reference to the parent form
            public ConsoleTraceListener(frmConsole frm, ForwardLog log)
                : base(log)
            {
                this.frm = frm;
            }

            protected override void LogUpdated()
            {
                LogEntry[] lEntries = theLog.Entries;
                LogEntry newest = lEntries[lEntries.Length - 1];

                if (newest.Msg != null && newest.DateTime != null && newest.Timestamp != null)
                {
                    //we have enough info to post to the terminal window
                    frm.IncomingText(newest.ToManualString());
                }
            }
        }

        /// <summary>
        /// Function to add new text to the terminal window
        /// </summary>
        /// <param name="text">the text to add</param>
        public void IncomingText(string text)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => IncomingText(text)));
                return;
            }

            txtTerminal.AppendText(text + "\r\n");
            
            //txtTerminal.Select(txtTerminal.Text.Length - distToEnd, 0);
            
        }

        public frmConsole()
        {
            InitializeComponent();
        }

        private void Console_Load(object sender, EventArgs e)
        {
            try
            {
                thisListener = new ConsoleTraceListener(this, new ForwardLog());
                thisListener.TraceOutputOptions |= TraceOptions.DateTime | TraceOptions.Timestamp;
                Program.WinSIP_TS.Listeners.Add(thisListener);                
                foreach (var item in Enum.GetValues(typeof(SourceLevels)))
                    cboTraceLevel.Items.Add(item);
                cboTraceLevel.SelectedItem = Program.WinSIP_TS.Switch.Level;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to add the console as a listener. \r\n\r\nError: " + ex.ToString(), "Logging not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void frmConsole_Closing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Program.WinSIP_TS.Listeners.Remove(thisListener);
            }
            catch { }
        }

        private void cboTraceLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            Program.WinSIP_TS.Switch.Level = (SourceLevels) cboTraceLevel.SelectedItem;
        }

        private void cmdCopy_Click(object sender, EventArgs e)
        {
            txtTerminal.SelectAll();
            txtTerminal.Copy();
            txtTerminal.Select(0, 0);
        }
    }
}
