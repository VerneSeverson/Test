using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Crypto;
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
            cboAuthHost.Items.Add("Host A");
            cboAuthHost.Items.Add("Host B");
            cboAuthHost.Items.Add("Host C");
            cboAuthHost.Items.Add("Host D");
            cboAuthHost.SelectedItem = "Host A";
            //
            cboSettleHost.Items.Add("Host A");
            cboSettleHost.Items.Add("Host B");
            cboSettleHost.Items.Add("Host B");
            cboSettleHost.Items.Add("Host B");
            cboSettleHost.SelectedItem = "Host A";
            //
            cboMessageHost.Items.Add("Host A");
            cboMessageHost.Items.Add("Host B");
            cboMessageHost.Items.Add("Host C");
            cboMessageHost.Items.Add("Host D");
            cboMessageHost.SelectedItem = "Host C";
            //
            cboAlternateHost.Items.Add("Host A");
            cboAlternateHost.Items.Add("Host B");
            cboAlternateHost.Items.Add("Host C");
            cboAlternateHost.Items.Add("Host D");
            cboAlternateHost.SelectedItem = "Host C";
            //
            cboHostA.Items.Add("Not Used");
            cboHostA.Items.Add("Paymentech");
            cboHostA.Items.Add("WorldPay");
            cboHostA.Items.Add("Global");
            cboHostA.Items.Add("BlueVend");
            cboHostA.Items.Add("EasyHost");
            cboHostA.SelectedItem = "Paymentech";
            //
            cboHostB.Items.Add("Not Used");
            cboHostB.Items.Add("Paymentech");
            cboHostB.Items.Add("WorldPay");
            cboHostB.Items.Add("Global");
            cboHostB.Items.Add("BlueVend");
            cboHostB.Items.Add("EasyHost");
            cboHostB.SelectedItem = "Not Used";
            //
            cboHostC.Items.Add("Not Used");
            cboHostC.Items.Add("Paymentech");
            cboHostC.Items.Add("WorldPay");
            cboHostC.Items.Add("Global");
            cboHostC.Items.Add("BlueVend");
            cboHostC.Items.Add("EasyHost");
            cboHostC.SelectedItem = "Not Used";
            //
            cboHostD.Items.Add("Not Used");
            cboHostD.Items.Add("Paymentech");
            cboHostD.Items.Add("WorldPay");
            cboHostD.Items.Add("Global");
            cboHostD.Items.Add("BlueVend");
            cboHostD.Items.Add("EasyHost");
            cboHostD.SelectedItem = "Not Used";
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
            Program.UpdateCertificate();
        }

        override protected void OnClosing(CancelEventArgs e)
        {
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
            try
            {
                activeConnection.Dispose();
            }
            catch { }
            activeConnection = null;
            gbLogin.Enabled = true;
            gbNAC.Enabled = false;
            cmdDisconnect.Enabled = false;
            cmdLogOut.Enabled = false;
        }

        private void cmdDisconnect_Click(object sender, EventArgs e)
        {

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
            try
            {
                if (activeConnection != null)
                {
                    try
                    {
                        activeConnection.Dispose();
                    }
                    catch { }
                    activeConnection = null;
                }

                LoginToServer login = new LoginToServer(txtUsername.Text, CStoredCertificate.MakeSecureString(txtPassword.Text),
                    WinSIP2E.Properties.Settings.Default.ServerAddress,
                    WinSIP2E.Properties.Settings.Default.ManuallySetCN ? WinSIP2E.Properties.Settings.Default.ServerCN : WinSIP2E.Properties.Settings.Default.ServerAddress,
                    WinSIP2E.Properties.Settings.Default.ManuallySetPort ? Convert.ToInt32(WinSIP2E.Properties.Settings.Default.ServerPort) : 1102,
                    Program.WinSIP_Cert, Program.WinSIP_TS);

                OperationStatusDialog frm = new OperationStatusDialog();
                frm.operation = login;
                frm.ShowDialog();
                if (login.Status == Operation.CompletionCode.FinishedSuccess)
                {
                    activeConnection = login.ServerConnection;
                    gbLogin.Enabled = false;
                    gbNAC.Enabled = true;
                    cmdLogOut.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occured: \r\n\r\n" + ex.ToString());
            }
        }

        private void cmdManual_Click(object sender, EventArgs e)
        {
            frmManualTerminal frm = new frmManualTerminal();
            frm.Show();
        }
    }

}
