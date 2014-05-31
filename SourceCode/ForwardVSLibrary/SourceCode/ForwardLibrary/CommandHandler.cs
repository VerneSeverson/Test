using ForwardLibrary.WinSIPserver;
using ForwardLibrary.Communications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForwardLibrary.Communications.STXETX;
using System.Security;
using ForwardLibrary.Crypto;

namespace ForwardLibrary
{

    namespace Communications
    {
        namespace CommandHandlers
        {
            #region Exceptions
            public class ResponseException : Exception
            {
                public string DataSent;
                public List<string> ResponsesReceived;

                public virtual string ExtendedMessage
                {
                    get
                    {
                        string ret = Message + "\r\nSent: " + DataSent;
                        if (ResponsesReceived != null)
                            ret = ret + "\r\nReceived: " + String.Join("\r\n", ResponsesReceived.ToArray());
                        return ret;
                    }
                }

                public ResponseException(string message, string sent, string response)
                    : base(message)
                {
                    DataSent = sent;
                    ResponsesReceived = new List<string>();
                    ResponsesReceived.Add(response);
                }

                public ResponseException(string message, string sent, List<string> responses)
                    : base(message)
                {
                    DataSent = sent;
                    ResponsesReceived = responses;
                }

                public ResponseException(string message, string sent, string response, Exception innerException)
                    : base(message, innerException)
                {
                    DataSent = sent;
                    ResponsesReceived = new List<string>();
                    ResponsesReceived.Add(response);
                }

                public ResponseException(string message, string sent, List<string> responses, Exception innerException)
                    : base(message, innerException)
                {
                    DataSent = sent;
                    ResponsesReceived = responses;
                }

                public override string ToString()
                {
                    /*return "Sent: " + DataSent 
                        + "\r\nReceived: " + String.Join("\r\n", ResponsesReceived.ToArray()) 
                        + "\r\n" + base.ToString();*/
                    StringBuilder description = new StringBuilder();
                    description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);
                    description.AppendFormat("\r\nSent: {0}", DataSent);
                    if (ResponsesReceived != null)
                        description.AppendFormat("\r\nReceived: {0}\r\n", String.Join("\r\n", ResponsesReceived.ToArray()));

                    if (this.InnerException != null)
                    {
                        description.AppendFormat(" ---> {0}", this.InnerException);
                        description.AppendFormat(
                            "{0}   --- End of inner exception stack trace ---{0}",
                            Environment.NewLine);
                    }

                    description.Append(this.StackTrace);

                    return description.ToString();
                }
            
            }

            /// <summary>
            /// CommandHandler and its children use this exception to indicate the receipt of an error code from the client device
            /// </summary>
            public class ResponseErrorCodeException : ResponseException
            {
                public ResponseErrorCodeException(string message, string sent, string response)
                    : base(message, sent, response)
                {
                }
                public ResponseErrorCodeException(string message, string sent, List<string> responses)
                    : base(message, sent, responses)
                {                    
                }

                public ResponseErrorCodeException(string message, string sent, string response, Exception innerException)
                    : base(message, sent, response, innerException)
                {                 
                }

                public ResponseErrorCodeException(string message, string sent, List<string> responses, Exception innerException)
                    : base(message, sent, responses, innerException)
                {
                }

                public override string ToString()
                {
                    return Message;
                }
            }


            public class UnresponsiveConnectionException : Exception
            {
                public string DataSent;

                public string ExtendedMessage
                {
                    get
                    {
                        return Message + "\r\nSent: " + DataSent;                    
                    }
                }

                public UnresponsiveConnectionException(string message, string sent)
                    : base(message)
                {
                    DataSent = sent;                
                }

                public UnresponsiveConnectionException(string message, string sent, Exception innerException)
                    : base(message, innerException)
                {
                    DataSent = sent;            
                }

                public override string ToString()
                {
                    /*return "Sent: " + DataSent 
                        + "\r\nReceived: " + String.Join("\r\n", ResponsesReceived.ToArray()) 
                        + "\r\n" + base.ToString();*/
                    StringBuilder description = new StringBuilder();
                    description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);
                    description.AppendFormat("\r\nSent: {0}\r\n", DataSent);

                    if (this.InnerException != null)
                    {
                        description.AppendFormat(" ---> {0}", this.InnerException);
                        description.AppendFormat(
                            "{0}   --- End of inner exception stack trace ---{0}",
                            Environment.NewLine);
                    }

                    description.Append(this.StackTrace);

                    return description.ToString();
                }
            }
            #endregion

            /// <summary>
            /// General command handler class. Children classes inherit this class for specific devices.
            /// 
            /// NOTE ON INHERITING THIS CLASS:
            /// children will define specific command functions that pertain to a device
            /// Consider as an example the following command: TST which can take the following
            /// format: TST=xxx (sets parameterOne to xxx), TST? (reads xxx out of TST)
            /// --> the corresponding command functions should be called SetTST and ReadTST
            /// --> (conversely, if TST was a read-only or write-only command the corresponding
            ///      command function would just be called TST)
            /// --> Example SetTST:
            /// public void SetTST(string parameterOne, bool optionalCloseConn = false)
            /// {
            ///     string command = "CBT=" + BNAC_Table.CreateID(ID, idType) + "?";
            ///     List<string> resps = SendCommand(command, 1, optionalCloseConn);
            ///        
            ///     try
            ///     { 
            ///         //Parse the response, if errors occur use only these exceptions:
            ///         //ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
            ///     }
            ///     catch(Exception ex)
            ///     {
            ///         if (!IsStandardException(ex))
            ///             ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);
            ///
            ///         LogMsg(TraceEventType.Warning, ex.ToString());
            ///         throw ex;
            ///     }
            /// }
            /// --> Example ReadTST
            /// public void ReadTST(out string parameterOne, bool optionalCloseConn = false)
            /// {
            ///     string command = "CBT=" + BNAC_Table.CreateID(ID, idType) + "?";
            ///     List<string> resps = SendCommand(command, 1, optionalCloseConn);
            ///        
            ///     try
            ///     { 
            ///         //Parse the response and set parameterOne, if errors occur use only these exceptions:
            ///         //ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
            ///     }
            ///     catch(Exception ex)
            ///     {
            ///         if (!IsStandardException(ex))
            ///             ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);
            ///
            ///         LogMsg(TraceEventType.Warning, ex.ToString());
            ///         throw ex;
            ///     }
            /// }
            /// </summary>
            public class CommandHandler
            {
                public enum LogIDs
                {
                    GenericCommandHandler = 500,
                    WinSIPserver = 501,
                    UCR = 502,
                    NAC = 503
                }

                /// <summary>
                /// This controls the LogID for the command handler, but not the underlying communication interface
                /// </summary>
                public int LogID = (int) LogIDs.GenericCommandHandler;

                
                public TraceSource ts;

                protected StxEtxHandler _stxetxClient = null;

                public StxEtxHandler StxEtxPeer
                {
                    get { return _stxetxClient; }
                }
                
                /// <summary>
                /// constructor for when an STXETX handler is already in place
                /// </summary>
                /// <param name="stxetxClient"></param>
                /// <param name="optionalTS"></param>
                public CommandHandler(StxEtxHandler stxetxClient, TraceSource optionalTS = null)
                {
                    if (optionalTS == null)
                        ts = new TraceSource("dummy");
                    else
                        ts = optionalTS;

                    this._stxetxClient = stxetxClient;
                    
                }

                /// <summary>
                /// Constructor for when a TCP/IP connection must be established
                /// </summary>
                /// <param name="hostname"></param>
                /// <param name="optionalTS"></param>
                /// <param name="optionalPort"></param>
                public CommandHandler(string hostname, TraceSource optionalTS = null, int optionalPort = 1100)
                {
                    if (optionalTS == null)
                        ts = new TraceSource("dummy");
                    else
                        ts = optionalTS;

                    LogMsg(TraceEventType.Information, "Initiating connection to: " + hostname);
                    _stxetxClient = new StxEtxHandler(new TCPconnManager(ts).ConnectToServer(hostname, optionalPort), true);
                    LogMsg(TraceEventType.Information, "Server connection established.");
                }

                

                /// <summary>
                /// Send a command to a STXETX device
                /// </summary>
                /// <param name="command">The command to send</param>
                /// <param name="NumResponses">The number of STXETX replies required</param>
                /// <param name="optionalCloseConn">Set to true if the connection should be closed when this function is done. Default: false</param>
                /// <param name="optionalRetries">Number of retries to get the command sent. Default: 3</param>
                /// <param name="optionalTimeout">Timeout (in seconds) when waiting for an STXETX response. Default: 10 seconds</param>
                /// <returns></returns>
                public List<string> SendCommand(string command, int NumResponses = 0,
                                        bool optionalCloseConn = false, int optionalRetries = 3,
                                        int optionalTimeout = 10)
                {

                    List<string> Responses = new List<string>();

                    try
                    {
                        while (optionalRetries-- > 0)
                        {
                            if (_stxetxClient.SendCommand(command))
                            {
                                string reply = null;
                                bool result = true;
                                int giveUp = NumResponses + 3;
                                while (result && Responses.Count < NumResponses && giveUp-- > 0)
                                {
                                    result = _stxetxClient.ReceiveData(out reply, optionalTimeout * 1000);
                                    if (reply != null)
                                        Responses.Add(reply);

                                }

                                if (Responses.Count == 1)
                                {
                                    string resp = Responses[0];
                                    if (resp == "ERM")
                                        throw new ResponseErrorCodeException("Received an unexpected ERM.", command, Responses);
                                    else if (resp == "ERP")
                                        throw new ResponseErrorCodeException("The user does not have permission to use this command.", command, Responses);
                                    if (resp == "ERL")
                                        throw new ResponseErrorCodeException("The communication link is not suitable for this command.", command, Responses);
                                }
                                    
                                //see if we found all the responses we were looking for
                                if (Responses.Count < NumResponses)
                                {
                                    throw new ResponseException(
                                        "Did not receive the desired number of responses: found "
                                        + Responses.Count.ToString() + " of " + NumResponses.ToString(),
                                        command, Responses);
                                    
                                }
                                break;
                            }
                            else if (optionalRetries < 1)
                            {
                                throw new UnresponsiveConnectionException(
                                    "Failed to send the data: connection is unresponsive.", command);                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when trying to send or receive a command.", command, Responses, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;                         
                    }

                    finally
                    {
                        if (optionalCloseConn == true)
                            try { Dispose(); }
                            catch { }

                    }

                    return Responses;

                }

                


                /// <summary>
                /// returns true if the exception is one of the default exceptions expected
                /// when sending message
                /// </summary>
                /// <param name="ex"></param>
                /// <returns></returns>
                protected static bool IsStandardException(Exception ex)
                {
                    return ( (ex is ResponseException) || (ex is ResponseErrorCodeException) || (ex is UnresponsiveConnectionException) );
                }

                protected void LogMsg(TraceEventType type, string msg)
                {
                    ts.TraceEvent(type, LogID, msg);
                }

                public void Dispose()
                {
                    try
                    {
                        _stxetxClient.Dispose();
                        _stxetxClient = null;
                    }
                    catch
                    {
                    }
                }
            }

            public class WinSIPserver : CommandHandler
            {                

                /// <summary>
                /// constructor for when an STXETX handler is already in place
                /// </summary>
                /// <param name="stxetxClient"></param>
                /// <param name="optionalTS"></param>
                public WinSIPserver(StxEtxHandler stxetxClient, TraceSource optionalTS = null) :base(stxetxClient, optionalTS)
                {
                    LogID = (int) LogIDs.WinSIPserver;
                }

                /// <summary>
                /// Constructor for when a TCP/IP connection must be established
                /// </summary>
                /// <param name="hostname"></param>
                /// <param name="optionalTS">default is null (for no logging)</param>
                /// <param name="optionalPort">default is 1100</param>
                public WinSIPserver(string hostname, TraceSource optionalTS = null, int optionalPort = 1100) : base(hostname, optionalTS, optionalPort)
                {
                    LogID = (int)LogIDs.WinSIPserver;
                }

                #region Specific commands


                #region ADMINISTRATIVE AND MSC COMMANDS
                /// <summary>
                /// Read server settings 
                /// </summary>
                /// <param name="LogUploadPeriod">the period in minutes between log file uploads (set to 0 to get 30 seconds)</param>
                /// <param name="LogMaxSize">the maximum number of entries in the log. Logs are rolling quantities, so when the log is filled and a new event happens, the log will remove the oldest event to make room for the newest event. </param>
                /// <param name="AppTraceLevel">the application trace source level. Set to one of the following case sensitive strings: “Verbose”, “Information”, “Warning”, “Error”, “Critical”</param>
                /// <param name="ComTraceLevel">the trace source level for communication events (STX ETX, new connections, etc.).</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadSSET(out string LogUploadPeriod, out string LogMaxSize, out string AppTraceLevel, out string ComTraceLevel, bool optionalCloseConn = false)
                {
                    int _LogUploadPeriod, _LogMaxSize;
                    ReadSSET(out _LogUploadPeriod, out _LogMaxSize, out AppTraceLevel, out ComTraceLevel, optionalCloseConn);
                    LogUploadPeriod = _LogUploadPeriod.ToString();
                    LogMaxSize = _LogMaxSize.ToString();
                }

                /// <summary>
                /// Read server settings 
                /// </summary>
                /// <param name="LogUploadPeriod">the period in minutes between log file uploads (set to 0 to get 30 seconds)</param>
                /// <param name="LogMaxSize">the maximum number of entries in the log. Logs are rolling quantities, so when the log is filled and a new event happens, the log will remove the oldest event to make room for the newest event. </param>
                /// <param name="AppTraceLevel">the application trace source level. Set to one of the following case sensitive strings: “Verbose”, “Information”, “Warning”, “Error”, “Critical”</param>
                /// <param name="ComTraceLevel">the trace source level for communication events (STX ETX, new connections, etc.).</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadSSET(out int LogUploadPeriod, out int LogMaxSize, out string AppTraceLevel, out string ComTraceLevel, bool optionalCloseConn = false)
                {
                    string command = "SSET?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)                        
                            throw new ResponseException("Invalid response received.", command, resps);
                            
                        Vals = Vals[1].Split(',');

                        if (Vals.Length != 4)
                            throw new ResponseException("Invalid response received.", command, resps);                            
                        
                        LogUploadPeriod = Convert.ToInt32(Vals[0]);
                        LogMaxSize = Convert.ToInt32(Vals[1]);
                        AppTraceLevel = Vals[2];
                        ComTraceLevel = Vals[3];                        
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                }

                /// <summary>
                /// Set server settings 
                ///                 
                /// </summary>
                /// <param name="LogUploadPeriod">the period in minutes between log file uploads (set to 0 to get 30 seconds)</param>
                /// <param name="LogMaxSize">the maximum number of entries in the log. Logs are rolling quantities, so when the log is filled and a new event happens, the log will remove the oldest event to make room for the newest event. </param>
                /// <param name="AppTraceLevel">the application trace source level. Set to one of the following case sensitive strings: “Verbose”, “Information”, “Warning”, “Error”, “Critical”</param>
                /// <param name="ComTraceLevel">the trace source level for communication events (STX ETX, new connections, etc.).</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                /// <param name="optionalRetries">Number of retries allowed</param>
                /// <returns>DON'T USE THE RETURN VALUE, in the future this function will be type void</returns>
                public string SetSSET(int LogUploadPeriod, int LogMaxSize, string AppTraceLevel, string ComTraceLevel, bool optionalCloseConn = false, int optionalRetries = 3)
                {                    
                        return SetSSET(LogUploadPeriod.ToString(), LogMaxSize.ToString(), AppTraceLevel, ComTraceLevel, optionalCloseConn, optionalRetries);                                        
                }

                /// <summary>
                /// Set server settings 
                /// </summary>
                /// <param name="LogUploadPeriod">the period in minutes between log file uploads (set to 0 to get 30 seconds)</param>
                /// <param name="LogMaxSize">the maximum number of entries in the log. Logs are rolling quantities, so when the log is filled and a new event happens, the log will remove the oldest event to make room for the newest event. </param>
                /// <param name="AppTraceLevel">the application trace source level. Set to one of the following case sensitive strings: “Verbose”, “Information”, “Warning”, “Error”, “Critical”</param>
                /// <param name="ComTraceLevel">the trace source level for communication events (STX ETX, new connections, etc.).</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                /// <param name="optionalRetries">Number of retries allowed</param>
                /// <returns>DON'T USE THE RETURN VALUE, in the future this function will be type void</returns>
                public string SetSSET(string LogUploadPeriod, string LogMaxSize, string AppTraceLevel, string ComTraceLevel, bool optionalCloseConn = false, int optionalRetries = 3)
                {
                    string resp;
                    string command = "SSET=" + LogUploadPeriod + ","
                                                        + LogMaxSize + ","
                                                        + AppTraceLevel + ","
                                                        + ComTraceLevel;

                    List<string> resps = SendCommand(command, 1, optionalRetries: optionalRetries);
                    try
                    {
                        if (resps[0].StartsWith("SSET="))
                            resp = resps[0].Substring(5);
                        else                        
                            throw new ResponseException("Invalid response received.", command, resps);                         

                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                    return resp;
                }
                #endregion

                #region BNAC TABLE COMMANDS

                /// <summary>
                /// Read BNAC table entry
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the entry to read</param>
                /// <param name="idType">the type of ID</param>
                /// <param name="tableEntry">the entry found</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>
                /// <returns>DON'T USE THE RETURN VALUE, in the future this function will be type void</returns>
                public string ReadCBT(string ID, BNAC_Table.ID_Type idType, out BNAC_Table.Entry tableEntry, bool optionalCloseConn = false)
                {
                    string command = "CBT=" + BNAC_Table.CreateID(ID, idType) + "?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                        {
                            ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }

                        string response = Vals[1].Trim();
                        if (response == "E")
                            throw new ResponseErrorCodeException("No entry exists for this ID.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);                            
                        else
                            tableEntry = new BNAC_Table.Entry(response.Split(','));
                            
                        return response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Note this function is just obsolete (use ReadCBT(string ID, BNAC_Table.ID_Type idType, out BNAC_Table.Entry tableEntry, bool optionalCloseConn = false) instead)
                /// </summary>
                /// <param name="ID"></param>
                /// <param name="idType"></param>
                /// <param name="optionalCloseConn"></param>
                /// <returns></returns>
                public string ReadCBT(string ID, BNAC_Table.ID_Type idType, bool optionalCloseConn = false)
                {
                    string command = "CBT=" + BNAC_Table.CreateID(ID, idType) + "?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                        {
                            ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }
                        return Vals[1];
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                    
                }
                #endregion

                #region CLIENT SSL/TLS CERTIFICATE COMMANDS

                /// <summary>
                /// Generate a new client certificate pin number. This pin will be valid for a limited time.
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="pinCode">6 digit pin code</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CSP(out string pinCode, bool optionalCloseConn = false)
                {
                    string command = "CSP";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                        {
                            ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }
                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        if (response.Length != CertificateRequestTable.PinCodeLen)
                            throw new ResponseException("Invalid pin code length.", command, resps);
                        try
                        {
                            int testPin = Convert.ToInt32(response.Trim());
                        }
                        catch (Exception e)
                        {
                            ResponseException ex = new ResponseException("Non-numeric pin code received.", command, resps, e);
                        }

                        //passed all checks, pincode must be good:
                        pinCode = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Generate a new client certificate ID number.
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                ///     ArgumentException -- if one of the arguments is invalid
                /// </summary>
                /// <param name="pinCode">6 digit pin code</param>
                /// <param name="machineID">up to 32 character alpha-numeric string descriptive of the client's computer</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CID(string pinCode, string machineID, out string certificateID, bool optionalCloseConn = false)
                {                    
                    if (machineID.Length > CertificateRequestTable.MachineID_MaxLen)
                        throw new ArgumentException("Argument is too long.", "machineID");
                    if (pinCode.Length != CertificateRequestTable.PinCodeLen)
                        throw new ArgumentException("Argument is incorrect length.", "pinCode");

                    string command = "CID=" + pinCode + "," + machineID;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                        {
                            ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }
                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response == "P")
                            throw new ResponseErrorCodeException("The pin code is invalid.", command, resps);

                        if (response.Length != CertificateRequestTable.CertificateID_Len)
                            throw new ResponseException("Invalid certificate ID length.", command, resps);

                        //passed all checks, the certificate ID must be good:
                        certificateID = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                }

                /// <summary>
                /// Upload the certificate signing request
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException                
                /// </summary>
                /// <param name="certificateRequest">a pem formatted certificate signing request (“-----BEGIN CERTIFICATE REQUEST----- ...”) and must have a common name of “PIN[pin]-MID[machine id]-CID[certificate id]”</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CCSR(string certificateRequest, bool optionalCloseConn = false)
                {

                    string command = "CCSR=" + certificateRequest;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                        {
                            ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }
                        string response = Vals[1].Trim();
                        if (response == "C")
                            throw new ResponseErrorCodeException("The certificate request is invalid or does not meet the server's requirements.", command, resps);
                        else if (response == "P")
                            throw new ResponseErrorCodeException("The pin code is invalid.", command, resps);
                        else if (response == "E")
                            throw new ResponseErrorCodeException("The certificate contains incorrect information in the subject line.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseException("Invalid response received.", command, resps);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Download a client’s certificate signing request 
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentException -- if one of the arguments is invalid
                /// </summary>
                /// <param name="pinCode">6 digit pin code</param>
                /// <param name="certificateID">12 character certificate ID</param>
                /// <param name="certificateRequest">a pem formatted certificate signing request (“-----BEGIN CERTIFICATE REQUEST----- ...”) and must have a common name of “PIN[pin]-MID[machine id]-CID[certificate id]”</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CDSR(string pinCode, string certificateID, out string certificateRequest, bool optionalCloseConn = false)
                {
                    if (certificateID.Length != CertificateRequestTable.CertificateID_Len)
                        throw new ArgumentException("Argument is incorrect length.", "certificateID");
                    if (pinCode.Length != CertificateRequestTable.PinCodeLen)
                        throw new ArgumentException("Argument is incorrect length.", "pinCode");

                    string command = "CDSR=" + pinCode + "," + certificateID;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (resps.Count != 1)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = resps[0].Trim().Substring(5);             
                                                
                        if (response == "C")
                            throw new ResponseErrorCodeException("The certificate request has not yet been uploaded. Please trye again later.", command, resps);
                        else if (response == "P")
                            throw new ResponseErrorCodeException("The pin code is invalid.", command, resps);
                        else if (response == "E")
                            throw new ResponseErrorCodeException("No request exists for this certificate ID and pin code combination.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        //okay, the recieved information must be valid
                        certificateRequest = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Upload the signed pem-formatted certificate
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException                
                /// </summary>
                /// <param name="certificateRequest">a pem formatted certificate signing request (“-----BEGIN CERTIFICATE REQUEST----- ...”) and must have a common name of “PIN[pin]-MID[machine id]-CID[certificate id]”</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CUCC(string certificateRequest, bool optionalCloseConn = false)
                {

                    string command = "CUCC=" + certificateRequest;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);                            
                        
                        string response = Vals[1].Trim();
                        if (response == "C")
                            throw new ResponseErrorCodeException("The signed certificate does not match the request.", command, resps);
                        else if (response == "P")
                            throw new ResponseErrorCodeException("The pin code is invalid.", command, resps);
                        else if (response == "E")
                            throw new ResponseErrorCodeException("No certificate is expected for the pin code and certificate ID of this certificate.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseException("Invalid response received.", command, resps);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }


                /// <summary>
                /// Download the signed pem-formatted certificate
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentException -- if one of the arguments is invalid
                /// </summary>
                /// <param name="pinCode">6 digit pin code</param>
                /// <param name="certificateID">12 character certificate ID</param>
                /// <param name="certificate">a pem formatted signed certificate (“-----BEGIN CERTIFICATE----- ...”) and must have a common name of “PIN[pin]-MID[machine id]-CID[certificate id]”</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CDCC(string pinCode, string certificateID, out string certificate, bool optionalCloseConn = false)
                {
                    if (certificateID.Length != CertificateRequestTable.CertificateID_Len)
                        throw new ArgumentException("Argument is incorrect length.", "certificateID");
                    if (pinCode.Length != CertificateRequestTable.PinCodeLen)
                        throw new ArgumentException("Argument is incorrect length.", "pinCode");

                    string command = "CDCC=" + pinCode + "," + certificateID;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (resps.Count != 1)                        
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = resps[0].Trim().Substring(5);
                        if (response == "C")
                            throw new ResponseErrorCodeException("The certificate request has not yet been signed. Please try again later.", command, resps);
                        else if (response == "P")
                            throw new ResponseErrorCodeException("The pin code is invalid.", command, resps);
                        else if (response == "E")
                            throw new ResponseErrorCodeException("No request exists for this certificate ID and pin code combination.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        //okay, the recieved information must be valid
                        certificate = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }                    
                }

                /// <summary>
                /// Clean the client certificate request database. This deletes all expired requests.
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException                
                /// </summary>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CCDB(bool optionalCloseConn = false)
                {                    
                    string command = "CCDB";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)                        
                            throw new ResponseException("Invalid response received.", command, resps);                            
                        
                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseException("Invalid response received.", command, resps);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                }
                #endregion

                #region Server SSL/TLS Certificate Commands

                /// <summary>
                /// Generate a new server private key and certificate signing request. 
                /// NOTE: this is the preferred method to update the server’s certificate
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentNullException -- if CN is null
                /// </summary>
                /// <param name="CN">the common name to use for the server</param>
                /// <param name="certificate_req">the PEM formatted certificate signing request</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CSNR(string CN, out string certificate_req, bool optionalCloseConn = false)
                {
                    if (CN == null)
                        throw new ArgumentNullException("A common name must be specified.", "CN");                    

                    string command = "CSNR=" + CN;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        

                        string response = resps[0].Trim().Substring(5);
                        if (response == "I")
                            throw new ResponseErrorCodeException("The generation request failed due to an internal server error. Try again later.", command, resps);                        
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseErrorCodeException("Received unknown response from the server.", command, resps);
                        
                        //okay, the recieved information must be valid
                        certificate_req = response.Substring(3);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Upload the certificate authority’s response to the certificate signing request
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentNullException
                /// </summary>
                /// <param name="certificate">the PEM formatted response for the certificate authority (“-----BEGIN CERTIFICATE----- ...”)</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void SetCSRR(string certificate, bool optionalCloseConn = false)
                {
                    if (certificate == null)
                        throw new ArgumentNullException("A certificate must be included.", "certificate");

                    string command = "CSRR=" + certificate;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();                        
                        if (response == "I")
                            throw new ResponseErrorCodeException("The generation request failed due to an internal server error. Try again later.", command, resps);
                        else if (response == "C")
                            throw new ResponseErrorCodeException("The certificate was invalid or did not meet the server's requirements.", command, resps);
                        else if (response == "E")
                            throw new ResponseErrorCodeException("There is no record of CSNR having been run.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseErrorCodeException("Received unknown response from the server.", command, resps);

                        //okay, the information was received by the server
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Download the server’s public certificate
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                /// </summary>                
                /// <param name="certificate">the PEM formatted certificate</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadCSRR(out string certificate, bool optionalCloseConn = false)
                {

                    string command = "CSRR?";

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {                        


                        string response = resps[0].Trim().Substring(5);
                        if (response == "I")
                            throw new ResponseErrorCodeException("Failed to download the certificate due to an internal server error. Try again later.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);                        

                        //okay, the recieved information must be valid
                        certificate = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Upload the chain of certificates that have signed the server’s certificate
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentNullException
                /// </summary>
                /// <param name="signers">The PEM formatted list of certificates that appear in the chain of signers for the server’s certificate (“-----BEGIN CERTIFICATE----- ...”)</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void SetCSS(string signers, bool optionalCloseConn = false)
                {
                    if (signers == null)
                        throw new ArgumentNullException("A certificate must be included.", "signers");

                    string command = "CSS=" + signers;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "I")
                            throw new ResponseErrorCodeException("The operation failed due to an internal server error. Try again later.", command, resps);
                        else if (response == "C")
                            throw new ResponseErrorCodeException("This signer certificate chain is invalid.", command, resps); //It may just mean that these signers don't sign the server cert
                        else if (response == "E")
                            throw new ResponseErrorCodeException("The server does not currently have a certificate.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseErrorCodeException("Received unknown response from the server.", command, resps);

                        //okay, the information was received by the server
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Download the server’s certificate signing chain
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                /// </summary>                
                /// <param name="signers">the PEM formatted certificate chain</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadCSS(out string signers, bool optionalCloseConn = false)
                {

                    string command = "CSS?";

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string response = resps[0].Trim().Substring(4);
                        if (response == "I")
                            throw new ResponseErrorCodeException("Failed to download the server certificate chain due to an internal server error. Try again later.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        //okay, the recieved information must be valid
                        signers = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                /// Upload the chain of certificates that have signed the WinSIP clients’ certificates
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentNullException
                /// </summary>
                /// <param name="signers">The PEM formatted list of certificates that appear in the chain of signers for the server’s certificate (“-----BEGIN CERTIFICATE----- ...”)</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void SetCWS(string signers, bool optionalCloseConn = false)
                {
                    if (signers == null)
                        throw new ArgumentNullException("A certificate chain must be included.", "signers");

                    string command = "CWS=" + signers;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "I")
                            throw new ResponseErrorCodeException("The operation failed due to an internal server error. Try again later.", command, resps);
                        else if (response == "C")
                            throw new ResponseErrorCodeException("This certificate chain is invalid.", command, resps); //It may just mean that these signers don't sign each other                        
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseErrorCodeException("Received unknown response from the server.", command, resps);

                        //okay, the information was received by the server
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                /// <summary>
                ///  Download the WinSIP clients’ certificate signing chain
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                /// </summary>                
                /// <param name="signers">the PEM formatted certificate chain</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadCWS(out string signers, bool optionalCloseConn = false)
                {

                    string command = "CWS?";

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string response = resps[0].Trim().Substring(4);
                        if (response == "I")
                            throw new ResponseErrorCodeException("Failed to download the certificate chain due to an internal server error. Try again later.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        //okay, the recieved information must be valid
                        signers = response;
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }


                /// <summary>
                /// Upload a complete server certificate (private key included). 
                /// NOTE: the use of this command is not recommended. 
                /// CSNR should be used whenever possible as it is more secure.
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                ///     ArgumentNullException
                /// </summary>
                /// <param name="password">Used for opening the pfx file.</param>                
                /// <param name="cert">A base-64 encoded pfx (PKCS12) file containing the certificate </param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void CSFU(string password, string cert, bool optionalCloseConn = false)
                {
                    if (password == null)
                        throw new ArgumentNullException("A certificate chain must be included.", "password");
                    if (cert == null)
                        throw new ArgumentNullException("A certificate chain must be included.", "cert");

                    string command = "CSFU=" + password + "," + cert;

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "I")
                            throw new ResponseErrorCodeException("The operation failed due to an internal server error. Try again later.", command, resps);
                        else if (response == "C")
                            throw new ResponseErrorCodeException("Invalid certificate.", command, resps); 
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseErrorCodeException("Received unknown response from the server.", command, resps);

                        //okay, the information was received by the server
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }


                /// <summary>
                /// Update the certificate information changed flag to the current time.
                /// This forces all instances of the BNAC server to refresh their certificate information.
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException                
                /// </summary>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void SendCSU(bool optionalCloseConn = false)
                {
                    string command = "CSU=1";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseException("Invalid response received.", command, resps);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                }


                /// <summary>
                ///  Retrieve the last time that the certificates were updated
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException  
                /// </summary>                
                /// <param name="lastUpdate">the last time that the server's certificate information was updated</param>                
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void ReadCSU(out DateTime lastUpdate, bool optionalCloseConn = false)
                {
                    string command = "CSU?";

                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string response = resps[0].Trim().Substring(4);
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);

                        //okay, the recieved information must be valid
                        lastUpdate = DateTime.Parse(response);                                                
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }

                #endregion

                #region User database commands
                /// <summary>
                /// Authenticate user (user login command)
                /// 
                /// Exceptions thrown: 
                ///     ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException,
                ///     ArgumentNullException
                /// </summary>  
                /// <param name="userName">the user name</param>
                /// <param name="password">the users password</param>
                /// <param name="optionalCloseConn">Set to true to close the connection after executing the command</param>
                public void UDA(string userName, SecureString password, bool optionalCloseConn = false)
                {
                    string command = "UDA=" + userName + "," + CStoredCertificate.ConvertToUnsecureString(password);
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "LOCKED")
                            throw new ResponseErrorCodeException("This user account is locked out.", command, resps);
                        if (response == "REVOKED")
                            throw new ResponseErrorCodeException("This user account has been revoked.", command, resps);
                        if (response == "REJECT")
                            throw new ResponseErrorCodeException("Bad username or password.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (response != "OK")
                            throw new ResponseException("Invalid response received.", command, resps);
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when interpretting the response.", command, resps, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                }

                #endregion

                #endregion

            }

        
        }
    }
}
