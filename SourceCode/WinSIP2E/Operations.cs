using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Communications.STXETX;
using ForwardLibrary.Crypto;
using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WinSIP2E
{
    namespace Operations
    {


        abstract public class Operation
        {
            

            protected object SyncObject = new object();

            protected TraceSource LogTS;            

            public int LogID = 1000;

            /// <summary>
            /// Recommended interval to check Status in
            /// milliseconds
            /// </summary>
            public abstract int RefreshInterval { get; }

            /// <summary>
            /// Whether or not the operation can be canceled
            /// </summary>
            public abstract bool AllowCancel { get; }

            /// <summary>
            /// If this is set to true, it is requested that the user acknowledge
            /// the final status message. (i.e. OperationStatusDialog should 
            /// make the user press OK to close the dialog box)
            /// </summary>
            public abstract bool RequireUserOK { get; }

            /// <summary>
            /// The current status of the operation (user-readable)
            /// </summary>
            public abstract string StatusMessage { get; }

            /// <summary>
            /// This is a short string describing the operation. 
            /// It is used as the form text for OperationStatusDialog.
            /// </summary>
            public abstract string SubjectLine { get; }

            public enum CompletionCode
            {
                NotStarted,
                InProgress,
                FinishedSuccess,
                FinishedError,
                UserCancelReq,
                UserCancelFinish
            }

            public abstract CompletionCode Status { get; }

            //returns the percent completion (0 to 100)
            public abstract int StatusPercent { get; }


            /// <summary>
            /// Function to start the operation
            /// Will throw InvalidOperation exception if Status != NotStarted
            /// </summary>
            public abstract void Start();

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public abstract void Cancel();

            virtual protected void LogMsg(TraceEventType type, string msg)
            {
                LogTS.TraceEvent(type, LogID, msg);
            }


            #region classes for enum
            protected class StatusCompletionPercent : System.Attribute
            {
                private int _value;

                public StatusCompletionPercent(int value)
                {
                    _value = value;
                }

                public int Value
                {
                    get {return _value; }
                }
            }
            protected class StatusOkMsg : System.Attribute
            {
                private string _value;

                public StatusOkMsg(string value)
                {
                    _value = value;
                }

                public string Value
                {
                    get {return _value; }
                }
            }
            protected class StatusErrorMsg : System.Attribute
            {
                private string _value;

                public StatusErrorMsg(string value)
                {
                    _value = value;
                }

                public string Value
                {
                    get {return _value; }
                }
            }
            protected static int GetStatusCompletionPercent(Enum value)
            {
                int output = 0;
                Type type = value.GetType();
                FieldInfo fi = type.GetField(value.ToString());
                StatusCompletionPercent[] attrs = 
                    fi.GetCustomAttributes(typeof(StatusCompletionPercent),
                                   false) as StatusCompletionPercent[];
                if(attrs.Length > 0)
                    output = attrs[0].Value;

                return output;
            }
            protected static string GetStatusOkMsg(Enum value)
            {
                string output = null;
                Type type = value.GetType();
                FieldInfo fi = type.GetField(value.ToString());
                StatusOkMsg[] attrs = 
                    fi.GetCustomAttributes(typeof(StatusOkMsg),
                                   false) as StatusOkMsg[];
                if(attrs.Length > 0)
                    output = attrs[0].Value;

                return output;
            }
            protected static string GetStatusErrorMsg(Enum value)
            {
                string output = null;
                Type type = value.GetType();
                FieldInfo fi = type.GetField(value.ToString());
                StatusErrorMsg[] attrs = 
                    fi.GetCustomAttributes(typeof(StatusErrorMsg),
                                   false) as StatusErrorMsg[];
                if(attrs.Length > 0)
                    output = attrs[0].Value;

                return output;
            }
            #endregion

        }

        public class RequestCertificate : Operation
        {
            #region properties
            public override int RefreshInterval
            {
                get { return 50; }
            }
            
            public override bool AllowCancel { get { return true; } }

            public override bool RequireUserOK
            {
                get
                {
                    if (Status != CompletionCode.FinishedError)
                        return false;
                    else
                        return true;
                }
            }
            
            private string _StatusErrorMessage = null;            
            public override string StatusMessage
            {
                get { 
                    if (_StatusErrorMessage == null)
                        return GetStatusOkMsg(CurrentState); 
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Generating a certificate signing request..."; }
            }

            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }
           
            public override int StatusPercent { get { return GetStatusCompletionPercent(CurrentState); } }

            private string _CertificateID;
            public string CertificateID
            {
                get { return _CertificateID; }
            }

            //Certificate fields
            public string Country = "US";
            public string State = "Minnesota";
            public string Locality = "Eden Prairie";
            public string Organization = "Forward Pay Systems, Inc.";
            public string OrganizationalUnit = "WinSIP";

            //private internal properties
            private string PinCode;
            private string MachineID;
            private string ServerAddress, ServerCN;
            private int ServerPort;
            private WinSIPserver ServerHandler;
            private bool CloseUponCompletion = true;
            

            private string CommonName = null;
            private string CSR = null;
            private WorkState _CurrentState = WorkState.Idle;
            private WorkState CurrentState 
            {
                get { return _CurrentState;}
                set 
                {
                    CheckUserCancel();
                    _CurrentState = value;                    
                    LogMsg(TraceEventType.Verbose, StatusMessage);
                }
            }
            #endregion

            #region API

            /// <summary>
            /// Possible exceptions:
            /// ArgumentException
            /// </summary>
            /// <param name="pinCode"></param>
            /// <param name="machineID"></param>
            /// <param name="serverHandler">A server command handler object.</param>
            /// <param name="closeUponCompletion">Optional. Set to true if the command handler should be closed upon operation completion.</param>
            public RequestCertificate(string pinCode, string machineID, WinSIPserver serverHandler, bool closeUponCompletion = false, TraceSource LogTS = null)
            {
                SetUpBasicFields(pinCode, machineID, LogTS);
                this.ServerHandler = serverHandler;
                CloseUponCompletion = closeUponCompletion;
            }

            /// <summary>
            /// Possible exceptions:
            /// ArgumentException
            /// </summary>
            /// <param name="pinCode"></param>
            /// <param name="machineID"></param>
            /// <param name="serverAddress">A URL of the server to connect to.</param>            
            /// <param name="serverCN">The common name of the server's certificate.</param>            
            /// <param name="serverPort">The common name of the server's certificate.</param>            
            public RequestCertificate(string pinCode, string machineID, string serverAddress, string serverCN, int serverPort, TraceSource LogTS = null)
            {
                SetUpBasicFields(pinCode, machineID, LogTS);
                this.ServerAddress = serverAddress;
                this.ServerCN = serverCN;
                this.ServerPort = serverPort;            
            }

            private delegate void VoidDel();

            public override void Start()
            {                
                VoidDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }

            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }

            

            #endregion

            #region Main helper functions

            enum WorkState
            {
                [StatusOkMsg("Preparing to start certificate generation operation."),
                StatusErrorMsg("Failed to start certificate generation operation."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Connecting to WinSIP server."),
                StatusErrorMsg("Failed to connect to the WinSIP server."),
                StatusCompletionPercent(1)]
                ConnectToServer,
                [StatusOkMsg("Obtaining a certificate ID."),
                StatusErrorMsg("Failed to obtain a certificate ID from the WinSIP server."),
                StatusCompletionPercent(25)]
                ObtainCertID,
                [StatusOkMsg("Generating a certificate signing request."),
                StatusErrorMsg("Failed to generate a certificate signing request on the local PC."),
                StatusCompletionPercent(50)]
                GenerateCSR,
                [StatusOkMsg("Uploading the certificate signing request."),
                StatusErrorMsg("Failed to upload the certificate signing request to the WinSIP server."),
                StatusCompletionPercent(75)]
                UploadCSR,
                [StatusOkMsg("Successfully generated and uploaded the request. The certificate must now be signed by a Forward Pay employee. \r\nPlease present the Certificate ID to a Forward Pay employee and follow their instructions to retrieve the signed certificate."),
                StatusErrorMsg("Should not see this message."),
                StatusCompletionPercent(100)]
                Finish
            }

            private void DoTheWork()
            {
                
                try
                {
                    //1. Connect to server if not already connected:   
                    CurrentState = WorkState.ConnectToServer;     
                    if (ServerHandler == null)                                                               
                        ConnectToServer();                                        

                    //2. Obtain a certificate ID:                                        
                    CurrentState = WorkState.ObtainCertID;                                        
                    ServerHandler.CID(PinCode, MachineID, out _CertificateID);                    

                    //3. Create a new certificate   
                    CurrentState = WorkState.GenerateCSR;
                    CSR = GenerateCertificateRequest();                    

                    //4. Upload the certificate request
                    CurrentState = WorkState.UploadCSR;
                    ServerHandler.CCSR(CSR);

                    //5. Done
                    CurrentState = WorkState.Finish;

                    //6. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    RemoveCSR(); //clean up in here: delete the CSR from the PC if created
                    _StatusErrorMessage = "User canceled the operation.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {
                    RemoveCSR(); //clean up in here: delete the CSR from the PC if created
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " Error: " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                }
                finally
                {
                    try
                    {
                        if (CloseUponCompletion)
                            CloseConnection();
                    }
                    catch { }   //don't care if this fails.
                }
            }

            #endregion

            #region private helper functions
            private void RemoveCSR()
            {
                X509Store store = null;
                if (CSR != null)
                {
                    try
                    {
                        bool deleteSuccess = false;
                        store = new X509Store("REQUEST", StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);
                        foreach (X509Certificate2 cert in store.Certificates)
                        {
                            if (cert.GetNameInfo(X509NameType.SimpleName, false) ==
                                CommonName)
                            {
                                store.Remove(cert);
                                deleteSuccess = true;
                                break;
                            }
                        }

                        if (!deleteSuccess)
                            LogMsg(TraceEventType.Warning, "Unable to delete the generated certificate request from the store because it could not be found. Expecting to find a certificate with this common name: " + CommonName);
                    }
                    catch (Exception e)
                    {
                        LogMsg(TraceEventType.Warning, "Failed to delete the generated certificate request from the store. Error: " + e.ToString());
                    }
                    finally
                    {
                        if (store != null)
                        {
                            try { store.Close(); }
                            catch { }
                        }
                    }
                }
            }

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                {                    
                    throw new OperationCanceledException("User canceled the operation");
                }
            }

            private void CloseConnection()
            {
                ServerHandler.Dispose();
            }

            private string GenerateCertificateRequest()
            {
                CommonName = "PIN" + PinCode + "-MID" + MachineID + "-CID" + CertificateID;

                CCertificateRequest req = new CCertificateRequest
                {
                    CommonName = this.CommonName,
                    Country = this.Country,
                    State = this.State,
                    Locality = this.Locality,
                    Organization = this.Organization,
                    OrganizationalUnit = this.OrganizationalUnit
                };

                return req.GenerateRequest();
            }

            private void ConnectToServer()
            {                
                CommLogMessages CommMsgs = new CommLogMessages
                {
                    msgNewTCP_Client = "CONNECTED " + ServerAddress + ":" + ServerPort.ToString(),
                    msgTCP_DisconnectWithReason = "DISCONNECTED",
                    msgSuppressTCP_DisconnectReason = true
                };

                TCPconnManager cm = new TCPconnManager
                {
                    LogTrace = LogTS,
                    logMsgs = CommMsgs,
                    sslSettings = new SSL_Settings
                    {
                        peerName = ServerCN,
                        protocolsAllowed = SslProtocols.Tls12,
                        server = false
                    }
                };

                StxEtxHandler connection = new StxEtxHandler(cm.ConnectToServer(ServerAddress, ServerPort), true);
                ServerHandler = new WinSIPserver(connection, LogTS);
            }


            private void SetUpBasicFields(string pinCode, string machineID, TraceSource LogTS)
            {
                if (machineID.Length > CertificateRequestTable.MachineID_MaxLen)
                    throw new ArgumentException("Argument is too long.", "machineID");
                if (pinCode.Length != CertificateRequestTable.PinCodeLen)
                    throw new ArgumentException("Argument is incorrect length.", "pinCode");

                this.PinCode = pinCode;
                this.MachineID = machineID;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            #endregion 

        }

        public class DownloadCertificate : Operation
        {
            #region properties
            /// <summary>
            /// Recommended interval to check Status in
            /// milliseconds
            /// </summary>
            public override int RefreshInterval { get { return 100; } }

            /// <summary>
            /// Whether or not the operation can be canceled
            /// </summary>
            public override bool AllowCancel { get { return true; } }

            /// <summary>
            /// If this is set to true, it is requested that the user acknowledge
            /// the final status message. (i.e. OperationStatusDialog should 
            /// make the user press OK to close the dialog box)
            /// </summary>
            public override bool RequireUserOK
            {
                get
                {
                    if (Status != CompletionCode.FinishedError)
                        return false;
                    else
                        return true;
                }
            }

            private string _StatusErrorMessage = null;
            public override string StatusMessage
            {
                get
                {
                    if (_StatusErrorMessage == null)
                        return GetStatusOkMsg(CurrentState);
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Downloading signed certificate..."; }
            }

            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            public override int StatusPercent { get { return GetStatusCompletionPercent(CurrentState); } }


            private string PinCode;            
            private string ServerAddress, ServerCN;
            private int ServerPort;
            private WinSIPserver ServerHandler;
            private bool CloseUponCompletion = true;

            private WorkState _CurrentState = WorkState.Idle;
            private WorkState CurrentState
            {
                get { return _CurrentState; }
                set
                {
                    CheckUserCancel();
                    _CurrentState = value;
                    LogMsg(TraceEventType.Verbose, StatusMessage);
                }
            }

            #endregion
            /// <summary>
            /// Function to start the operation
            /// Will throw InvalidOperation exception if Status != NotStarted
            /// </summary>
            public abstract void Start();

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public abstract void Cancel();



            enum WorkState
            {
                [StatusOkMsg("Preparing to start downloading the signed certificate."),
                StatusErrorMsg("Failed to start downloading the signed certificate."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Connecting to WinSIP server."),
                StatusErrorMsg("Failed to connect to the WinSIP server."),
                StatusCompletionPercent(1)]
                ConnectToServer,
                [StatusOkMsg("Downloading the certificate."),
                StatusErrorMsg("Failed to download the certificate from the WinSIP server."),
                StatusCompletionPercent(33)]
                DownloadCert,
                [StatusOkMsg("Installing the certificate."),
                StatusErrorMsg("Failed to install the certificate on the local PC."),
                StatusCompletionPercent(67)]
                InstallCert,                
                [StatusOkMsg("Successfully downloaded and installed the signed certificate."),
                StatusErrorMsg("Should not see this message."),
                StatusCompletionPercent(100)]
                Finish
            }

            #region private helper functions
            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                {
                    throw new OperationCanceledException("User canceled the operation");
                }
            }

            private void CloseConnection()
            {
                ServerHandler.Dispose();
            }
            #endregion
        }
    }

}
