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
using System.Security;
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
    }

}
