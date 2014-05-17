﻿using ForwardLibrary.WinSIPserver;
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
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Settings_Load(object sender, EventArgs e)
        {
            txtMachineID.Text = (System.Environment.MachineName + ":" + System.Environment.UserName);
            if (txtMachineID.Text.Length > CertificateRequestTable.MachineID_MaxLen)
                txtMachineID.Text = txtMachineID.Text.Substring(0, CertificateRequestTable.MachineID_MaxLen);

            //UpdateServerNameDataBindings(WinSIP2E.Properties.Settings.Default.ManuallySetCN);

            if (WinSIP2E.Properties.Settings.Default.ManuallySetCN)
                txtManualServerName.Enabled = true;
            else
                txtManualServerName.Enabled = false;

            if (WinSIP2E.Properties.Settings.Default.ManuallySetPort)
                txtServerPort.Enabled = true;
            else
                txtServerPort.Enabled = false;
        }

        private void chkOverrideName_CheckedChanged(object sender, EventArgs e)
        {
            //UpdateServerNameDataBindings(chkOverrideName.Checked);
            if (chkOverrideName.Checked == true)
            {
                txtManualServerName.Enabled = true;                
            }
            else
            {
                txtManualServerName.Enabled = false;
            }
        }

        private void txtServerAddress_TextChanged(object sender, EventArgs e)
        {
            
            //if (!WinSIP2E.Properties.Settings.Default.ManuallySetCN)
            //    txtManualServerName.Text = txtServerAddress.Text; //WinSIP2E.Properties.Settings.Default.ServerCN = txtServerAddress.Text; //WinSIP2E.Properties.Settings.Default.ServerAddress;
        }

        private void UpdateServerNameDataBindings(bool ManualOverride)
        {
            /*if (ManualOverride)
            {
                txtManualServerName.DataBindings.Clear();
                txtManualServerName.DataBindings.Add("Text", global::WinSIP2E.Properties.Settings.Default, "ServerCN");
            }
            else
            {
                txtManualServerName.DataBindings.Clear();
                txtManualServerName.DataBindings.Add("Text", global::WinSIP2E.Properties.Settings.Default, "ServerAddress");
            }*/
        }

        private void cmdRequestCert_Click(object sender, EventArgs e)
        {            
             

            RequestCertificate req = new RequestCertificate(txtPinCode.Text, txtMachineID.Text,
                WinSIP2E.Properties.Settings.Default.ServerAddress, 
                WinSIP2E.Properties.Settings.Default.ManuallySetCN ? WinSIP2E.Properties.Settings.Default.ServerCN : WinSIP2E.Properties.Settings.Default.ServerAddress,
                WinSIP2E.Properties.Settings.Default.ManuallySetPort ? Convert.ToInt32(WinSIP2E.Properties.Settings.Default.ServerPort) : 1101, 
                Program.WinSIP_TS);

            req.State = txtState.Text;
            req.Organization = txtCompany.Text;
            req.Locality = txtCity.Text;
            req.Country = txtCountry.Text;

            OperationStatusDialog frm = new OperationStatusDialog();
            frm.operation = req;
            frm.ShowDialog();

            txtStatus.Text = req.StatusMessage;
            if (req.Status == Operation.CompletionCode.FinishedSuccess)
            {
                txtNewCertID.Text = req.CertificateID;
                cmdRequestCert.Enabled = false;
                cmdLoadCertificate.Enabled = true;
                txtPinCode.Enabled = false;
            }

        }

        private void chkOverridePort_CheckedChanged(object sender, EventArgs e)
        {            
            txtServerPort.Enabled = chkOverridePort.Checked;            
        }

        private void cmdLoadCertificate_Click(object sender, EventArgs e)
        {

        }
    }
}
