using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinSIP2E.Operations;

namespace WinSIP2E
{
    public partial class frmSettings : Form
    {
        private RequestCertificate CertReq = null;

        public frmSettings()
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


            //auto complete list of states (currently only pertains to US states)
            AutoCompleteStringCollection col = new AutoCompleteStringCollection();
            col.AddRange(MakeStateList());
            txtState.AutoCompleteCustomSource = col;
            txtState.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtState.AutoCompleteSource = AutoCompleteSource.CustomSource;

            //auto complete list of countries:
            AutoCompleteStringCollection colCountry = new AutoCompleteStringCollection();
            colCountry.AddRange(MakeCountryCodeList());
            txtCountry.AutoCompleteCustomSource = colCountry;
            txtCountry.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtCountry.AutoCompleteSource = AutoCompleteSource.CustomSource;     

            //certificate info
            UpdateCertDisplay();

            DisableEnableButtons();            

        }

        private void UpdateCertDisplay()
        {
            if (Program.WinSIP_Cert == null)
                txtCertExpires.Text = "No valid certificate exists";
            else
            {
                DateTime certExpire = DateTime.Parse(Program.WinSIP_Cert.Certificate.GetExpirationDateString());
                if (DateTime.Compare(certExpire, DateTime.Now) < 0)
                    txtCertExpires.Text = "Expired certificate found, expired on: " + Program.WinSIP_Cert.Certificate.GetExpirationDateString();
                else
                    txtCertExpires.Text = "Valid certificate found, expires on: " + Program.WinSIP_Cert.Certificate.GetExpirationDateString();
            }
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
            
            StringBuilder errorStr = new StringBuilder();
            if (txtState.Text.Length < 3)
                errorStr.AppendLine("State must be the full state name (i.e. Minnesota)");

            if (txtCountry.Text.Length != 2)
                errorStr.AppendLine("Country code must be the 2 character abbreviation (i.e. US)");

            int dummy;
            if ( (txtPinCode.Text.Length != CertificateRequestTable.PinCodeLen) || (!int.TryParse(txtPinCode.Text, out dummy)) )
                errorStr.AppendLine("The pin code length must be exactly 6 numeric digits");            

            if (errorStr.Length > 0)
                MessageBox.Show("You have specified improper certificate information: \r\n\r\n" + errorStr.ToString()
                    + "\r\n Please correct this information and try again.", "Improper certificate field information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
            {
                try
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

                        //If a previous request exists, remove it
                        if (CertReq != null)
                            CertReq.RemoveCSR();

                        CertReq = req;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An unexpected error occured: \r\n\r\n" + ex.ToString());
                }
            }

        }

        private void chkOverridePort_CheckedChanged(object sender, EventArgs e)
        {            
            txtServerPort.Enabled = chkOverridePort.Checked;            
        }

        private void cmdLoadCertificate_Click(object sender, EventArgs e)
        {
            try
            {

                DownloadCertificate req = new DownloadCertificate(txtPinCode.Text, txtNewCertID.Text.Replace("-", ""),
                    WinSIP2E.Properties.Settings.Default.ServerAddress,
                    WinSIP2E.Properties.Settings.Default.ManuallySetCN ? WinSIP2E.Properties.Settings.Default.ServerCN : WinSIP2E.Properties.Settings.Default.ServerAddress,
                    WinSIP2E.Properties.Settings.Default.ManuallySetPort ? Convert.ToInt32(WinSIP2E.Properties.Settings.Default.ServerPort) : 1101,
                    Program.WinSIP_TS);


                OperationStatusDialog frm = new OperationStatusDialog();
                frm.operation = req;
                frm.ShowDialog();

                txtStatus.Text = req.StatusMessage;
                if (req.Status == Operation.CompletionCode.FinishedSuccess)
                {
                    WinSIP2E.Properties.Settings.Default.CertificateID = txtNewCertID.Text.Replace("-", "");
                    if (Program.WinSIP_Cert != null)
                    {
                        try
                        {
                            Program.WinSIP_Cert.RemoveTheCert();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Unable to remove the old WinSIP certificate from the certificate store.", "Unable to remove old WinSIP certificate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Program.LogMsg(System.Diagnostics.TraceEventType.Warning, "Unable to remove the old WinSIP certificate from the certificate store. Error: " + ex.ToString());
                        }
                    }

                    Program.UpdateCertificate();
                    UpdateCertDisplay();
                    txtNewCertID.Text = "";
                    cmdRequestCert.Enabled = false;
                    cmdLoadCertificate.Enabled = false;
                    txtPinCode.Enabled = false;

                    //the certificate request has been answered, remove the reference to it
                    CertReq = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occured: \r\n\r\n" + ex.ToString());
            } 
        }

        private string[] MakeCountryCodeList()
        {
            List<string> list = new List<string>();             
            foreach ( CultureInfo ci in CultureInfo.GetCultures ( CultureTypes.SpecificCultures ) )
            {
              RegionInfo ri = null;
                
              try
              {
                ri = new RegionInfo(ci.Name);
                list.Add(ri.TwoLetterISORegionName);
              }
              catch
              {
                continue;
              }
            }
            return list.ToArray();
        }

        private string[] MakeStateList()
        {
            string[] list = {
                    "Alabama",
                    "Alaska",
                    "Arizona",
                    "Arkansas",
                    "California",
                    "Colorado",
                    "Connecticut",
                    "Delaware",
                    "District Of Columbia",
                    "Florida",
                    "Georgia",
                    "Hawaii",
                    "Idaho",
                    "Illinois",
                    "Indiana",
                    "Iowa",
                    "Kansas",
                    "Kentucky",
                    "Louisiana",
                    "Maine",
                    "Maryland",
                    "Massachusetts",
                    "Michigan",
                    "Minnesota",
                    "Mississippi",
                    "Missouri",
                    "Montana",
                    "Nebraska",
                    "Nevada",
                    "New Hampshire",
                    "New Jersey",
                    "New Mexico",
                    "New York",
                    "North Carolina",
                    "North Dakota",
                    "Ohio",
                    "Oklahoma",
                    "Oregon",
                    "Pennsylvania",
                    "Rhode Island",
                    "South Carolina",
                    "South Dakota",
                    "Tennessee",
                    "Texas",
                    "Utah",
                    "Vermont",
                    "Virginia",
                    "Washington",
                    "West Virginia",
                    "Wisconsin",
                    "Wyoming"
            };
            return list;
        }

        private void txtNewCertID_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            
        }

        private void txtPinCode_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void txtPinCode_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }

        private void DisableEnableButtons()
        {
            if ((txtPinCode.Text.Length == CertificateRequestTable.PinCodeLen)
                    && (txtNewCertID.Text.Replace("-", "").Length == CertificateRequestTable.CertificateID_Len))
                cmdLoadCertificate.Enabled = true;
            else
                cmdLoadCertificate.Enabled = false;

            if ((txtPinCode.Text.Length == CertificateRequestTable.PinCodeLen)
                    && (txtState.Text.Length > 3)
                    && (txtCountry.Text.Length >= 2) 
                    && (txtCity.Text.Length >= 3) 
                    && (txtCompany.Text.Length >= 2) )
                cmdRequestCert.Enabled = true;                
            else
                cmdRequestCert.Enabled = false;
                
        }

        private void txtCompany_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }

        private void txtCity_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }

        private void txtState_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }

        private void txtCountry_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }

        private void frmSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CertReq != null)
            {
                DialogResult res = MessageBox.Show("You have created a certificate request but have not yet received a response. Do you want to delete the request?" +
                    "\r\n\r\n\t Yes, if you do not anticipate receiving a response to this request in the future" +
                    "\r\n\t No, if you anticipate receiving a response. In this case, please write down the pin code and certificate ID as you will need to use them to retrieve the response." +
                    "\r\n\t Cancel, to leave this window open.", "Certificate response not yet received", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                    CertReq.RemoveCSR();
                else if (res == DialogResult.Cancel)
                    e.Cancel = true;

            }
        }

        private void txtNewCertID_TextChanged(object sender, EventArgs e)
        {
            DisableEnableButtons();
        }
    }
}
