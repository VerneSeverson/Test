using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Communications.STXETX;
using ForwardLibrary.Crypto;
using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            #region protected helper functions common to several operations
            protected delegate void StartDel();

            virtual protected void LogMsg(TraceEventType type, string msg)
            {
                LogTS.TraceEvent(type, LogID, msg);
            }

            virtual protected WinSIPserver ConnectToServerOneWaySSL(string ServerAddress, int ServerPort, string ServerCN)
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
                return new WinSIPserver(connection, LogTS);
            }

            virtual protected WinSIPserver ConnectToServerTwoWaySSL(string ServerAddress, int ServerPort, string ServerCN, X509Certificate2 clientCert)
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
                        localCert = clientCert,
                        server = false
                    }
                };

                StxEtxHandler connection = new StxEtxHandler(cm.ConnectToServer(ServerAddress, ServerPort), true);
                return new WinSIPserver(connection, LogTS);
            }
            #endregion

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

            public override void Start()
            {
                StartDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }

            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }

            /// <summary>
            /// Call this function to remove a certificate signing request from the windows 
            /// certificate store
            /// </summary>
            public void RemoveCSR()
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
                        ServerHandler = ConnectToServerOneWaySSL(ServerAddress, ServerPort, ServerCN);                                

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
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
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
            

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                {                    
                    throw new OperationCanceledException("User canceled the operation");
                }
            }

            private void CloseConnection()
            {
                if (ServerHandler != null)
                    ServerHandler.Dispose();
                ServerHandler = null;
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
            private string CertificateID;
            private string ServerAddress, ServerCN;
            private int ServerPort;
            private WinSIPserver ServerHandler;
            private bool CloseUponCompletion = true;
            private string cert;
            private string cert_signers;
            private CStoredCertificate certObj = null;

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
            public override void Start()
            {
                StartDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }            

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }

            #region constructors
            /// <summary>
            /// Possible exceptions:
            /// ArgumentException
            /// </summary>
            /// <param name="pinCode"></param>
            /// <param name="machineID"></param>
            /// <param name="serverHandler">A server command handler object.</param>
            /// <param name="closeUponCompletion">Optional. Set to true if the command handler should be closed upon operation completion.</param>
            public DownloadCertificate(string pinCode, string certificateID, WinSIPserver serverHandler, bool closeUponCompletion = false, TraceSource LogTS = null)
            {
                SetUpBasicFields(pinCode, certificateID, LogTS);
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
            public DownloadCertificate(string pinCode, string certificateID, string serverAddress, string serverCN, int serverPort, TraceSource LogTS = null)
            {
                SetUpBasicFields(pinCode, certificateID, LogTS);
                this.ServerAddress = serverAddress;
                this.ServerCN = serverCN;
                this.ServerPort = serverPort;            
            }


            #endregion

            #region main working functions
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
                StatusCompletionPercent(20)]
                DownloadCert,
                [StatusOkMsg("Downloading the certificate signers."),
                StatusErrorMsg("Failed to download the certificate signers from the WinSIP server."),
                StatusCompletionPercent(40)]
                DownloadCertSigners,
                [StatusOkMsg("Installing the certificate."),
                StatusErrorMsg("Failed to install the certificate on the local PC."),
                StatusCompletionPercent(60)]
                InstallCert,                
                [StatusOkMsg("Installing the certificate signers."),
                StatusErrorMsg("Failed to install the certificate signers from the WinSIP server."),
                StatusCompletionPercent(80)]
                InstallCertSigners,              
                [StatusOkMsg("Successfully downloaded and installed the signed certificate."),
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
                        ServerHandler = ConnectToServerOneWaySSL(ServerAddress, ServerPort, ServerCN);

                    //2. Download the certificate:                                        
                    CurrentState = WorkState.DownloadCert;
                    ServerHandler.CDCC(PinCode, CertificateID, out cert);                    

                    //3. Download the certificate signers:
                    CurrentState = WorkState.DownloadCertSigners;
                    ServerHandler.ReadCWS(out cert_signers);

                    //4. Install the certificate   
                    CurrentState = WorkState.InstallCert;
                    InstallResponse();
                                        
                    //5. Install the certificate signers
                    CurrentState = WorkState.InstallCertSigners;
                    certObj.SetCertSigners(cert_signers, true);

                    //6. Done
                    CurrentState = WorkState.Finish;

                    //7. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    //I don't think we need to delete the CSR?
                    //but if we have already installed the certificate, we should try to remove it:
                    if (certObj != null)
                    {
                        try { certObj.RemoveTheCert(); }
                        catch { }
                    }
                    _StatusErrorMessage = "User canceled the operation.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {
                    //I don't think we need to delete the CSR?
                    //but if we have already installed the certificate, we should try to remove it:
                    if (certObj != null)
                    {
                        try { certObj.RemoveTheCert(); }
                        catch { }
                    }
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
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

            private void InstallResponse()
            {
                try
                {
                    CCertificateRequest.LoadResponse(cert, StoreLocation.CurrentUser);
                    certObj = new CStoredCertificate(StoreLocation.CurrentUser, CertificateID, false);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("0x80092004"))  //error code for now pending request
                        throw new InvalidOperationException("No request is pending for this certificate ID. \r\nPlease create a new request and try again.", ex);

                    throw ex;
                }
            }
            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)                
                    throw new OperationCanceledException("User canceled the operation");                
            }

            private void CloseConnection()
            {
                if (ServerHandler != null)
                    ServerHandler.Dispose();
                ServerHandler = null;
            }

            private void SetUpBasicFields(string pinCode, string certificateID, TraceSource LogTS)
            {
                if (certificateID.Length != CertificateRequestTable.CertificateID_Len)
                    throw new ArgumentException("Argument is incorrect length.", "certificateID");
                if (pinCode.Length != CertificateRequestTable.PinCodeLen)
                    throw new ArgumentException("Argument is incorrect length.", "pinCode");

                this.PinCode = pinCode;
                this.CertificateID = certificateID;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }
            #endregion
        }

        public class LoginToServer : Operation
        {
            #region Properties
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
                get { return "Connecting to server..."; }
            }
            

            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            public override int StatusPercent { get { return GetStatusCompletionPercent(CurrentState); } }

            private WinSIPserver ServerHandler;

            public WinSIPserver ServerConnection
            {
                get
                {
                    if (ReceivedServerHandler)
                        return ServerHandler;
                    else if (Status == CompletionCode.FinishedSuccess)
                        return ServerHandler;
                    else
                        return null;
                }
            }
            
            private string ServerAddress, ServerCN;
            private int ServerPort;
            private CStoredCertificate ServerCert; 
            
            private bool ReceivedServerHandler = false; //set to true if the ServerHandler was passed in via the constructor (then we won't close the connection upon an operation failure)

            private string UserName;
            private SecureString Password;

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

            #region API
            /// <summary>
            /// Function to start the operation
            /// Will throw InvalidOperation exception if Status != NotStarted
            /// </summary>
            public override void Start()
            {
                StartDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }
            #endregion

            #region constructors
            /// <summary>
            /// Possible exceptions:
            /// ArgumentException
            /// </summary>
            /// <param name="pinCode"></param>
            /// <param name="machineID"></param>
            /// <param name="serverHandler">A server command handler object.</param>
            /// <param name="closeUponCompletion">Optional. Set to true if the command handler should be closed upon operation completion.</param>
            public LoginToServer(string userName, SecureString password, WinSIPserver serverHandler, TraceSource LogTS = null)
            {
                SetUpBasicFields(userName, password, LogTS);
                this.ServerHandler = serverHandler;
                ReceivedServerHandler = true;
            }

            /// <summary>
            /// Possible exceptions:
            /// ArgumentException, ArgumentNullException
            /// </summary>
            /// <param name="pinCode"></param>
            /// <param name="machineID"></param>
            /// <param name="serverAddress">A URL of the server to connect to.</param>            
            /// <param name="serverCN">The common name of the server's certificate.</param>            
            /// <param name="serverPort">The common name of the server's certificate.</param>            
            public LoginToServer(string userName, SecureString password, string serverAddress, string serverCN, int serverPort, CStoredCertificate serverCert, TraceSource LogTS = null)
            {
                if ((userName == null) || (password == null) || (serverAddress == null) || (serverCN == null) || (serverCert == null))
                    throw new ArgumentNullException();
                SetUpBasicFields(userName, password, LogTS);
                this.ServerAddress = serverAddress;
                this.ServerCN = serverCN;
                this.ServerPort = serverPort;
                this.ServerCert = serverCert;
            }
            #endregion

            #region main working functions
            enum WorkState
            {
                [StatusOkMsg("Preparing to connect to the server."),
                StatusErrorMsg("Failed to initiate the server connection."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Connecting to WinSIP server."),
                StatusErrorMsg("Failed to connect to the WinSIP server."),
                StatusCompletionPercent(33)]
                ConnectToServer,
                [StatusOkMsg("Sending user name and password."),
                StatusErrorMsg("Failed to log into server."),
                StatusCompletionPercent(67)]
                LogIntoServer,
                [StatusOkMsg("User login successful."),
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
                        ServerHandler = ConnectToServerTwoWaySSL(ServerAddress, ServerPort, ServerCN, ServerCert.Certificate);

                    //2. Log in to server:                                        
                    CurrentState = WorkState.LogIntoServer;
                    ServerHandler.UDA(UserName, Password);

                    //3. Done
                    CurrentState = WorkState.Finish;
                    
                    //4. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    //I don't think we need to delete the CSR?
                    _StatusErrorMessage = "User canceled the operation.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {
                    //I don't think we need to delete the CSR?
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                    try
                    {
                        //only close the connection upon an error if we created the connection
                        if (!ReceivedServerHandler)
                            CloseConnection();
                    }
                    catch { }   //don't care if this fails.
                }                
            }
            #endregion

            #region private helper functions
            private void SetUpBasicFields(string userName, SecureString password, TraceSource LogTS)
            {
                this.UserName = userName;
                this.Password = password;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                    throw new OperationCanceledException("User canceled the operation");
            }

            private void CloseConnection()
            {
                if (ServerHandler != null)
                    ServerHandler.Dispose();
                ServerHandler = null;
            }
            #endregion
        }

        public class EstabilishPassthroughConnection : Operation
        {

            #region Properties
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
                    {
                        if (CurrentState == WorkState.WaitForUNAC)
                            return GetStatusOkMsg(CurrentState) +
                                "\r\n\r\nUnit last checked in at " +
                                bnac_status.LastCheckinDateTime.ToLocalTime().ToString();
                        else
                            return GetStatusOkMsg(CurrentState);
                    }
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Connecting to UNAC..."; }
            }


            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            public override int StatusPercent { get { return GetStatusCompletionPercent(CurrentState); } }

            private WinSIPserver ServerHandler;

            public WinSIPserver ServerConnection
            {
                get { return ServerHandler;  }
            }

            private string UnitID = null;
            private BNAC_Table.ID_Type ID_Type;
            private BNAC_StateTable.Entry bnac_status;

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

            #region API
            /// <summary>
            /// Function to start the operation
            /// Will throw InvalidOperation exception if Status != NotStarted
            /// </summary>
            public override void Start()
            {
                StartDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }
            #endregion

            #region constructors
            /// <summary>
            /// Possible exceptions:
            /// ArgumentNullException
            /// </summary>
            /// <param name="ID">the ID of the UNAC</param>
            /// <param name="idType">the type of ID</param>
            /// <param name="serverHandler">An active server command handler object.</param>
            /// <param name="closeUponCompletion">Optional. Set to true if the command handler should be closed upon operation completion.</param>
            /// <exception cref="System.ArgumentNullException">Thrown when any argument other then LogTS is null</exception>            
            public EstabilishPassthroughConnection(string ID, BNAC_Table.ID_Type idType, WinSIPserver serverHandler, TraceSource LogTS = null)
            {
                if ((ID == null) || (idType == null) || (serverHandler == null))
                    throw new ArgumentNullException();

                this.ServerHandler = serverHandler;
                this.UnitID = ID;
                this.ID_Type = idType;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            #endregion

            #region main working functions
            enum WorkState
            {
                [StatusOkMsg("Preparing to request connection."),
                StatusErrorMsg("Failed to request a connection."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Sending connection request to the server."),
                StatusErrorMsg("Failed to request a connection."),
                StatusCompletionPercent(33)]
                RequestPassthrough,
                [StatusOkMsg("Waiting for UNAC to connect."),
                StatusErrorMsg("An error occured while waitiing for the UNAC to connect."),
                StatusCompletionPercent(67)]
                WaitForUNAC,
                [StatusOkMsg("Connection to UNAC established."),
                StatusErrorMsg("Should not see this message."),
                StatusCompletionPercent(100)]
                Finish
            }

            private void DoTheWork()
            {

                try
                {
                    //1. Request connection to UNAC  
                    CurrentState = WorkState.RequestPassthrough;
                    ServerHandler.CONB(UnitID, ID_Type, out bnac_status);

                    //2. Wait for the UNAC to connect and indicate it is ready:                                        
                    CurrentState = WorkState.WaitForUNAC;
                    WaitForBNR();

                    //3. Done
                    CurrentState = WorkState.Finish;

                    //4. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {                    
                    _StatusErrorMessage = "User canceled the operation.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {                    
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                }
            }
            #endregion


            #region private helper functions
            

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                    throw new OperationCanceledException("User canceled the operation");
            }

            private void WaitForBNR()
            {
                StxEtxHandler handler = ServerHandler.StxEtxPeer;

                //Manually poll the received responses, waiting for a BNR
                while (true)
                {
                    if (handler.CommContext.bConnected == false)
                        throw new InvalidOperationException("The server connection is closed");

                    CheckUserCancel();

                    string rcvd = null;
                    bool gotRsp = handler.ReceiveData(out rcvd, 500); //limit the thread to blocking for 500ms at a time (to allow the user to cancel)
                    if (rcvd != null)
                        if (rcvd.Trim() == "BNR")
                            break;                    
                }
            }
            #endregion
        }

        public class SendScriptFile : Operation
        {

            #region Properties
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
                get { return true; }
            }

            private string _StatusErrorMessage = null;
            public override string StatusMessage
            {
                get
                {
                    if (_StatusErrorMessage == null)
                    {
                        if (CurrentState == WorkState.SendingScript)
                            return "Sending line " + CurrentLine + " of " + ScriptLines.Count() + ".";
                        else
                            return GetStatusOkMsg(CurrentState);
                    }
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Sending script file " + Path.GetFileNameWithoutExtension(FileName); }
            }


            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }
            
            public override int StatusPercent { 
                get 
                {
                    if (PendingUserMessage.Length > 0)
                    {
                        DialogResult res = MessageBox.Show(PendingUserMessage, "Attention", MessageBoxButtons.OKCancel);
                        if (res == DialogResult.Cancel)
                            Cancel();
                        else
                            PendingUserMessage = "";
                    }

                    if (CurrentState != WorkState.SendingScript)
                        return GetStatusCompletionPercent(CurrentState);
                    else
                        return (CurrentLine * 100) / ScriptLines.Count();
                } 
            }

            private StxEtxHandler Handler;

            public StxEtxHandler Connection
            {
                get { return Handler; }
            }

            private string FileName;
            private IEnumerable<string> ScriptLines;
            private int CurrentLine = 0;
            private string PendingUserMessage = "";

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


            #region API
            /// <summary>
            /// Function to start the operation
            /// Will throw InvalidOperation exception if Status != NotStarted
            /// </summary>
            public override void Start()
            {
                StartDel caller = this.DoTheWork;
                caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
            }

            /// <summary>
            /// Function to cancel the operation
            /// Will throw InvalidOperation exception if Status != InProgress
            /// OR if AllowCancel == false
            /// </summary>
            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }
            #endregion

            #region constructors
            /// <summary>
            /// Possible exceptions:
            /// ArgumentNullException
            /// </summary>
            /// <param name="handler">The active command handler object.</param>
            /// <param name="fileName">The name of the script file to send.</param>                        
            /// <exception cref="System.ArgumentNullException">Thrown when any argument other then LogTS is null</exception>            
            public SendScriptFile(StxEtxHandler handler, string fileName, TraceSource LogTS = null)
            {
                if ( (handler == null) || (fileName == null) )
                    throw new ArgumentNullException();

                this.Handler = handler;
                this.FileName = fileName;                

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            /// <summary>
            /// The user will be prompted to locate the script file to send.            
            /// </summary>
            /// <param name="handler">An active StxEtxHandler object.</param>            
            /// <exception cref="System.ArgumentNullException">Thrown when any argument other then LogTS is null</exception>            
            /// /// <exception cref="System.OperationCanceledException">Thrown when the user presses cancel when prompted to select a script file</exception>      
            public SendScriptFile(StxEtxHandler handler, TraceSource LogTS = null)
            {
                if (handler == null)
                    throw new ArgumentNullException();

                this.Handler = handler;
                if (!DetermineScriptFileName(out this.FileName))
                    throw new OperationCanceledException("Sending the script file was canceled by the user.");

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            #endregion


            #region main working functions
            enum WorkState
            {
                [StatusOkMsg("Preparing to send script."),
                StatusErrorMsg("Failed to prepare script."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Validating script file."),
                StatusErrorMsg("Failed to validate the script file."),
                StatusCompletionPercent(1)]
                LoadingScript,   
                [StatusOkMsg("Sending script..."),
                StatusErrorMsg("Failed to send script."),
                StatusCompletionPercent(2)]
                SendingScript,                
                [StatusOkMsg("Script file successfully sent."),
                StatusErrorMsg("Should not see this message."),
                StatusCompletionPercent(100)]
                Finish
            }

            private void DoTheWork()
            {

                try
                {
                    string tempStr;

                    //0. Empty receive buffer
                    while (Handler.ReceiveData(out tempStr, 100))
                        ;

                    //1. Read in the script file
                    CurrentState = WorkState.LoadingScript;
                    ScriptLines = File.ReadLines(FileName);

                    //2. Send the script
                    CurrentState = WorkState.SendingScript;
                    foreach (string line in ScriptLines)
                    {
                        CurrentLine++;
                        ProcessScriptLine(line);

                        //If the user has to OK something
                        while (PendingUserMessage.Length > 0)
                        {
                            Thread.Sleep(100);
                            CheckUserCancel();                            
                        }
                    }
                                        
                    //3. Done
                    CurrentState = WorkState.Finish;

                    //4. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    _StatusErrorMessage = "User canceled the operation.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                }
            }
            #endregion


            #region Private helper functions
            /// <summary>
            /// Call this function to locate the script file
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            private bool DetermineScriptFileName(out string fileName)
            {
                OpenFileDialog fDialog = new OpenFileDialog();
                fDialog.Title = "Select script file to send";
                fDialog.InitialDirectory = Properties.Settings.Default.LastManualBrowseFolder;
                fDialog.CheckFileExists = true;
                fDialog.CheckPathExists = true;
                fDialog.Multiselect = false;
                fDialog.Filter = "Script Files (.cmd)|*.cmd";
                fDialog.FilterIndex = 1;

                if (fDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = fDialog.FileName;
                    Properties.Settings.Default.LastManualBrowseFolder = Path.GetDirectoryName(fDialog.FileName);
                    return true;
                }
                else
                {
                    fileName = null;
                    return false;
                }
            }

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                    throw new OperationCanceledException("User canceled the operation");
            }

            private void ProcessScriptLine(string scriptLine)
            {
                //1. Remove comment
                scriptLine = RemoveComment(scriptLine);
                
                //2. Remove whitespace
                scriptLine = scriptLine.Trim();

                //3. Parse it
                try
                {
                    ParseScriptLine(scriptLine);
                }
                catch (ResponseErrorCodeException ex)
                {
                    PendingUserMessage = "Received an error message. Press OK to send the remaining script lines.";
                    LogMsg(TraceEventType.Information, PendingUserMessage);
                }
                
            }

            private void ParseScriptLine(string scriptLine)
            {
                switch (scriptLine.Substring(0, 1))
                {
                    case "#":
                        //handle both # and ##
                        string workingLine = scriptLine.Substring(1);
                        bool allowAbortDelay = false;
                        if (workingLine.Substring(0, 1) == "#")
                        {
                            allowAbortDelay = true;
                            workingLine = workingLine.Substring(1);
                        }
                        int delay = int.Parse(workingLine);

                        LogMsg(TraceEventType.Verbose, "Delay " + delay + " second(s).");

                        if (!allowAbortDelay)
                            Thread.Sleep(delay * 100);
                        else
                            CheckForResponse(delay * 100);

                        break;

                    case "!":
                        PendingUserMessage = scriptLine.Substring(1);
                        break;


                    default:
                        CheckForResponse(0);
                        Handler.SendCommand(scriptLine);
                        break;
                }
            }

            private string RemoveComment(string scriptLine)
            {
                int comment = scriptLine.IndexOf("//");
                if (comment == 0)
                    return "";
                else if (comment > 0)
                    return scriptLine.Substring(0, comment);
                else
                    return scriptLine;
                
            }

            /// <summary>
            /// Get all responses. 
            /// Throws an exception if an error message is found.
            /// </summary>
            /// <param name="max_block">The maximum time (in ms) to block</param>
            /// <returns>An array of responses (null if no responses)</returns>
            /// <exception cref="System.ResponseErrorCodeException">Thrown when an error code is received.</exception>            
            private string[] CheckForResponse(int max_block)
            {
                List<string> resps = new List<string>();
                string tempStr;
                while (Handler.ReceiveData(out tempStr, max_block))
                {
                    if ((tempStr != null) && (tempStr.Length > 0))
                    {                        
                        resps.Add(tempStr);
                    }
                }

                foreach(string str in resps)
                    if (tempStr.StartsWith("ER"))
                        throw new ResponseErrorCodeException("Received an error response.", "", resps);

                if (resps.Count > 0)
                    return resps.ToArray();
                else
                    return null;
            }
            
            #endregion
        }
    }

}
