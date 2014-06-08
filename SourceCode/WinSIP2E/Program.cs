using ForwardLibrary.Communications;
using ForwardLibrary.Crypto;
using ForwardLibrary.Log;
using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinSIP2E
{
    static class Program
    {
        //Look at using this in ForwardLog object...
        public static TraceSource WinSIP_TS = new TraceSource("WinSIP");
        public static CStoredCertificate WinSIP_Cert;
        
        public const int GlobalLogID = 0;

        /// <summary>
        /// Time between pings sent to a default STX ETX connection
        /// </summary>
        public const int ConnectionPingTime = 30;


        /// <summary>
        /// Set to true if we are expecting a disconnect from the server
        /// connection object. (Setting to true prevents a UI message 
        /// from appearing notifying the user that the connection has 
        /// ended).
        /// </summary>
        public static bool bServerDisconnectExpected = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmHome());
        }

        public static void UpdateCertificate()
        {
            if (WinSIP2E.Properties.Settings.Default.CertificateID.Length != CertificateRequestTable.CertificateID_Len)
            {
                //no cert present
                WinSIP_Cert = null;
            }
            else
            {
                try
                {
                    WinSIP_Cert = new CStoredCertificate(StoreLocation.CurrentUser, WinSIP2E.Properties.Settings.Default.CertificateID);
                }
                catch (Exception ex)
                {
                    WinSIP_Cert = null;
                    WinSIP_TS.TraceEvent(TraceEventType.Error, GlobalLogID, "Encountered error upon loading the WinSIP certificate: " + ex.Message);
                }
            }
        }

        static public void LogMsg(TraceEventType type, string msg)
        {
            WinSIP_TS.TraceEvent(type, GlobalLogID, msg);
        }
    }
}
