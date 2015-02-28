using ForwardLibrary.Communications;
using ForwardLibrary.Crypto;
using ForwardLibrary.Log;
using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace WinSIP2E
{
    static class Program
    {
        //Look at using this in LogContainer object...
        public static TraceSource WinSIP_TS = new TraceSource("WinSIP");
        public static CStoredCertificate WinSIP_Cert;
        
        public const int GlobalLogID = 0;

        /// <summary>
        /// Time in seconds between pings sent to a default STX ETX connection
        /// </summary>
        public const int ConnectionPingTime = 30;

        /// <summary>
        /// Allowable idle time in seconds.
        /// 
        /// In regards to the primary connection, if no user interaction has 
        /// taken plance within this time (with WinSIP), the connection will
        /// be terminated. 
        /// In regards to manual mode connections, if no commands are sent 
        /// to the connection within this time, it will be disconnected.
        /// 
        /// Set this value to 0 to disable timeout checking
        /// </summary>
        public static int IdleTimeout = IdleTimeoutDefault;

        /// <summary>
        /// Default value of IdleTimeout
        /// </summary>
        public const int IdleTimeoutDefault = 60 * 5;


        /// <summary>
        /// Set to true if we are expecting a disconnect from the server
        /// connection object. (Setting to true prevents a UI message 
        /// from appearing notifying the user that the connection has 
        /// ended).
        /// </summary>
        public static bool bServerDisconnectExpected = false;

        #region Last user input time
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        /// <summary>
        /// Determine the amount of time since the last user interaction with this program
        /// http://www.pinvoke.net/default.aspx/Structures/LASTINPUTINFO.html
        /// </summary>
        /// <returns>number of seconecs since the last user interaction</returns>
        public static int GetLastInputTime()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint) Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return (int) ((idleTime > 0) ? (idleTime / 1000) : 0);
        }
        #endregion
        

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
