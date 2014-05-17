using ForwardLibrary.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    namespace Communications
    {
        //delegate definitions:  
        
        /// <summary>
        /// Function to handle communication events
        /// </summary>
        /// <param name="ev">The event</param>
        public delegate void EventNotify(ClientEvent ev);   //delegate for communication events
                
        /// <summary>
        /// Delegate for logging
        /// </summary>
        /// <param name="eventType">Type of event</param>
        /// <param name="id">Event id: unique ID for each connection, single ID for all pending connections (TCP)</param>
        /// <param name="msg">The event message</param>
        //public delegate void LogHandler(TraceEventType eventType, int id, string msg);

        public class CommLogMessages
        {
            public string msgNewTCP_Client = "New TCP client connected";

            /// <summary>
            /// Message that is displayed with a disconnect occurs
            /// [msgTCP_DisconnectWithReason] [reason code]
            /// see ClientDisconnectedEvent class for reason codes.
            /// if msgSuppressTCP_DisconnectReason == true, then the 
            /// reason code is omitted.
            /// </summary>
            public string msgTCP_DisconnectWithReason = "Socket disconnect: "; 

            /// <summary>
            /// Set this to true to suppress disconnect reason code messages from the log.
            /// </summary>
            public bool msgSuppressTCP_DisconnectReason = false;
        }


        public class SSL_Settings
        {
            /// <summary>
            /// Set to false if TLS role should be as a client
            /// </summary>
            public bool server = true;

            /// <summary>
            /// Require a certificate for the peer:
            /// If this is a server, set to true to require a client ceritifcate
            /// If this is a client, set to true to require a server certificate
            /// </summary>
            public bool requirePeerCert = false;

            /// <summary>
            /// Check the certificate revocation lists?
            /// </summary>
            public bool checkCertRevocation = false;

            /// <summary>
            /// Which SSL/TLS protocols are enabled.
            /// </summary>
            public SslProtocols protocolsAllowed = SslProtocols.Tls11 | SslProtocols.Tls12;

            /// <summary>
            /// timeout setting in ms
            /// </summary>
            public int readTimeout = 5000;

            /// <summary>
            /// timeout setting in ms
            /// </summary>
            public int writeTimeOut = 5000;

            /// <summary>
            /// Set to true to leave the connection open when the SSL session has ended
            /// </summary>
            public bool LeaveConnectionOpen = false;


            /// <summary>
            /// The encryption policy to use for the connection
            /// </summary>
            public EncryptionPolicy encPolicy = EncryptionPolicy.RequireEncryption;

            public delegate bool RemoteCertValidationCallback(object sender, X509Certificate PeerCertificate,
                X509Chain PeerCertificateChain, SslPolicyErrors sslPolicyErrors,
                TraceSource log, int LogID, CommLogMessages msgs);

            public delegate X509Certificate LocalCertSelectionCallback(object sender, string targetHost,
                X509CertificateCollection localCertificates, X509Certificate remoteCertificate,
                    string[] acceptableIssuers, TraceSource log, int LogID, CommLogMessages msgs);

            /// <summary>
            /// Set this to override the default internal delegate for verifying the peer
            /// </summary>
            public RemoteCertValidationCallback ValidatePeerCallback = null;

            /// <summary>
            /// Set this to override the default internal delegate for selecting a certificate
            /// </summary>
            public LocalCertSelectionCallback SelectLocalCertificate = null;

            /// <summary>
            /// Certificate used by local machine. For a client, this is not required
            /// and can be handled by SelectLocalCertificate delegate instead. 
            /// For a server, this is required.            
            /// </summary>
            public X509Certificate2 localCert = null;

            /// <summary>
            /// Optional. Specify the certificates which need to have signed the peer's certificate.
            /// This will be used in the default ValidatePeerCallback. If the peer's certificate
            /// was not signed by these signers, the connection will be aborted.
            /// 
            /// Note: requirePeerCert must be true for this to be checked.
            /// </summary>
            public X509Certificate2Collection peerSigners = null;

            /// <summary>
            /// Optional. Specify that the peer's certificate must be one of the certificates 
            /// in this collection. This is checked in the defaul ValidatePeerCallback.
            /// If the peer's certificate is not in this collection (and the collection is not
            /// null), the connection will be aborted.
            /// 
            /// Note: requirePeerCert must be true for this to be checked.
            /// </summary>
            public X509Certificate2Collection expectedPeerCert = null;

            /// <summary>
            /// Optional. If specified, the common name field of the peer's certificate will
            /// be checked against this string. If it doesn't match, the connection will be
            /// aborted.
            /// 
            /// Note: requirePeerCert must be true for this to be checked.
            /// </summary>
            public string peerName = null;

            /// <summary>
            /// Optional. Additional object that will be carried through to the communication
            /// context for custom application usage.
            /// </summary>
            public Object additionalObject = null;


            #region Start connection functions
            public SslStream StartSSL_Connection(Stream stream, TraceSource log, int LogID, CommLogMessages msgs)
            {
                SslStream sslStream;
                RemoteCertificateValidationCallback rcvCallback = setupRemoteCertValidationCallback(log, LogID, msgs);
                LocalCertificateSelectionCallback lcsCallback = setupLocalCertSelectionCallback(log, LogID, msgs);
                
                /*RemoteCertificateValidationCallback _ValidatePeerCallback = 
                    new RemoteCertificateValidationCallback( 
                        (sender, ClientCertificate, ClientCertificateChain, sslPolicyErrors) => 
                            dValidatePeerCertificate(sender, ClientCertificate, ClientCertificateChain, sslPolicyErrors, log, LogID, msgs));
                
                LocalCertificateSelectionCallback _SelectLocalCertificate = new LocalCertificateSelectionCallback (
                    (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => 
                        dSelectLocalCertificate( sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers, 
                            log, LogID, msgs));*/

                sslStream = new SslStream(stream, LeaveConnectionOpen, rcvCallback, lcsCallback, encPolicy);

                try
                {
                    log.TraceEvent(TraceEventType.Verbose, LogID, "Starting SSL authentication with peer.");
                    if (server)
                        sslStream.AuthenticateAsServer(localCert, requirePeerCert, protocolsAllowed, checkCertRevocation);
                    else
                    {
                        X509CertificateCollection col = new X509CertificateCollection();
                        if (localCert != null)
                            col.Add(localCert);

                        sslStream.AuthenticateAsClient(peerName, col, protocolsAllowed, checkCertRevocation);
                    }

                    sslStream.ReadTimeout = readTimeout;
                    sslStream.WriteTimeout = writeTimeOut;

                    //okay, we are up!
                    if (sslStream.RemoteCertificate != null)
                        log.TraceEvent(TraceEventType.Verbose, LogID, "SSL authentication succeeded, connected to peer with certificate: " + sslStream.RemoteCertificate.Subject);
                    else
                        log.TraceEvent(TraceEventType.Verbose, LogID, "SSL authentication succeeded, connected to peer without a certificate");
                }
                catch (Exception e)
                {
                    log.TraceEvent(TraceEventType.Warning, LogID, "SSL server connection caught exception: " + e.ToString());
                    throw e;
                }
                return sslStream;
            }
            #endregion

            #region private helper functions
            private RemoteCertificateValidationCallback setupRemoteCertValidationCallback(TraceSource log, int LogID, CommLogMessages msgs)
            {
                RemoteCertValidationCallback _ValidatePeerCallback = new RemoteCertValidationCallback(dValidatePeerCertificate);
                if (ValidatePeerCallback != null)
                    _ValidatePeerCallback = ValidatePeerCallback;

                RemoteCertificateValidationCallback rcvCallback =
                    (sender, ClientCertificate, ClientCertificateChain, sslPolicyErrors) =>
                            _ValidatePeerCallback(sender, ClientCertificate, ClientCertificateChain, sslPolicyErrors, log, LogID, msgs);

                return rcvCallback;
            }

            private LocalCertificateSelectionCallback setupLocalCertSelectionCallback(TraceSource log, int LogID, CommLogMessages msgs)
            {
                LocalCertSelectionCallback _SelectLocalCertificate = new LocalCertSelectionCallback(dSelectLocalCertificate);
                if (SelectLocalCertificate != null)
                    _SelectLocalCertificate = SelectLocalCertificate;

                LocalCertificateSelectionCallback lcsCallback =
                    (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                        _SelectLocalCertificate(sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers,
                            log, LogID, msgs);

                return lcsCallback;
            }

            private bool dValidatePeerCertificate(object sender, X509Certificate PeerCertificate,
                X509Chain PeerCertificateChain, SslPolicyErrors sslPolicyErrors,
                TraceSource log, int LogID, CommLogMessages msgs) //note the final three parameters are "curried" http://stackoverflow.com/questions/14324803/passing-delegate-function-with-extra-parameters
            {
                /*Console.WriteLine("ValidateClientCertificate Callback.");
                Console.WriteLine("Sender: {0}", sender);
                Console.WriteLine("ClientCertificate: {0}", ClientCertificate);
                Console.WriteLine("ClientCertificateChain: {0}", ClientCertificateChain);*/

                bool finalPass = true;
                try
                {
                    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNotAvailable)
                    {
                        if (requirePeerCert == true)
                        {
                            finalPass = false;
                            log.TraceEvent(TraceEventType.Warning, LogID, "Ssl Policy Errors: " + sslPolicyErrors.ToString() + ". Aborting connection.");
                        }
                    }
                    else if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        finalPass = false;
                        log.TraceEvent(TraceEventType.Warning, LogID, "Ssl Policy Errors: " + sslPolicyErrors.ToString() + ". Aborting connection.");
                    }
                                        
                    if ( (requirePeerCert == true) || (!server))
                    {
                        X509Certificate2 peerCert = new X509Certificate2(PeerCertificate);

                        //check certificate name
                        if (peerName != null)
                            if (peerName != peerCert.GetNameInfo(X509NameType.SimpleName, false))
                            {
                                finalPass = false;
                                log.TraceEvent(TraceEventType.Warning, LogID, "Certificate common name mismatch. Expected: " + peerName + ", found: " + peerCert.GetNameInfo(X509NameType.SimpleName, false) + ". Aborting connection.");
                            }
                        
                        //check if client certificate matches one of our accepted certificates
                        if (expectedPeerCert != null)                        
                            if (!CheckCollectionForPeerCert(peerCert))
                            {
                                finalPass = false;
                                log.TraceEvent(TraceEventType.Warning, LogID, "Peer certificate is not in list of acceptable certificates: " + peerCert.GetNameInfo(X509NameType.SimpleName, false) + ". Aborting connection.");
                            }                      

                        //check if client certificate has the expected signers
                        if (peerSigners != null)
                            if (!CheckPeerCertSigners(peerCert, PeerCertificateChain))
                            {
                                finalPass = false;
                                log.TraceEvent(TraceEventType.Warning, LogID, "Peer certificate does not have the proper signers. Aborting connection.");
                            } 
                    }
                }
                catch (Exception e)
                {
                    finalPass = false;
                    log.TraceEvent(TraceEventType.Warning, LogID, "Exception caught when validating the peer certificate. Aborting connection. " + e.ToString());
                }                

                // Do not allow this client to communicate with unauthenticated servers. 
                return finalPass;
            }


            private X509Certificate dSelectLocalCertificate( object sender, string targetHost,
                X509CertificateCollection localCertificates, X509Certificate remoteCertificate,
                    string[] acceptableIssuers, TraceSource log, int LogID, CommLogMessages msgs) //note the final three parameters are "curried" http://stackoverflow.com/questions/14324803/passing-delegate-function-with-extra-parameters
            {                                
                return localCert;
            }

            
            private bool CheckCollectionForPeerCert(X509Certificate2 remoteCert)
            {
                bool foundPeerCert = false;
                foreach (X509Certificate2 pcert in expectedPeerCert)
                {
                    if (pcert.Thumbprint == remoteCert.Thumbprint)
                    {
                        foundPeerCert = true;
                        break;
                    }
                }
                return foundPeerCert;
            }

            private bool CheckPeerCertSigners(X509Certificate2 remoteCert, X509Chain chain)
            {
                bool signersOk = false;
                try
                {
                    CStoredCertificate.ChainCertificateAndSigners(remoteCert, peerSigners, true);
                    signersOk = true;
                }
                catch (Exception e)
                {
                    signersOk = false;
                }
                return signersOk;
            }
            #endregion


        }

        /// <summary>
        /// Think of this class as a context for creating new connections, whether those connections
        /// are incoming or outgoing.
        /// </summary>
        public class TCPconnManager
        {
            #region TCP Communications            

            
            /// <summary>
            /// Delegate for when a new connection has been made (client or server) OR an error was encountered in the TCPconnManager
            /// </summary>
            /// <param name="context">The new client context (null if an error was encountered)</param>
            /// <param name="success">Whether or not the new connection was successful</param>
            /// <param name="e">Exception (null if no exception)</param>
            public delegate void NewClient(ClientContext context, bool success, Exception e);       //delegate for when a new client is       

            public NewClient NewClientCallback;
            public TraceSource LogTrace; // LogHandler LogMsg;
            public int ConnectToServerLogID = -1;       //id that is used when calling the LogHandler delegate for logging when connecting to a server
            public int AcceptConnLogID = -2;            //id that is used when calling the LogHandler delegate for logging when accepting client connections
            public TcpListener listener;                //listener used when acting as a server
            public bool bVerboseData = false;           //set to true created client contexts should report send and receive data to the trace listener (will use verbose setting)

            /// <summary>
            /// The default port used for connection (will be used unless a method over rides this)
            /// </summary>
            public int Port = 1100;                     

            /// <summary>
            /// Messages used for logging
            /// </summary>
            public CommLogMessages logMsgs = new CommLogMessages();

            public SSL_Settings sslSettings = null;     //if this is null, TLS will not be used.

            /// <summary>
            /// 
            /// </summary>
            /// <param name="trace">Trace source for logging</param>
            /// <param name="Msgs">Used for overriding the default log messages</param>
            /// <param name="optionalVerboseData">Set to true if extra verbose logging should be used</param>
            public TCPconnManager(TraceSource trace, CommLogMessages Msgs, bool optionalVerboseData = false)
            {
                logMsgs = Msgs;
                LogTrace = trace;
                bVerboseData = optionalVerboseData;
            }

            public TCPconnManager(TraceSource trace, bool optionalVerboseData = false)
            {
                LogTrace = trace;
                bVerboseData = optionalVerboseData;
            }

            public TCPconnManager()
            {
            }

            /// <summary>
            /// Blocking connect function. Calling function must handle exceptions.
            /// </summary>
            /// <param name="hostname"></param>
            /// <param name="port"></param>
            /// <param name="log">Log function to be used for communication protocol</param>
            /// <returns></returns>
            public ClientContext ConnectToServer(string hostname, Int32 port)
            {
                TcpClient theClient = new TcpClient();                

                theClient.Connect(hostname, port);
                TCP_ClientContext context = new TCP_ClientContext(theClient, LogTrace, bVerboseData, logMsgs, sslSettings);

                return context;
            }


            /// <summary>
            /// Start up the TCP server
            /// </summary>
            /// <param name="newClient">Delegate to call when a new client has been accepted</param>
            /// <returns>Returns a reference to the listener</returns>
            public void ConnectToServerAsync(NewClient newClient, string hostname, Int32 port)
            {
                //#region Sockets server
                TcpClient theClient = new TcpClient();
                NewClientCallback = newClient;                

                theClient.BeginConnect(hostname, port, OnServerAccepted, theClient);
                //#endregion
            }


            /// <summary>
            /// handle new connections to a server asynchronously (high performance)
            /// </summary>
            /// <param name="ar"></param>
            private void OnServerAccepted(IAsyncResult ar)
            {
                TcpClient theClient = ar.AsyncState as TcpClient;
                if (theClient == null)
                    return;
                

                try
                {
                    theClient.EndConnect(ar);
                    TCP_ClientContext context = new TCP_ClientContext(theClient, LogTrace, bVerboseData, logMsgs, sslSettings);

                    if (NewClientCallback != null)
                        NewClientCallback(context, true, null);

                }
                catch (Exception ex)
                {
                    NewClientCallback(null, false, ex);
                    LogTrace.TraceEvent(TraceEventType.Error, AcceptConnLogID, "Connection failed with exception: " + ex.ToString());
                    //Console.WriteLine(ex);
                }
            }

            /// <summary>
            /// Start up the TCP server
            /// </summary>
            /// <param name="newClient">Delegate to call when a new client has been accepted</param>
            /// <returns>Returns a reference to the listener</returns>
            public TcpListener Start_TCP_Server(NewClient newClient)
            {                

                //#region Sockets server
                TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
                NewClientCallback = newClient;

                listener.Start();
                listener.BeginAcceptTcpClient(OnClientAccepted, listener);

                return listener;
                //#endregion
            }

            private delegate void makeOnClientAsync(); //IAsyncResult ar, TcpListener listener);
            //handle new connections asynchronously (high performance)
            private void OnClientAccepted(IAsyncResult ar)
            {

                TcpListener listener = ar.AsyncState as TcpListener;
                if (listener == null)
                    return;
                
                makeOnClientAsync caller = delegate() //IAsyncResult ar, TcpListener listener)
                {
                    try
                    {                        
                        TCP_ClientContext context = new TCP_ClientContext(listener.EndAcceptTcpClient(ar), LogTrace, bVerboseData, logMsgs, sslSettings);

                        if (NewClientCallback != null)
                            NewClientCallback(context, true, null);

                    }
                    catch (Exception ex)
                    {
                        NewClientCallback(null, false, ex);
                        LogTrace.TraceEvent(TraceEventType.Error, AcceptConnLogID, "Caught exception when accepting a socket: " + ex 
                            + "\r\n\r\n Will continue accepting sockets");                                        
                    }  
                };

                try
                {
                    caller.BeginInvoke(delegate(IAsyncResult arr) { caller.EndInvoke(arr); }, null);
                }
                catch (Exception ex)
                {
                    NewClientCallback(null, false, ex);
                    LogTrace.TraceEvent(TraceEventType.Error, AcceptConnLogID, "Caught exception when trying to asynchronously connect a socket: " + ex
                        + "\r\n\r\n Will continue accepting sockets");
                }
                finally
                {
                    listener.BeginAcceptTcpClient(OnClientAccepted, listener);
                }
            }

            
            #endregion

        }

        #region Communication events
        public abstract class ClientEvent
        {

            public DateTime EventTime = DateTime.Now;
            public Exception ex = null;
            public ClientContext Context;
        }

        public class ClientReceivedDataEvent : ClientEvent
        {
            public byte[] RcvDat;

            public ClientReceivedDataEvent(byte[] RcvDat, ClientContext _context)
            {
                this.RcvDat = RcvDat;
                Context = _context;
            }
        }

        public class ClientWroteDataEvent : ClientEvent
        {

            public ClientWroteDataEvent(ClientContext _context)
            {
                Context = _context;
            }

        }

        public enum DisconnectReason : int { UNEXPECTED_DISCONNECT = 0, CLIENT_DISCONNECT = 1 };

        public class ClientDisconnectedEvent : ClientEvent
        {

            public DisconnectReason iReason;

            public ClientDisconnectedEvent(DisconnectReason iReason, ClientContext _context, Exception e = null)
            {
                this.iReason = iReason;
                Context = _context;
                ex = e;
            }
        }
        #endregion

        public abstract class ClientContext
        {            
            public DateTime lastRcvd = DateTime.Now;
            public DateTime lastSent = DateTime.Now;
            public CommLogMessages logMsgs;

            public Stream Stream;

            protected int _ConnectionID;
            public int ConnectionID { get { return _ConnectionID;  } }

            protected bool _bConnected = false;
            public bool bConnected             //indicates whether the client is still connected
            { 
                get{return _bConnected;}
            }
            public bool bReportAllMsgData = true;       //set to false if message data should not be reported to tracesource
            public EventNotify EventCallback = null;

            public abstract bool Write(byte[] data);    //to be implmented by child class
            public abstract void Close();               //to be implemented by child class
            public abstract void LogMsg(TraceEventType eventType, string msg);
            public void LogMsg(TraceEventType eventType, object msgSource)
            {
                LogMsg(eventType, msgSource.ToString());
            }
            public ClientContext(CommLogMessages msgs)
            {
                logMsgs = msgs;
            }
            
        }

        /// <summary>
        /// TCP Client's implementation of ClientContext
        /// </summary>
        public class TCP_ClientContext : ClientContext
        {
            

            public TcpClient Client;
            //public NetworkStream Stream;            
            
            

            public byte[] Buffer = new byte[4096]; //temporary received data, the size of this buffer determines how many bytes can be read at one time                                

            public TraceSource TheLogTrace;

            private void basicConstructorSetup(TcpClient Client, TraceSource log, bool bReportAllMsgData, CommLogMessages msgs)
            {
                _ConnectionID = GetNewConnID();
                TheLogTrace = log;
                _bConnected = true;
                this.Client = Client;
                base.bReportAllMsgData = bReportAllMsgData;
                Client.NoDelay = true;  //set this to true so that the server is very responsive (gets rid of latency, at the expense of network efficiency)

                Stream = Client.GetStream();

                LogMsg(TraceEventType.Information, logMsgs.msgNewTCP_Client);
            }

            /// <summary>
            /// Use this constructor when no SSL is required.
            /// </summary>
            /// <param name="Client"></param>
            /// <param name="log"></param>
            /// <param name="bReportAllMsgData"></param>
            /// <param name="msgs"></param>
            public TCP_ClientContext(TcpClient Client, TraceSource log, bool bReportAllMsgData, CommLogMessages msgs) : base(msgs)
            {                
                basicConstructorSetup(Client, log, bReportAllMsgData, msgs);
                
                //start reading immediately
                Stream.BeginRead(Buffer, 0, Buffer.Length, this.OnClientRead, null);
            }

            /// <summary>
            /// Use this constructor to add SSL over the top of the connection.
            /// </summary>
            /// <param name="Client"></param>
            /// <param name="log"></param>
            /// <param name="bReportAllMsgData"></param>
            /// <param name="msgs"></param>
            /// <param name="ssl_settings"></param>
            public TCP_ClientContext(TcpClient Client, TraceSource log, bool bReportAllMsgData, CommLogMessages msgs, SSL_Settings ssl_settings)
                : base(msgs)
            {
                basicConstructorSetup(Client, log, bReportAllMsgData, msgs);
                if (ssl_settings != null)
                {
                    try
                    {
                        SslStream ssl_stream = ssl_settings.StartSSL_Connection(Stream, log, ConnectionID, msgs);
                        Stream = ssl_stream;
                    }
                    catch (Exception e)
                    {
                        //any excption means that the SSL negotiation failed.
                        Client.Close();
                        Stream.Dispose();

                        throw e;
                    }
                }
                //start reading immediately
                Stream.BeginRead(Buffer, 0, Buffer.Length, this.OnClientRead, null);
            }

            #region Public methods overridden in Client Context
            /// <summary>
            /// Implementation of the Write method -- should be non-blocking and thread safe
            /// </summary>
            /// <param name="data">The data to be written</param>
            /// <returns>Whether the write was successfully queued or not, note that Wrote event will be generated upon completion</returns>
            public override bool Write(byte[] data)
            {
                bool bRetVal = false;
                if (Client.Connected == false)
                    return false;

                try
                {
                    if (bReportAllMsgData)
                        LogMsg(TraceEventType.Verbose, "TCP WRITE: " + data.ToString());

                    Stream.BeginWrite(data, 0, data.Length, this.OnWriteCompleted, this);
                    lastSent = DateTime.Now;
                    bRetVal = true;
                }
                catch (IOException e)
                {
                    LogMsg(TraceEventType.Error, "Exception caught, socket is closed: " + e);

                    //socket has closed
                    CloseTCP(DisconnectReason.UNEXPECTED_DISCONNECT, e);                    

                    //else if (e.GetType is ObjectDisposedException)
                    //{
                    //socket has closed and everything is cleaned up already, no need to alert the user (already has been alerted)
                    //}
                }
                catch (Exception e)
                {
                    LogMsg(TraceEventType.Error, "Exception caught, no course of action defined: " + e);
                }

                return bRetVal;
            }

            /// <summary>
            /// Implementation of Close
            /// </summary>
            public override void Close()
            {
                CloseTCP(DisconnectReason.CLIENT_DISCONNECT);
            }

            #endregion
            
            public override void LogMsg(TraceEventType eventType, string msg)
            {
                TheLogTrace.TraceEvent(eventType, ConnectionID, msg);
            }
            #region Event handlers

            /// <summary>
            /// Handle new reads asychronously (high performance)
            /// </summary>
            /// <param name="ar">The asynchronous result</param>
            void OnClientRead(IAsyncResult ar)
            {
                bool bDoRead = true;
                try
                {
                    int read = Stream.EndRead(ar);
                    if (read > 0)
                    {
                        lastRcvd = DateTime.Now;
                        MemoryStream byTemp = new MemoryStream();
                        byTemp.Write(Buffer, 0, read);
                        Buffer = new byte[Buffer.Length];   //create a new object for future data

                        if (bReportAllMsgData)
                            LogMsg(TraceEventType.Verbose, "TCP READ: " + System.Text.Encoding.Default.GetString(byTemp.ToArray()));

                        //alert the client, if delegate is specified
                        if (EventCallback != null)
                        {
                            try
                            {
                                EventCallback(new ClientReceivedDataEvent(byTemp.ToArray(), this));
                            }
                            catch (Exception e)
                            {
                                LogMsg(TraceEventType.Error, "Callback threw an exception: " + e);
                            }
                        }


                    }

                    //read 0 bytes, means the connection dropped
                    else
                    {
                        CloseTCP(DisconnectReason.CLIENT_DISCONNECT);
                        bDoRead = false;
                    }
                }

                catch (System.Exception e)
                {
                    /* Just means that the client context was disposed Console.WriteLine("{0} Exception caught, closing socket.", e);*/
                    CloseTCP(DisconnectReason.UNEXPECTED_DISCONNECT, e);
                    bDoRead = false;
                }
                finally
                {
                    if (bDoRead == true)
                        try
                        {
                            Stream.BeginRead(Buffer, 0, Buffer.Length, this.OnClientRead, null);
                        }
                        catch (Exception e)
                        {
                            //NO need to log, just means that the connection was closed by another part of the application. Console.WriteLine("Reading socket threw an exception: {0} (Socket possibly closed somehow? Forcing it to close now).", e);
                            try { CloseTCP(DisconnectReason.UNEXPECTED_DISCONNECT, e); }
                            catch { }
                        }
                }
            }



            /// <summary>
            /// Handle completions of writes to the socket
            /// </summary>
            /// <param name="ar"></param>
            public void OnWriteCompleted(IAsyncResult ar)
            {
                try
                {
                    Stream.EndWrite(ar);
                    if (EventCallback != null)
                    {
                        try
                        {
                            EventCallback(new ClientWroteDataEvent(this));   //callback will remove context if no more reading should be done
                        }
                        catch (Exception e)
                        {
                            LogMsg(TraceEventType.Error, "Exception caught in even callback: " + e);
                        }
                    }
                }
                catch (Exception e)
                {
                    /*Just means socket was closed Console.WriteLine("{0} Exception caught, closing socket.", e);*/
                    CloseTCP(DisconnectReason.UNEXPECTED_DISCONNECT, e);
                }
            }
            #endregion

            #region private helper functions
            /// <summary>
            /// Close the TCP socket and free up resources
            /// </summary>
            /// <param name="iReason">Reason that the function is called, see ClientDisconnectEvent class</param>
            /// <param name="ex"></param>
            private void CloseTCP(DisconnectReason iReason, Exception ex = null)
            {
                if (bConnected)
                {
                    _bConnected = false;
                    Client.Close();
                    Stream.Dispose();

                    if (logMsgs.msgSuppressTCP_DisconnectReason == false)
                        LogMsg(TraceEventType.Information, logMsgs.msgTCP_DisconnectWithReason + iReason); //"Socket disconnect: " + iReason + ".");
                    else
                        LogMsg(TraceEventType.Information, logMsgs.msgTCP_DisconnectWithReason); //"Socket disconnect: " + iReason + ".");
                    
                    if (EventCallback != null)
                    {
                        try
                        {
                            EventCallback(new ClientDisconnectedEvent(
                                iReason, this, ex));
                        }
                        catch (Exception e)
                        {
                            LogMsg(TraceEventType.Error, "Callback threw an exception: " + e);
                        }
                    }
                }
            }

            private static object connIDlock = new object();
            private static int connID = 1;
            private int GetNewConnID()
            {
                int id;
                lock(connIDlock)
                {
                    id = connID++;
                }
                return id;
            }


            #endregion

        }


    }
}
