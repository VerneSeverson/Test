using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Crypto;
using ForwardLibrary.Default;
using ForwardLibrary.WinSIPserver;
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
    public partial class frmHome : Form
    {
        WinSIPserver activeConnection = null;

        public frmHome()
        {
            InitializeComponent();
            //
            this.Text = "WinSIP-IP  Version 1.0.0.0  " ;
            txtUsername.Text = "User Name";
            //
            tabMainConnect.Text = "Login";
            tabMainConnect.BackColor = SystemColors.Control;
            tabMainNac.BackColor = SystemColors.Control;
            tabMainCardReader.BackColor = SystemColors.Control;
            //
            cboUnitIDType.Items.Add("MAC Address");
            cboUnitIDType.Items.Add("Unit ID");
            cboUnitIDType.SelectedItem = "MAC Address";
            //
            //cboAuthHost.Items.Add("Host A");
            //cboAuthHost.Items.Add("Host B");
            //cboAuthHost.Items.Add("Host C");
            //cboAuthHost.Items.Add("Host D");
            //cboAuthHost.SelectedItem = "Host A";
            ////
            //cboSettleHost.Items.Add("Host A");
            ////cboSettleHost.Items.Add("Host B");
            ////cboSettleHost.Items.Add("Host B");
            ////cboSettleHost.Items.Add("Host B");
            //cboSettleHost.SelectedItem = "Host A";
            ////
            //cboMessageHost.Items.Add("Host A");
            //cboMessageHost.Items.Add("Host B");
            //cboMessageHost.Items.Add("Host C");
            //cboMessageHost.Items.Add("Host D");
            //cboMessageHost.SelectedItem = "Host C";
            ////
            //cboSplitSettle.Items.Add("Host A");
            //cboSplitSettle.Items.Add("Host B");
            //cboSplitSettle.Items.Add("Host C");
            //cboSplitSettle.Items.Add("Host D");
            //cboSplitSettle.SelectedItem = "Host D";
            ////
            //cboSplitAuthorization.Items.Add("Host A");
            //cboSplitAuthorization.Items.Add("Host B");
            //cboSplitAuthorization.Items.Add("Host C");
            //cboSplitAuthorization.Items.Add("Host D");
            //cboSplitAuthorization.SelectedItem = "Host D";
            ////
            //cboHostA.Items.Add("Not Used");
            //cboHostA.Items.Add("Paymentech");
            //cboHostA.Items.Add("WorldPay");
            //cboHostA.Items.Add("Global");
            //cboHostA.Items.Add("BlueVend");
            //cboHostA.Items.Add("EasyHost");
            //cboHostA.SelectedItem = "Paymentech";
            ////
            //cboHostB.Items.Add("Not Used");
            //cboHostB.Items.Add("Paymentech");
            //cboHostB.Items.Add("WorldPay");
            //cboHostB.Items.Add("Global");
            //cboHostB.Items.Add("BlueVend");
            //cboHostB.Items.Add("EasyHost");
            //cboHostB.SelectedItem = "Not Used";
            ////
            //cboHostC.Items.Add("Not Used");
            //cboHostC.Items.Add("Paymentech");
            //cboHostC.Items.Add("WorldPay");
            //cboHostC.Items.Add("Global");
            //cboHostC.Items.Add("BlueVend");
            //cboHostC.Items.Add("EasyHost");
            //cboHostC.SelectedItem = "Not Used";
            ////
            //cboHostD.Items.Add("Not Used");
            //cboHostD.Items.Add("Paymentech");
            //cboHostD.Items.Add("WorldPay");
            //cboHostD.Items.Add("Global");
            //cboHostD.Items.Add("BlueVend");
            //cboHostD.Items.Add("EasyHost");
            //cboHostD.SelectedItem = "Not Used";
            //
        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void frmHome_Load(object sender, EventArgs e)
        {            
            if (Properties.Settings.Default.UpdateRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateRequired = false;
            }
            Program.UpdateCertificate();

            UpdateUI_gbLogin_Init();

            UpdateUI_gbNAC_Init();

            UpdateUI_StatusLabelInit();            
        }

        override protected void OnClosing(CancelEventArgs e)
        {
            Properties.Settings.Default.UnitID_SelectedIndex = cboUnitIDType.SelectedIndex;
            Properties.Settings.Default.Save();     //save the application settings
            base.OnClosing(e);
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox11_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox13_Enter(object sender, EventArgs e)
        {

        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnCancelTcp_Click(object sender, EventArgs e)
        {

        }

        private void label39_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void lblUcrIdInternal_Click(object sender, EventArgs e)
        {

        }

        private void btnConnectInternalUCR_Click(object sender, EventArgs e)
        {

        }

        private void tabMainProcHosts_Click(object sender, EventArgs e)
        {

        }

        private void label91_Click(object sender, EventArgs e)
        {

        }

        private void label98_Click(object sender, EventArgs e)
        {

        }

        private void label93_Click(object sender, EventArgs e)
        {

        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox18_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox40_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox73_TextChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void cmdLogOut_Click(object sender, EventArgs e)
        {
            LogoutOfServer();           
        }

        private void cmdDisconnect_Click(object sender, EventArgs e)
        {
            PassthroughDisconnectOnly();
        }

        private void cmdSettings_Click(object sender, EventArgs e)
        {
            frmSettings frm = new frmSettings();
            frm.ShowDialog();
        }

        private void cmdConsole_Click(object sender, EventArgs e)
        {
            frmConsole frm = new frmConsole();
            frm.Show();
        }

        private void cmdLogIn_click(object sender, EventArgs e)
        {
            LoginToServer();                        
        }

        private void cmdManual_Click(object sender, EventArgs e)
        {            
            frmManualTerminal frm = new frmManualTerminal();
            try
            {
                if (activeConnection.ProtocolHandler.CommContext.bConnected)
                    frm.homeConnection = activeConnection.ProtocolHandler;
            }
            catch { }

            frm.Show();
        }
        
        private void btnConnect_Click(object sender, EventArgs e)
        {
            PassthroughConnect();
        }



        #region GUI ACTIONS 

        /// <summary>
        /// Call this to log into the BNAC server
        /// </summary>
        private void LoginToServer()
        {      
            //we can further abstract this if needed, but for now use the global info:
            string username = txtUsername.Text, password = txtPassword.Text;
            CStoredCertificate cert = Program.WinSIP_Cert;

            try
            {
                if (cert == null)
                    MessageBox.Show("No valid cetificate is present.\r\n\r\nPlease click the settings button to request a certificate.", "Unable to connect to the server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    try
                    {
                        //1. See if we need to disconnect or clean an old connection (ideally, this should never happen)
                        if (activeConnection != null)
                        {
                            try
                            { activeConnection.Dispose(); }
                            catch { }
                            activeConnection = null;
                        }

                        //2. Create the operation request
                        LoginToServer login = new LoginToServer(txtUsername.Text, CStoredCertificate.MakeSecureString(txtPassword.Text),
                            WinSIP2E.Properties.Settings.Default.ServerAddress,
                            WinSIP2E.Properties.Settings.Default.ManuallySetCN ? WinSIP2E.Properties.Settings.Default.ServerCN : WinSIP2E.Properties.Settings.Default.ServerAddress,
                            WinSIP2E.Properties.Settings.Default.ManuallySetPort ? Convert.ToInt32(WinSIP2E.Properties.Settings.Default.ServerPort) : 1102,
                            Program.WinSIP_Cert, Program.WinSIP_TS);

                        //3. Start the operation
                        OperationStatusDialog frm = new OperationStatusDialog();
                        frm.operation = login;
                        frm.ShowDialog();

                        //4. Link the active connection to WinSIP
                        if (login.Status == Operation.CompletionCode.FinishedSuccess)
                        {
                            activeConnection = login.ServerConnection;
                            activeConnection.ProtocolHandler.PeriodicPing(true, TimeSpan.FromSeconds(Program.ConnectionPingTime));
                            activeConnection.ProtocolHandler.AddCommEventHandler(this.ActiveConnectionEvents);
                            Program.bServerDisconnectExpected = false;

                        //5. Update the UI
                            UpdateUI_ServerLoginChange(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An unexpected error occured: \r\n\r\n" + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected exception caught:\r\n\r\n" + ex.ToString());
            }
        }

        /// <summary>
        /// Call this function to log out and disconnect from the server
        /// </summary>
        private void LogoutOfServer()
        {
            Program.bServerDisconnectExpected = true;   //indicate that the disconnect which is about to occur is expected        
            try { activeConnection.Dispose(); }
            catch { }
            activeConnection = null;
            UpdateUI_ServerLoginChange(false);
        }

        
        /// <summary>
        /// Used to handle communication events from connection to WinSIPserver        
        /// </summary>
        /// <param name="ev"></param>
        protected void ActiveConnectionEvents(ClientEvent ev)
        {
            //only handle disconnect events
            if (ev is ClientDisconnectedEvent)
            {
                if (!Program.bServerDisconnectExpected)
                {
                    //take action only if it is unexpected
                    MessageBox.Show("The connection to the WinSIP server has ended unexpectedly. Please login again.", "WinSIP has Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Program.bServerDisconnectExpected = true;
                    LogoutOfServer();
                }
            }
        }

        /// <summary>
        /// Establish a passthrough connection to a UNAC
        /// </summary>                        
        private void PassthroughConnect()
        {
            //we can further abstract this if needed, but for now use the global info:
            try
            {
                string ID = txtID.Text;
                BNAC_Table.ID_Type id_type = FPS_LibFuncs.ParseEnumFriendlyName<BNAC_Table.ID_Type>(cboUnitIDType.Text);
                WinSIPserver connection = activeConnection;
                if (connection == null)
                    MessageBox.Show("An active connection to a WinSIP server is required to initiate a passthrough connection", "No active server connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (connection.ProtocolHandler.CommContext.bConnected == false)
                    MessageBox.Show("An active connection to a WinSIP server is required to initiate a passthrough connection", "No active server connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    try
                    {
                        EstabilishPassthroughConnection req = new EstabilishPassthroughConnection(txtID.Text,
                            FPS_LibFuncs.ParseEnumFriendlyName<BNAC_Table.ID_Type>(cboUnitIDType.Text),
                            activeConnection,
                            Program.WinSIP_TS);

                        OperationStatusDialog frm = new OperationStatusDialog();
                        frm.operation = req;
                        frm.ShowDialog();
                        if (req.Status == Operation.CompletionCode.FinishedSuccess)
                            UpdateUI_PassthroughConnectChange(true);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An unexpected error occured: \r\n\r\n" + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected exception caught:\r\n\r\n" + ex);
            }
        }

        /// <summary>
        /// Call this function to disconnect the passthrough but keep an active connection to the server
        /// </summary>        
        private void PassthroughDisconnectOnly()
        {
            try
            {
                LogoutOfServer();
                LoginToServer();
            }
            catch (Exception e)
            {
                MessageBox.Show("Caught an unexpected exception:\r\n\r\n" + e);
            }
        }

        /// <summary>
        /// Call this function upon a passthrough connect to a UNAC starting/stopping
        /// </summary>
        /// <param name="connected"></param>
        private void UpdateUI_ServerLoginChange(bool connected)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateUI_ServerLoginChange(connected)));
                return;
            }
            //update the various group boxes:
            gbLogin.Enabled = !connected;    
        
            cmdLogOut.Enabled = connected;
            lblServerConnected.Visible = connected;
            lblServerNotConnected.Visible = !connected;
            UpdateUI_PassthroughConnectChange(false);
            gbNAC.Enabled = connected;
        }

        /// <summary>
        /// Call this function upon a passthrough connect to a UNAC starting/stopping
        /// </summary>
        /// <param name="connected"></param>
        private void UpdateUI_PassthroughConnectChange(bool connected)
        {
            //update the various group boxes:
            gbNAC.Enabled = !connected;
            gbNACsettingsFiles.Enabled = connected;

            cmdDisconnect.Enabled = connected;

            lblNAC_OK.Visible = connected;
            lblNAC_Shutdown.Visible = false;
            lblNAC_NotConnected.Visible = !connected;
        }

        /// <summary>
        /// Function to set up the status labels (call on form load)
        /// </summary>
        private void UpdateUI_StatusLabelInit()
        {
            lblServerConnected.Location = lblServerNotConnected.Location;
            lblServerOr.Visible = false;

            lblNAC_or1.Visible = false;
            lblNAC_or2.Visible = false;
            lblNAC_OK.Location = lblNAC_NotConnected.Location;
            lblNAC_Shutdown.Location = lblNAC_NotConnected.Location;
            UpdateUI_ServerLoginChange(false);
        }

        private void UpdateUI_gbLogin_Init()
        {            
            lblgbLoginInst1.Visible = false;
            lblgbLoginInst2.Visible = false;

        }

        private void UpdateUI_gbNAC_Init()
        {
            cboUnitIDType.Items.Clear();
            foreach (BNAC_Table.ID_Type id_type in Enum.GetValues(typeof(BNAC_Table.ID_Type)))
                cboUnitIDType.Items.Add(FPS_LibFuncs.GetEnumFriendlyName(id_type));
            cboUnitIDType.SelectedIndex = Properties.Settings.Default.UnitID_SelectedIndex;

            lblgbNAC_Inst1.Visible = false;
            lblgbNAC_Inst2.Visible = false;

        }

        
        bool bCheckingIdle = false; 
        private void CheckUI_Timeout()
        {
            if (!bCheckingIdle) //the timer will keep re-entring this if we don't protect it
            {
                bCheckingIdle = true;
                try
                {
                    if ((Program.GetLastInputTime() > Program.IdleTimeout) && (activeConnection != null))
                    {
                        IdleTimeout timeout = new IdleTimeout(DateTime.Now + TimeSpan.FromSeconds(30), this.LogoutOfServer, Program.WinSIP_TS);
                        OperationStatusDialog dlg = new OperationStatusDialog();
                        dlg.operation = timeout;
                        dlg.ShowDialog();

                        //connection was closed:
                        //if (timeout.Status != Operation.CompletionCode.UserCancelFinish)
                        //    LogoutOfServer();
                    }
                }
                finally
                {
                    bCheckingIdle = false;
                }
            }
        }
        #endregion


        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                LoginToServer();
            }
        }

        private void txtID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                PassthroughConnect();
            }
        }

        private void tmrStatus_Tick(object sender, EventArgs e)
        {
            CheckUI_Timeout();
        }

        private void cmdSendScript_Click(object sender, EventArgs e)
        {
            frmManualTerminal frm = new frmManualTerminal();
            try
            {
                //frmManualTerminal                 
                try
                {
                    if (activeConnection.ProtocolHandler.CommContext.bConnected)
                        frm.homeConnection = activeConnection.ProtocolHandler;
                }
                catch { }
                frm.Show(this);
                frm.LoadAndSendScriptFile();                

            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show("Caught an unexpected exception: " + ex);
            }
            finally
            {
                try { frm.Close(); }
                catch { }
            }
        }

        private void label24_Click(object sender, EventArgs e)
        {

        }

        private void textBox36_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblAutID2_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void textBox38_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox41_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAuthIPProtocol_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox86_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox109_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAuthID1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void txtNacPassword_TextChanged(object sender, EventArgs e)
        {

        }

        
    }

}
