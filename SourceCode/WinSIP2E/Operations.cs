﻿using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Communications.STXETX;
using ForwardLibrary.Crypto;
using ForwardLibrary.Default;
using ForwardLibrary.Flash.LPC2400;
using ForwardLibrary.Flash.LPC2400.Forward;
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

                IProtocolHandler connection = new StxEtxHandler(cm.ConnectToServer(ServerAddress, ServerPort), true);
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

                IProtocolHandler connection = new StxEtxHandler(cm.ConnectToServer(ServerAddress, ServerPort), true);
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
                if ((ID == null) || (serverHandler == null))
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
                    if (bnac_status.PendingRequest == BNAC_StateTable.BNAC_Status.RDNS_REQUEST)
                        throw new InvalidOperationException("UNAC is currently busy with another connection. Try again later.");

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
                IProtocolHandler handler = ServerHandler.ProtocolHandler;

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
                            try
                            {
                                return "Sending command " + CurrentLine + " of " + ScriptLines.Count() + ".";
                            }
                            catch { return "Sending commands..."; }
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

                    if (CurrentState != WorkState.SendingScript)
                        return GetStatusCompletionPercent(CurrentState);
                    else
                        try
                        {
                            return (CurrentLine * 100) / ScriptLines.Count();
                        }
                        catch
                        { return 0; }
                } 
            }

            private IProtocolHandler Handler;

            public IProtocolHandler Connection
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
                    if (_CurrentState == WorkState.SendingScript)
                        LogMsg(TraceEventType.Verbose, "Sending script file commands...");
                    else
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
                _Status = CompletionCode.InProgress;
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
            public SendScriptFile(IProtocolHandler handler, string fileName, TraceSource LogTS = null)
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
            public SendScriptFile(IProtocolHandler handler, TraceSource LogTS = null)
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
                    ScriptLines = File.ReadAllLines(FileName);

                    //2. Send the script
                    CurrentState = WorkState.SendingScript;
                    foreach (string line in ScriptLines)
                    {
                        CurrentLine++;

                        //are we still connected?
                        if (!Handler.CommContext.bConnected)
                            throw new UnresponsiveConnectionException("The connection closed.", "");

                        ProcessScriptLine(line);

                        //If the user has to OK something
                        if (PendingUserMessage.Length > 0)
                        {
                            DialogResult res = MessageBox.Show(PendingUserMessage, "Attention", MessageBoxButtons.OKCancel);
                            if (res == DialogResult.Cancel)
                                Cancel();
                            else
                                PendingUserMessage = "";
                        }

                        //see if the user has canceled the operation
                        CheckUserCancel();
                    }
                                        
                    //3. Done
                    CurrentState = WorkState.Finish;

                    //4. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException)
                {
                    _StatusErrorMessage = "User canceled sending the script file.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, _StatusErrorMessage);
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
                    if (scriptLine.Length > 0)
                        ParseScriptLine(scriptLine);
                }
                catch (ResponseErrorCodeException)
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

                        LogMsg(TraceEventType.Verbose, "Delay " + delay + " second" + ((delay > 0) ? "s" : "") + ".");

                        if (!allowAbortDelay)
                            Thread.Sleep(delay * 1000);
                        else
                            CheckForResponse(delay * 1000);

                        break;

                    case "!":
                        PendingUserMessage = scriptLine.Substring(1);
                        break;


                    default:
                        CheckForResponse(0);
                        if (!Handler.SendCommand(scriptLine))
                            PendingUserMessage = "Failed to send command: " + scriptLine +
                                "\r\n\r\n Press OK to continue.";
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

        public class IdleTimeout : Operation
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
                get {
                    if (Status == CompletionCode.UserCancelFinish)
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
                        if (Status == CompletionCode.InProgress)
                            return string.Format(InProgressMsg, (int)((DisconnectAt - DateTime.Now).TotalSeconds)); //InProgressMsg + " in " + (int) ((DisconnectAt - DateTime.Now).TotalSeconds) + " seconds. Press cancel to remain connected.";
                        else if (Status == CompletionCode.FinishedSuccess)
                            return ConnectionEndMessage;
                        else
                            return "";
                    }
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Idle Timeout"; }
            }


            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            public override int StatusPercent
            {
                get
                {
                    if (DateTime.Compare(DateTime.Now, DisconnectAt) > 0)
                        return 100;
                    else
                    {
                        TimeSpan TotalTime = DisconnectAt - CreatedAt;
                        TimeSpan RemainingTime = DisconnectAt - DateTime.Now;
                        return ((int)(RemainingTime.TotalSeconds * 100 / (TotalTime.TotalSeconds)));
                    }
                }
            }

            
            private DateTime DisconnectAt;
            private DateTime CreatedAt = DateTime.Now;
            
            /// <summary>
            /// The message that is displayed while waiting for the timeout. It must contain the
            /// substring "{0}" where the remaining seconds should appear. Default value:
            /// "WinSIP has been idle for several minutes. The connection will be terminated in {0} seconds. Press cancel to remain connected."
            /// </summary>
            public string InProgressMsg = "WinSIP has been idle for several minutes. The connection will be terminated in {0} seconds. Press cancel to remain connected.";

            /// <summary>
            /// The message that is displayed when the connection has timed out. Default value:
            /// "WinSIP has timed out. The connection to the server has ended."
            /// </summary>
            public string ConnectionEndMessage = "WinSIP has timed out. The connection to the server has ended.";

            /// <summary>
            /// The function to call when the timeout event occurs
            /// </summary>
            private VoidDel TimedOutFunction;
            //private ClientContext ContextToDisconnect;
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
                _Status = CompletionCode.InProgress;
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
            public IdleTimeout(DateTime disconnectAt, VoidDel timedOutFunction, TraceSource LogTS = null)
            {
                if ((disconnectAt == null) || (timedOutFunction == null))
                    throw new ArgumentNullException();

                this.DisconnectAt = disconnectAt;
                this.TimedOutFunction = timedOutFunction;                

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            #endregion


            #region main working functions            
            private void DoTheWork()
            {

                try
                {                    

                    while (DateTime.Compare(DateTime.Now, DisconnectAt) < 0)
                    {
                        CheckUserCancel();
                        Thread.Sleep(100);
                    }

                    //time's up and no cancel -- so let's call the timeout function
                    TimedOutFunction();

                    //Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException)
                {
                    _StatusErrorMessage = "User canceled sending the disconnect."; //this message won't appear because the dialog box will close
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, _StatusErrorMessage);
                }
                catch (Exception ex)
                {
                    _StatusErrorMessage = "Unexpected exception occured: " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                }
            }
            #endregion

            private void CheckUserCancel()
            {
                if (Status == CompletionCode.UserCancelReq)
                    throw new OperationCanceledException("User canceled the operation");
            }
        }


        public class FlashUNAC : Operation
        {
            public class FlashUNACinfo
            {
                /// <summary>
                /// WinSIP server username
                /// </summary>
                public string userName;

                /// <summary>
                /// WinSIP server password
                /// </summary>
                public SecureString password;

                /// <summary>
                /// WinSIP server address
                /// </summary>
                public string serverAddress;

                /// <summary>
                /// WinSIP server common name (on certificate)
                /// </summary>
                public string serverCN;

                private int _serverPort = 0;
                /// <summary>
                /// Port to use for connecting to the WinSIP server
                /// </summary>
                public int serverPort
                {
                    get { return _serverPort; }
                    set { _serverPort = value; }
                }

                /// <summary>
                /// WinSIP's public certificate (for connecting to the WinSIP server)
                /// </summary>
                public CStoredCertificate serverCert;

                /// <summary>
                /// UNAC ID
                /// </summary>
                public string UNAC_ID;

                private BNAC_Table.ID_Type _UNAC_IDType = BNAC_Table.ID_Type.Index;
                /// <summary>
                /// The UNAC ID's type
                /// </summary>
                public BNAC_Table.ID_Type UNAC_IDType
                {
                    get { return _UNAC_IDType; }
                    set { _UNAC_IDType = value; }
                }
            }
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
                        if ( (_CurrentState == WorkState.ReconnectToUNAC) && (ReconnectOpperation != null) )
                            return GetStatusOkMsg(CurrentState) + "\r\n" + ReconnectOpperation.StatusMessage;
                        else if (_CurrentState == WorkState.FlashInProgress)
                            return GetStatusOkMsg(CurrentState) + " Currently flashing sector " + CurrentSector.ToString() + ".";
                        else
                            return GetStatusOkMsg(CurrentState);
                    }
                    else
                        return _StatusErrorMessage;
                }
            }

            public override string SubjectLine
            {
                get { return "Flashing the UNAC..."; }
            }


            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            public override int StatusPercent { 
                get 
                {
                    if (_CurrentState != WorkState.FlashInProgress)
                        return GetStatusCompletionPercent(CurrentState);
                    else
                    {
                        if (SectorsToFlash == 0)
                            return 0;
                        else
                        {
                            double percent = 100.0*((double)SectorsFlashed) / ((double)SectorsToFlash);
                            return (int)percent;
                        }
                    }
                } 
            }

            private WinSIPserver ServerHandler;
            private FlashUNACinfo BasicInfo;            

            Operation ReconnectOpperation = null;

            /// <summary>
            /// The RAM address where flash data should be stored before writing to flash
            /// </summary>
            public uint DeviceRAMaddr = 0x10000200;  

            /// <summary>
            /// The unlock code sent ot the device to unlock the flash commands
            /// </summary>
            public uint UnlockCode = 23130;

            private FPS_ISP_CommandHandler ISP_CommandHandler;
            private FPS_4MBExtFlash_LPC2400 FlashFile;            
            private string FileName;
            private int CurrentSector = 0;
            private int SectorsFlashed = 0;
            private int SectorsToFlash = 0;            

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
                _Status = CompletionCode.InProgress;
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
            public FlashUNAC(WinSIPserver handler, string fileName, FlashUNACinfo info, TraceSource LogTS = null)
            {
                if ( (handler == null) || (fileName == null) )
                    throw new ArgumentNullException();
                
                this.ServerHandler = handler;
                SetUpBasicFields(info, LogTS);                
                this.FileName = fileName;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

            /// <summary>
            /// The user will be prompted to locate the flash file to send.            
            /// </summary>
            /// <param name="handler">An active StxEtxHandler object.</param>            
            /// <exception cref="System.ArgumentNullException">Thrown when any argument other then LogTS is null</exception>            
            /// <exception cref="System.OperationCanceledException">Thrown when the user presses cancel when prompted to select a script file</exception>      
            public FlashUNAC(WinSIPserver handler, FlashUNACinfo info, TraceSource LogTS = null)
            {
                if (handler == null)
                    throw new ArgumentNullException();

                this.ServerHandler = handler;
                SetUpBasicFields(info, LogTS);                
                if (!DetermineFlashFileName(out this.FileName))
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
                [StatusOkMsg("Preparing to read the flash file."),
                StatusErrorMsg("Failed to prepare to read the flash file."),
                StatusCompletionPercent(0)]
                Idle,
                [StatusOkMsg("Reading the flash file."),
                StatusErrorMsg("Invalid flash file."),
                StatusCompletionPercent(1)]
                ParseHexFile,
                [StatusOkMsg("Checking if the UNAC is already in flash mode."),
                StatusErrorMsg("Failed to determine the UNAC state."),
                StatusCompletionPercent(2)]
                CheckUNACmode,
                [StatusOkMsg("Placing the UNAC into flash mode."),
                StatusErrorMsg("Unable to instruct UNAC to enter flash mode."),
                StatusCompletionPercent(3)]
                SendFlashModeRequest,
                [StatusOkMsg("UNAC is entering flash mode, waiting for reconnection."),
                StatusErrorMsg("UNAC failed to reconnect after attempting to enter flash mode."),
                StatusCompletionPercent(4)]
                ReconnectToUNAC,
                [StatusOkMsg("Estabilishing communication with UNAC flashing firmware."),
                StatusErrorMsg("Failed to establish communication with UNAC flashing firmware."),
                StatusCompletionPercent(5)]
                FlasherSync,
                [StatusOkMsg("Sending flash file."),
                StatusErrorMsg("Failed to send flash file."),
                StatusCompletionPercent(6)]
                FlashInProgress,
                [StatusOkMsg("Flash successful."),
                StatusErrorMsg("Should not see this message."),
                StatusCompletionPercent(100)]
                Finish
            }

            private void DoTheWork()
            {

                try
                {
                    //1. Read in the hex file
                    CurrentState = WorkState.ParseHexFile;
                    LoadTheHexFile();

                    //2. See if the UNAC is already in flashmode
                    CurrentState = WorkState.CheckUNACmode;
                    if (!isUNACinFlashMode())
                    {
                    //3. Enter flash mode
                        CurrentState = WorkState.SendFlashModeRequest;
                        RequestFlashMode();

                    //4. Reconnect to the UNAC in flash mode
                        CurrentState = WorkState.ReconnectToUNAC;
                        ReconnectToUNAC();

                    //5. Connect to the UNAC in flash mode
                        CurrentState = WorkState.FlasherSync;
                        if (!isUNACinFlashMode())
                            throw new InvalidOperationException();  //think about what kind of exception should be thrown
                    }

                    //6. Start flashing
                    CurrentState = WorkState.FlashInProgress;
                    FlashTheUNAC();

                    //7. Done
                    CurrentState = WorkState.Finish;

                    //8. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    //I don't think we need to delete the CSR?
                    _StatusErrorMessage = "User canceled the flashing the UNAC.";
                    _Status = CompletionCode.UserCancelFinish;
                    LogMsg(TraceEventType.Verbose, ex.ToString());
                }
                catch (Exception ex)
                {                    
                    _StatusErrorMessage = GetStatusErrorMsg(CurrentState) + " " + ex.Message;
                    _Status = CompletionCode.FinishedError;
                    LogMsg(TraceEventType.Warning, ex.ToString());
                    try
                    {                        
                        CloseConnection();
                    }
                    catch { }   //don't care if this fails.
                }
            }
            #endregion

            #region private helper functions
            private void SetUpBasicFields(FlashUNACinfo info, TraceSource LogTS)
            {
                if ((info.userName == null) || (info.password == null) || (info.serverAddress == null) || (info.serverCN == null) || (info.serverCert == null) || 
                    ( info.UNAC_ID == null) || (info.serverPort == 0) )
                    throw new ArgumentNullException();

                BasicInfo = info;

                if (LogTS != null)
                    this.LogTS = LogTS;
                else
                    this.LogTS = new TraceSource("DummyTS");
            }

                        /// <summary>
            /// Call this function to locate the script file
            /// </summary>
            /// <param name="fileName"></param>
            /// <returns></returns>
            private bool DetermineFlashFileName(out string fileName)
            {
                OpenFileDialog fDialog = new OpenFileDialog();
                fDialog.Title = "Select script file to send";
                fDialog.InitialDirectory = Properties.Settings.Default.LastManualBrowseFolder;
                fDialog.CheckFileExists = true;
                fDialog.CheckPathExists = true;
                fDialog.Multiselect = false;
                fDialog.Filter = "Script Files (.hex)|*.hex";
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

            private void CloseConnection()
            {
                if (ISP_CommandHandler != null)
                    ISP_CommandHandler.Dispose();
                ISP_CommandHandler = null;
            }

            /// <summary>
            /// Create the FlashFile object and load in the hex file specified.
            /// </summary>
            private void LoadTheHexFile()
            {
                FlashFile = new FPS_4MBExtFlash_LPC2400();
                FlashFile.LoadHexFile(FileName);
                FlashFile.InsertISR_Checksum();
                FlashFile.InsertChecksums();
            }

            /// <summary>
            /// Determine if the UNAC is in flash mode.
            /// If it is in flash mode, ServerProtocolHandler is disabled and 
            /// a live object is created for ISP_CommandHandler. 
            /// 
            /// If it is not in flash mode, ServerProtocolHandler should still work
            /// just fine.
            /// 
            /// FOR NOW THIS FUNCTION IS STUBBED OUT
            /// </summary>
            /// <returns>true if the UNAC is in flash mode and Flasher is ready to flash, otherwise: false</returns>
            private bool isUNACinFlashMode()
            {
                Boolean bSuccess = false;
                //1. remove the link between the STX ETX handler and the communication object
                //give it a dummy link
                ClientContext LiveConnection = ServerHandler.ProtocolHandler.CommContext;
                ClientContext DummyConnection = new PlaceHolderClientContext(LiveConnection.logMsgs);
                ServerHandler.ProtocolHandler.CommContext = DummyConnection;

                //2. Create an ISP command handler and give it the live link
                ISP_CommandHandler = new FPS_ISP_CommandHandler(new LPC_ISP_Handler(LiveConnection));

                //3. Try to do autobaud
                try
                {
                    ISP_CommandHandler.DoAutoBaudSynchronization();
                    bSuccess = true;
                }
                catch (Exception ex)
                {
                    //autobaud failed
                    bSuccess = false;

                    //if autobaud fails, restore the STX ETX handler.
                    ServerHandler.ProtocolHandler.CommContext = LiveConnection;
                    ISP_CommandHandler.ProtocolHandler.CommContext = DummyConnection;
                    ISP_CommandHandler.Dispose();
                    ISP_CommandHandler = null;
                }
                //private IProtocolHandler ServerProtocolHandler;
                //private FPS_ISP_CommandHandler ISP_CommandHandler;

                return bSuccess;
            }

            private void RequestFlashMode()
            {
                UNAC unacCmdHandler = new UNAC(ServerHandler.ProtocolHandler, LogTS);                

                unacCmdHandler.EnterFlash(FileName.Split('\\').Last(), null);                
            }

            private void ReconnectToUNAC()
            {
                //1. reconnect to the server.
                ServerHandler.Dispose();
                ReconnectOpperation = new LoginToServer(BasicInfo.userName, BasicInfo.password, BasicInfo.serverAddress, BasicInfo.serverCN, BasicInfo.serverPort, BasicInfo.serverCert, LogTS);
                ReconnectOpperation.Start();
                while ((ReconnectOpperation.Status != CompletionCode.FinishedError) &&
                        (ReconnectOpperation.Status != CompletionCode.FinishedSuccess) &&
                        (ReconnectOpperation.Status != CompletionCode.UserCancelFinish))
                {
                    if (Status == CompletionCode.UserCancelReq)
                        ReconnectOpperation.Cancel();
                    else
                        Thread.Sleep(ReconnectOpperation.RefreshInterval);
                }
                //was the reconnect operation to server successful?
                if (ReconnectOpperation.Status != CompletionCode.FinishedSuccess)
                    throw new InvalidOperationException("Unable to reconnect to the login server after instructing the UNAC to enter flash mode.\r\nError: " + ReconnectOpperation.StatusMessage);
                ServerHandler = ((LoginToServer)ReconnectOpperation).ServerConnection;

                //2. if successfully reconnected to the server, reconnect to the UNAC:
                ReconnectOpperation = new EstabilishPassthroughConnection(BasicInfo.UNAC_ID, BasicInfo.UNAC_IDType, ServerHandler, LogTS);
                ReconnectOpperation.Start();
                while ((ReconnectOpperation.Status != CompletionCode.FinishedError) &&
                        (ReconnectOpperation.Status != CompletionCode.FinishedSuccess) &&
                        (ReconnectOpperation.Status != CompletionCode.UserCancelFinish))
                {
                    if (Status == CompletionCode.UserCancelReq)
                        ReconnectOpperation.Cancel();
                    else
                        Thread.Sleep(ReconnectOpperation.RefreshInterval);
                }

                //was the reconnect operation successful?
                if (ReconnectOpperation.Status != CompletionCode.FinishedSuccess)
                    throw new InvalidOperationException("Unable to reconnect to the UNAC after instructing it to enter flash mode.");
            }

            private void FlashTheUNAC()
            {         
                bool TryAgain = false;       
                int RetriesRemain = 3;
                //setup the status registers
                SectorsFlashed = 0;
                for (uint i = 0; i < FlashFile.NumberOfSectors; i++)
                {
                    if (!FlashFile.SectorEmpty(i))
                        SectorsToFlash++;
                }

            //CONFIGURE FLASHER SETTINGS
                do
                {
                    try
                    {
                        //turn echo off
                        ISP_CommandHandler.Echo(false);

                        //Send the unlock command (only needs to be sent once per ISP session):
                        ISP_CommandHandler.Unlock(UnlockCode);

                        TryAgain = false; //success!
                    }
                    catch (Exception ex)
                    {
                        LogMsg(TraceEventType.Error, "Caught the following exception while trying to send commands to setup flasher.\r\n" + ex.ToString());
                        RetriesRemain--;
                        if (!ISP_CommandHandler.ProtocolHandler.CommContext.bConnected)
                        {
                            LogMsg(TraceEventType.Error, "Connection terminated. Aborting flash attempt.");
                            throw ex;   //connection lost or out of retries, re-throw exception and abort
                        }
                        else if (RetriesRemain <= 0)
                        {
                            LogMsg(TraceEventType.Error, "Out of retries for this sector. Aborting flash attempt.");
                            throw ex;   //connection lost or out of retries, re-throw exception and abort
                        }
                        else
                        {
                            LogMsg(TraceEventType.Error, "Delaying 1 second and then retrying. " + RetriesRemain.ToString() + " retries remain.");
                            TryAgain = true;
                            Thread.Sleep(1000);
                        }
                    }
                } while (TryAgain);

            //DO THE FLASHING
                for (uint i = 0; i < FlashFile.NumberOfSectors; i++)
                {
                    CurrentSector = (int)i;     //status info

                    if (!FlashFile.SectorEmpty(i))
                    {
                        RetriesRemain = 3;
                        TryAgain = false;
                        do
                        {
                            try
                            {
                                //does it have a valid md5 hash?
                                byte[] found_hash;
                                ISP_CommandHandler.GenerateHashOfData(FlashFile.SectorAddress(i), FlashFile.SectorSize(i), out found_hash);
                                byte[] calc_hash = FlashFile.GetSectorMD5Hash(i);
                                if (found_hash != FlashFile.GetSectorMD5Hash(i))
                                {
                                    //needs to be flashed

                                    //1. Copy to RAM (optionally use compression)
                                    ISP_CommandHandler.WriteToRAM(DeviceRAMaddr, FlashFile.GetSectorData(i));

                                    //2. Prepare sector for erasing
                                    ISP_CommandHandler.PrepareSectors(i, i);

                                    //3. Erase the sector
                                    /*ISP_CommandHandler.EraseSectors(i, i);

                                    //4. Prepare sector for flashing
                                    ISP_CommandHandler.PrepareSectors(i, i);

                                    //5. Copy the RAM into the sector
                                    ISP_CommandHandler.CopyRAMtoFlash(FlashFile.SectorAddress(i), DeviceRAMaddr, FlashFile.SectorSize(i));*/

                                    //6. Verify the flash
                                    ISP_CommandHandler.GenerateHashOfData(DeviceRAMaddr, FlashFile.SectorSize(i), out found_hash);
                                    if (found_hash != FlashFile.GetSectorMD5Hash(i))
                                        throw new InvalidOperationException("Hash verification failed for sector " + i.ToString() + ".");

                                    //7. sector complete
                                    TryAgain = false;
                                    SectorsFlashed++;   //status info: indicate another sector has been flashed.
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMsg(TraceEventType.Error, "Caught the following exception while flashing sector " + i.ToString() + ".\r\n" + ex.ToString());                                                                
                                RetriesRemain--;
                                if (!ISP_CommandHandler.ProtocolHandler.CommContext.bConnected)
                                {
                                    LogMsg(TraceEventType.Error, "Connection terminated. Aborting flash attempt.");
                                    throw ex;   //connection lost or out of retries, re-throw exception and abort
                                }
                                else if (RetriesRemain <= 0)
                                {
                                    LogMsg(TraceEventType.Error, "Out of retries for this sector. Aborting flash attempt.");
                                    throw ex;   //connection lost or out of retries, re-throw exception and abort
                                }
                                else
                                {
                                    LogMsg(TraceEventType.Error, "Delaying 1 second and then retrying. " + RetriesRemain.ToString() + " retries remain for this sector.");
                                    TryAgain = true;
                                    Thread.Sleep(1000);
                                }
                            }
                        } while (TryAgain);
                    }
                }
            }

            #endregion
        }
    }

}
