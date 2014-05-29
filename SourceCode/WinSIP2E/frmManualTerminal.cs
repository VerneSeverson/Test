using ForwardLibrary.Communications;
using ForwardLibrary.Communications.STXETX;
using ForwardLibrary.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinSIP2E.Operations;

namespace WinSIP2E
{
    public partial class frmManualTerminal : Form
    {
        private ManualTraceListener thisListener = null;
        SourceLevels oldSourceLevel; 
                

        //variables needed for connections
        public StxEtxHandler connection;                

        //variables needed to support command history
        private LinkedList<string> outgoingCMDs = new LinkedList<string>();
        private LinkedList<string> searchOutgoingCMDs = new LinkedList<string>();
        private LinkedList<string> incomingCMDs = new LinkedList<string>();
        string accumCMD = "";

        //keep alive
        DateTime lastMsg = DateTime.Now;
        DateTime lastUserMsg = DateTime.Now; //used for ending the connection if the user has been inactive for too long

        public frmManualTerminal()
        {
            InitializeComponent();
        }

        private void rbActiveConnection_CheckedChanged(object sender, EventArgs e)
        {
            //ToggleConnectionSettings(!rbActiveConnection.Checked);
        }

        private void ToggleConnectionSettings(bool value)
        {
            txtManualServerName.Enabled = value;
            txtServerAddress.Enabled = value;
            txtServerPort.Enabled = value;

            txtServerAddress.Text = WinSIP2E.Properties.Settings.Default.ServerAddress;
            txtManualServerName.Text = WinSIP2E.Properties.Settings.Default.ServerCN;
            txtServerPort.Text = WinSIP2E.Properties.Settings.Default.ServerPort;
        }

        private void rbDefaultConnection_CheckedChanged(object sender, EventArgs e)
        {
            //ToggleConnectionSettings(!rbDefaultConnection.Checked);
        }

        private void rbCustomConnection_CheckedChanged(object sender, EventArgs e)
        {
            ToggleConnectionSettings(rbCustomConnection.Checked);
        }

        private void cmdConnect_Click(object sender, EventArgs e)
        {
            if (cmdConnect.Text == "Connect")
                Connect();
            else
                Disconnect();         
        }

        #region Connect/Disconnect
        private delegate void connDel(string serverAddr, string serverName, int connectionPort);
        private void ConnectWorker(string serverAddr, string serverName, int connectionPort)
        {
            try
            {
                //1. set up the proper messages
                CommLogMessages msgs = new CommLogMessages
                {
                    msgNewTCP_Client = "CONNECTED " + serverAddr + ":" + connectionPort.ToString(),
                    msgTCP_DisconnectWithReason = "DISCONNECTED",
                    msgSuppressTCP_DisconnectReason = true
                };

                //2. set up the trace source for grabbing communications info
                thisListener = new ManualTraceListener(this, new ForwardLog());
                thisListener.TraceOutputOptions |= TraceOptions.DateTime | TraceOptions.Timestamp;
                Program.WinSIP_TS.Listeners.Add(thisListener);
                oldSourceLevel = Program.WinSIP_TS.Switch.Level;
                Program.WinSIP_TS.Switch.Level = SourceLevels.Verbose;
            
                //3. create the connection with security settings based on the port
                if (rbActiveConnection.Checked == false)
                    connection = CreateConnObject(serverAddr, serverName, connectionPort, msgs);                        
            

                //4. setup the listener so it only grabs messages related to this connection
                thisListener.FilterByEventID(connection.CommContext.ConnectionID);
            
                //5. manage the form settings for a new connection
                UpdateFormConnection(true, serverAddr);
            }
            catch (Exception ex)
            {
                IncomingText("CONNECTION FAILED: " + ex.ToString());
                try{ Program.WinSIP_TS.Listeners.Remove(thisListener); }
                catch { }
                try { UpdateFormConnection(false, serverAddr); }
                catch { }
            }
        }

        private StxEtxHandler CreateConnObject(string serverAddr, string serverName, int connectionPort, CommLogMessages msgs)
        {
            StxEtxHandler theConnection = null;
            if (connectionPort == 1100)
                theConnection = new StxEtxHandler(new TCPconnManager(Program.WinSIP_TS, msgs).ConnectToServer(serverAddr, connectionPort), true);
            else if (connectionPort == 443 || connectionPort == 1101)    //Server should use SSL, but utility does not need to provide a certificate
            {
                TCPconnManager cm = new TCPconnManager
                {
                    LogTrace = Program.WinSIP_TS,
                    logMsgs = msgs,
                    sslSettings = new SSL_Settings
                    {
                        peerName = serverName,
                        protocolsAllowed = SslProtocols.Tls12,
                        server = false
                    }
                };
                theConnection = new StxEtxHandler(cm.ConnectToServer(serverAddr, connectionPort), true);
                IncomingText("SSL Encrypted with 1 way authentication");
            }
            else if (connectionPort == 1102) //Server should use SSL and the utility needs to provide a certificate
            {
                if (Program.WinSIP_Cert == null)
                {
                    throw new InvalidOperationException("WinSIP has no certificate. Unable to connect with these settings.");                        
                }

                TCPconnManager cm = new TCPconnManager
                {
                    LogTrace = Program.WinSIP_TS,
                    logMsgs = msgs,
                    sslSettings = new SSL_Settings
                    {
                        peerName = serverName,
                        localCert = Program.WinSIP_Cert.Certificate,
                        server = false
                    }
                };
                theConnection = new StxEtxHandler(cm.ConnectToServer(serverAddr, connectionPort), true);
                IncomingText("SSL Encrypted with 2 way authentication");
            }
            return theConnection;
            
        }

        private void UpdateFormConnection(bool success, string serverAddr)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateFormConnection(success, serverAddr)));
                return;
            }

            if (success)
            {
                this.Text = "Manual Mode - Connected: " + serverAddr;
                cmdConnect.Text = "Disconnect";
                txtTerminal.Focus();
                gbConnection.Enabled = false;

                lastMsg = DateTime.Now; //used for keeping the connection alive
                lastUserMsg = DateTime.Now; //used for ending the connection if the user has been inactive for too long
                tmrKeepAlive.Enabled = true;

                if (rbActiveConnection.Checked)
                    cmdConnect.Enabled = false;
                else
                    cmdConnect.Enabled = true;
            }
            else
                cmdConnect.Enabled = true;
            
        }

        private void Connect()
        {
            string serverAddr = null;
            string serverName = null;
            int connectionPort = 0;            
            
            //connect
            try
            {                                
                if (rbActiveConnection.Checked)
                {
                    IncomingText("Using existing connection");
                    serverAddr = WinSIP2E.Properties.Settings.Default.ServerAddress;                    
                }
                else if (rbDefaultConnection.Checked)
                {
                    connectionPort = WinSIP2E.Properties.Settings.Default.ManuallySetPort ? Convert.ToInt32(WinSIP2E.Properties.Settings.Default.ServerPort) : 1102;
                    serverAddr = WinSIP2E.Properties.Settings.Default.ServerAddress;
                    serverName = WinSIP2E.Properties.Settings.Default.ManuallySetCN ? WinSIP2E.Properties.Settings.Default.ServerCN : WinSIP2E.Properties.Settings.Default.ServerAddress;
                }                                   
                else if (rbCustomConnection.Checked)
                {
                    connectionPort = Convert.ToInt32(txtServerPort.Text);
                    serverAddr = txtServerAddress.Text;
                    serverName = txtManualServerName.Text;
                }
                if (!rbActiveConnection.Checked)
                    IncomingText("Connecting to " + serverAddr);

                cmdConnect.Enabled = false;
                connDel caller = this.ConnectWorker;
                caller.BeginInvoke(serverAddr, serverName, connectionPort, delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
                
            }
            catch (Exception ex)
            {
                IncomingText("CONNECTION FAILED: " + ex.ToString());
            }           
        }

        private void Disconnect()
        {
            gbConnection.Enabled = true;

            //disconnect
            try
            {                
                connection.Dispose();   //close the connection                    
            }
            catch { }
            this.Text = "Manual Mode - Disconnected";
            cmdConnect.Text = "Connect";
            tmrKeepAlive.Enabled = false;

            try
            {
                if (thisListener != null)
                {
                    Program.WinSIP_TS.Listeners.Remove(thisListener);
                    Program.WinSIP_TS.Switch.Level = oldSourceLevel;
                    thisListener = null;
                }
            }
            catch { }
        }
        #endregion

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

            //remove any accumulated command so it can be put back on at the end

            int distToEnd = txtTerminal.Text.Length - txtTerminal.SelectionStart;
            if (accumCMD.Length > 0)
                txtTerminal.Text = txtTerminal.Text.Substring(0, txtTerminal.Text.Length - accumCMD.Length);

            txtTerminal.AppendText(text + "\r\n");

            if (accumCMD.Length > 0)
                txtTerminal.AppendText(accumCMD);

            txtTerminal.Select(txtTerminal.Text.Length - distToEnd, 0);
            lastMsg = DateTime.Now;
            if (connection == null || connection.CommContext == null || connection.CommContext.bConnected == false)
                if (tmrKeepAlive.Enabled == true)    //only disconnect if this is true (to prevent disconnections before the connection is established)
                    Disconnect();
        }

        #region functions for handling the user interface in the terminal (key strokes)
        private bool softIgnoreKey = false, pasteText = false;
        private void txtTerminal_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            softIgnoreKey = false; pasteText = false;

            //distance from end of text:
            int distToEnd = txtTerminal.Text.Length - txtTerminal.SelectionStart;

            //Copy or Paste?
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.C)
                    softIgnoreKey = true;  //copy
                else if (e.KeyCode == Keys.V)
                    pasteText = true;
                else
                    e.SuppressKeyPress = true; //no other keys allowed
            }

            //Using arrows to search command history
            if ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down))
            {
                //indicate that the key should be supressed
                e.SuppressKeyPress = true;

                //search the command history
                string res = DoCMD_Search(e.KeyCode);

                //remove search data from txtTerminal window            
                int index = txtTerminal.Text.Length - accumCMD.Length;
                if (accumCMD.Length > 0)
                    txtTerminal.Text = txtTerminal.Text.Remove(index);

                //save the found data and update txtTerminal and accumCMD
                accumCMD = res;
                txtTerminal.Text = txtTerminal.Text.Insert(index, accumCMD);
                txtTerminal.Select(txtTerminal.Text.Length, 0);
                txtTerminal.ScrollToCaret();
            }

            //Move the cursor position to the end of txtTerminal
            if (distToEnd > accumCMD.Length && (e.Modifiers == Keys.None || pasteText)) //on a different line
                txtTerminal.Select(txtTerminal.Text.Length, 0);

            //BACKSPACE or DELETE pressed
            if ((e.KeyCode == Keys.Back) || (e.KeyCode == Keys.Delete))
            {
                distToEnd = txtTerminal.Text.Length - txtTerminal.SelectionStart;
                int accumCMDindex = accumCMD.Length - distToEnd;

                //text selected?
                if (txtTerminal.SelectionLength > 0)
                    accumCMD = accumCMD.Remove(accumCMDindex, txtTerminal.SelectionLength);
                else
                {
                    if ((e.KeyCode == Keys.Back) && (accumCMDindex > 0))
                        accumCMD = accumCMD.Remove(accumCMDindex - 1, 1);
                    else if ((e.KeyCode == Keys.Delete) && (accumCMDindex < accumCMD.Length))
                        accumCMD = accumCMD.Remove(accumCMDindex, 1);
                    else
                        e.SuppressKeyPress = true; //ignoreKey = true ;
                }
            }

            //debug:
            txtAccumString.Text = accumCMD;
        }

        private void txtTerminal_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {   //only called for printable characters AND: backspace, CR

            //if a key has been pressed that my logic shouldn't handle (but the control logic should):
            if (softIgnoreKey)
                return;

            //we know the cursor position of txtTerminal is in the accumulated string (thanks to KeyDown evt handler)

            //where are we in the accumulated string?
            int distToEnd = txtTerminal.Text.Length - txtTerminal.SelectionStart;
            int accumCMDindex = accumCMD.Length - distToEnd;


            //is text selected?
            if (txtTerminal.SelectionLength > 0 && e.KeyChar != '\r' && accumCMDindex >= 0) //!pasteText)
                accumCMD = accumCMD.Remove(accumCMDindex, txtTerminal.SelectionLength);

            //is a command finished?
            if (e.KeyChar == '\r')
            {
                lastUserMsg = DateTime.Now;
                CommandAccumulated(accumCMD);
                accumCMD = "";
                e.Handled = true;
                txtTerminal.Text += "\r\n";
                txtTerminal.Select(txtTerminal.Text.Length, 0); //txtTerminal.SelectionStart = txtTerminal.Text.Length;
            }
            //are we pasting text?
            else if (pasteText && accumCMDindex >= 0)
            {
                //1st. remove any undesirable characters:
                string cp = Clipboard.GetText();
                char[] BAD_CHARS = new char[] { '\t', '\r', '\n' }; //simple example
                cp = string.Concat(cp.Split(BAD_CHARS, StringSplitOptions.RemoveEmptyEntries));

                //2nd. insert it
                accumCMD = accumCMD.Insert(accumCMDindex, cp);

                //3rd. update the clipboard
                try { Clipboard.SetText(cp); }
                catch { }
            }
            //otherwise -- we must be ading a character
            else if (e.KeyChar != '\b')
            {
                if (accumCMDindex < 0)
                {
                    accumCMDindex = accumCMD.Length;
                    txtTerminal.Select(txtTerminal.Text.Length, 0); //txtTerminal.SelectionStart = txtTerminal.Text.Length;
                }
                accumCMD = accumCMD.Insert(accumCMDindex, "" + e.KeyChar);
            }

            //update our search entries for autocompletion
            UpdateCMD_Search();

            //debug:
            txtAccumString.Text = accumCMD;

        }

        /// <summary>
        /// A command has been accumulated and is ready to be sent off. Handle this.
        /// </summary>
        /// <param name="cmd"></param>
        private void CommandAccumulated(string cmd)
        {
            lastMsg = DateTime.Now;
            outgoingCMDs.AddFirst(cmd);
            //txtTerminal.Text += "\r\nSent (" + DateTime.Now.ToString("HH:mm:ss.ff") + ") --> <STX>" + cmd + "<ETX>\r\n";
            connection.AsyncSendCommand(cmd, 0);

        }
        #endregion

        #region functions to search the previously issued commands

        LinkedListNode<string> searchStringLLN = null;
        /// <summary>
        /// Cycle through the outgoing command history that matches
        /// the accumulated accumCMD string
        /// </summary>
        /// <param name="key">Direction to search in (either Up or Down)</param>
        /// <returns>matching string (or "")</returns>
        private string DoCMD_Search(Keys key)
        {
            string ret = "";
            //cycle through the old commands
            if (searchStringLLN == null)
                if (key == Keys.Up)
                    searchStringLLN = searchOutgoingCMDs.First;
                else
                    searchStringLLN = searchOutgoingCMDs.Last;
            else
                if (key == Keys.Up)
                    if (searchStringLLN.Next != null)
                        searchStringLLN = searchStringLLN.Next;
                    else
                        searchStringLLN = searchOutgoingCMDs.First;
                else
                    if (searchStringLLN.Previous != null)
                        searchStringLLN = searchStringLLN.Previous;
                    else
                        searchStringLLN = searchOutgoingCMDs.Last;

            if (searchStringLLN != null)
                ret = searchStringLLN.Value;

            return ret;
        }

        /// <summary>
        /// Called to re-compile the list of matching outgoing commands
        /// </summary>
        private void UpdateCMD_Search()
        {
            //put together a linked list of previous outgoing commands where all the strings start with accumCMD
            searchOutgoingCMDs = new LinkedList<string>();
            searchStringLLN = null;
            LinkedListNode<string> lln = outgoingCMDs.First;
            while (lln != null)
            {
                if (lln.Value.StartsWith(accumCMD))
                    searchOutgoingCMDs.AddLast(lln.Value);
                lln = lln.Next;
            }
        }

        #endregion

        private void frmManualTerminal_Load(object sender, EventArgs e)
        {            
            this.Text = "Manual Mode - Disconnected";

            if (connection != null)
                rbActiveConnection.Enabled = connection.CommContext.bConnected;
            else
                rbActiveConnection.Enabled = false;
        }

        //LOG CLASS FOR MANUAL MODE
        class ManualTraceListener : ForwardTraceListener
        {
            private int EventID_Filter = -1;
            private frmManualTerminal frm;  //reference to the parent form
            public ManualTraceListener(frmManualTerminal frm, ForwardLog log)
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
                    if ( (EventID_Filter == -1) || (EventID_Filter.ToString() == newest.eventID) )
                        frm.IncomingText(newest.ToManualString());
                }
            }

            /// <summary>
            /// Call this to only display log messages from this event ID 
            /// HINT: set this to a connection ID to only display a certain connection.
            /// </summary>
            /// <param name="eventID"></param>
            public void FilterByEventID(int eventID)
            {
                EventID_Filter = eventID;
            }

        }

        private void cmdAppendAndSend_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Select file to send";
            fDialog.InitialDirectory = Properties.Settings.Default.LastManualBrowseFolder;
            fDialog.CheckFileExists = true;
            fDialog.CheckPathExists = true;
            fDialog.Multiselect = false;

            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                byte[] raw_data = File.ReadAllBytes(fDialog.FileName);
                Properties.Settings.Default.LastManualBrowseFolder = Path.GetDirectoryName(fDialog.FileName);

                string data_read;
                if (chkBase64Encode.Checked)
                    data_read = Convert.ToBase64String(raw_data);
                else
                    data_read = Encoding.Default.GetString(raw_data);

                CommandAccumulated(accumCMD + data_read);
                accumCMD = "";
                txtTerminal.Text += data_read + "\r\n";
                txtTerminal.Select(txtTerminal.Text.Length, 0); //txtTerminal.SelectionStart = txtTerminal.Text.Length;
            }
        }

        private void tmrKeepAlive_Tick(object sender, EventArgs e)
        {
            TimeSpan sinceLastMsg = DateTime.Now - lastMsg;
            if (sinceLastMsg.TotalSeconds > 45)
                CommandAccumulated("");

            TimeSpan sinceLastUserMsg = DateTime.Now - lastUserMsg;
            if (sinceLastUserMsg.TotalSeconds > 300)
                Disconnect();       //should probably have a text box that gives the user an option to stay connected.
        }

        private void txtTerminal_TextChanged(object sender, EventArgs e)
        {

        }

        private void frmManualTerminal_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (thisListener != null)
                {
                    Program.WinSIP_TS.Listeners.Remove(thisListener);
                    Program.WinSIP_TS.Switch.Level = oldSourceLevel;
                    thisListener = null;
                }
            }
            catch { }
        }

    }
}
