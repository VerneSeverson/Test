using ForwardLibrary.Communications;
using ForwardLibrary.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinSIP2E
{
    static class Program
    {
        //Look at using this in ForwardLog object...
        public static TraceSource WinSIP_TS = new TraceSource("WinSIP");
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Home());
        }
    }
}
