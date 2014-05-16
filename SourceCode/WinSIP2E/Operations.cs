using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using ForwardLibrary.Communications.STXETX;
using ForwardLibrary.Crypto;
using ForwardLibrary.WinSIPserver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace WinSIP2E
{
    namespace Operations
    {

        abstract class Operation
        {
            protected object SyncObject = new object();

            protected abstract TraceSource LogTS;

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

            private string __StatusMessage = "";
            private string _StatusMessage
            {
                get { return __StatusMessage; }
                set
                {
                    __StatusMessage = value;
                    LogMsg(TraceEventType.Verbose, value);
                }
            }
            public override string StatusMessage
            {
                get { return _StatusMessage; }
            }

            private CompletionCode _Status = CompletionCode.NotStarted;
            public override CompletionCode Status
            {
                get { return _Status; }
            }

            private int _StatusPercent = 0;
            public override int StatusPercent { get { return _StatusPercent; } }

            //Certificate fields
            public string Country = "US";
            public string State = "Minnesota";
            public string Locality = "Eden Prairie";
            public string Organization = "Forward Pay Systems, Inc.";
            public string OrganizationalUnit = "Forward Pay";

            //private internal properties
            private string PinCode;
            private string MachineID;
            private string ServerAddress, ServerCN;
            private int ServerPort;
            private WinSIPserver ServerHandler;
            private bool CloseUponCompletion = true;
            private string CertificateID;
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

            }

            public override void Cancel()
            {
                _Status = CompletionCode.UserCancelReq;
            }

            

            #endregion

            #region Main helper functions
            enum WorkState
            {
                Idle,
                ConnectToServer,
                ObtainCertID,
                GenerateCSR,
                UploadCSR,
                Finish
            }

            private void DoTheWork()
            {
                WorkState state = WorkState.Idle;
                try
                {
                    //1. Connect to server if not already connected:   
                    if (ServerHandler == null)
                    {
                        state = WorkState.ConnectToServer;
                        _StatusMessage = "Connecting to WinSIP server.";
                        ConnectToServer();
                    }

                    CheckUserCancel();

                    //2. Obtain a certificate ID:                                        
                    state = WorkState.ObtainCertID;
                    _StatusMessage = "Obtaining a certificate ID.";
                    _StatusPercent = 25;
                    ServerHandler.CID(PinCode, MachineID, out CertificateID);
                    

                    CheckUserCancel();

                    //3. Create a new certificate   
                    state = WorkState.GenerateCSR;
                    _StatusMessage = "Generating a certificate signing request.";
                    _StatusPercent = 50;
                    string CSR = GenerateCertificateRequest();

                    CheckUserCancel();

                    //4. Upload the certificate request
                    state = WorkState.UploadCSR;
                    _StatusMessage = "Uploading the certificate signing request.";
                    _StatusPercent = 75;
                    ServerHandler.CCSR(CSR);

                    CheckUserCancel();

                    //5. Done
                    state = WorkState.Finish;
                    _StatusMessage = "Successfully generated and uploaded request. Closing the connection to the server.";
                    _StatusPercent = 100;

                    CheckUserCancel();

                    //6. Mark completed
                    _Status = CompletionCode.FinishedSuccess;

                }
                catch (OperationCanceledException ex)
                {
                    //clean up in here: delete the CSR from the PC if created
                }
                catch (Exception ex)
                {
                    //clean up in here: delete the CSR from the PC if created
                    switch(state)
                    {
                        case WorkState.ConnectToServer:
                            _StatusMessage = "Failed to connect to the WinSIP server. Error: " + ex.Message;
                            break;
                        case WorkState.GenerateCSR
                            _StatusMessage = "Failed to connect to the WinSIP server. Error: " + ex.Message;
                    LogMsg(TraceEventType.Warning, e.ToString());
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
                    _StatusMessage = "User canceled the operation.";
                    throw new OperationCanceledException("User canceled the operation");
                }
            }

            private void CloseConnection()
            {
                ServerHandler.Dispose();
            }

            private string GenerateCertificateRequest()
            {
                string commonName = "PIN" + PinCode + "-MID" + MachineID + "-CID" + CertificateID;

                CCertificateRequest req = new CCertificateRequest
                {
                    CommonName = commonName,
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
    }

}
