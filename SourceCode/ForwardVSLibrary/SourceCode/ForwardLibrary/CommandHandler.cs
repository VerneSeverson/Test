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


            public interface ICommandHandler
            {
                int LogID
                {
                    get;
                    set;
                }

                TraceSource ts
                {
                    get;
                    set;
                }

                IProtocolHandler ProtocolHandler
                {
                    get;
                }

                /// <summary>
                /// Send a command
                /// </summary>
                /// <param name="command">The command to send</param>
                /// <param name="NumResponses">The number of replies required (as defined by protocol handler)</param>
                /// <param name="optionalCloseConn">Set to true if the connection should be closed when this function is done. Default: false</param>
                /// <param name="optionalRetries">Number of retries to get the command sent. Default: 3 (if supported by protocol handler)</param>
                /// <param name="optionalTimeout">Timeout (in seconds) when waiting for an STXETX response. Default: 10 seconds (if supported by protocol handler)</param>
                /// <returns></returns>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                List<string> SendCommand(string command, int NumResponses = 0,
                                        bool optionalCloseConn = false, int optionalRetries = 3,
                                        int optionalTimeout = 10);
            }

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
            public class CommandHandler : ICommandHandler
            {
                public enum LogIDs
                {
                    GenericCommandHandler = 500,
                    WinSIPserver = 501,
                    UCR = 502,
                    UNAC = 503
                }

                private int _LogID = (int)LogIDs.GenericCommandHandler;
                /// <summary>
                /// This controls the LogID for the command handler, but not the underlying communication interface
                /// </summary>
                public int LogID
                {
                    get { return _LogID; }
                    set { _LogID = value; }
                }


                public TraceSource ts
                {
                    get;
                    set;
                }

                protected IProtocolHandler _ProtocolHandler = null;

                public IProtocolHandler ProtocolHandler
                {
                    get { return _ProtocolHandler; }
                }
                
                /// <summary>
                /// constructor for when an STXETX handler is already in place
                /// </summary>
                /// <param name="stxetxClient"></param>
                /// <param name="optionalTS"></param>
                public CommandHandler(IProtocolHandler client, TraceSource optionalTS = null)
                {
                    if (optionalTS == null)
                        ts = new TraceSource("dummy");
                    else
                        ts = optionalTS;

                    this._ProtocolHandler = client;
                    
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
                    _ProtocolHandler = new StxEtxHandler(new TCPconnManager(ts).ConnectToServer(hostname, optionalPort), true);
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
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public List<string> SendCommand(string command, int NumResponses = 0,
                                        bool optionalCloseConn = false, int optionalRetries = 3,
                                        int optionalTimeout = 10)
                {

                    List<string> Responses = new List<string>();
                    
                    try
                    {
                        while (optionalRetries-- > 0)
                        {
                            if (_ProtocolHandler.CommContext.bConnected == false)
                                throw new UnresponsiveConnectionException("Connection has disconnected.", command);

                            if (_ProtocolHandler.SendCommand(command))
                            {
                                string reply = null;
                                bool result = true;
                                int giveUp = NumResponses + 3;
                                while (result && Responses.Count < NumResponses && giveUp-- > 0)
                                {
                                    result = _ProtocolHandler.ReceiveData(out reply, optionalTimeout * 1000);
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
                        _ProtocolHandler.Dispose();
                        _ProtocolHandler = null;
                    }
                    catch
                    {
                    }
                }
            }

            public class UNAC : CommandHandler
            {
                /// <summary>
                /// constructor for when an STXETX handler is already in place
                /// </summary>
                /// <param name="stxetxClient"></param>
                /// <param name="optionalTS"></param>
                public UNAC(IProtocolHandler client, TraceSource optionalTS = null)
                    : base(client, optionalTS)
                {
                    LogID = (int)LogIDs.UNAC;                    
                }

                #region 3.1 Set NAC Configuration - Server I/F
                /// <summary>
                /// Class used to hold data of RSR command
                /// </summary>
                public class SETH_Data
                {

                    /// <summary>
                    /// Table entry
                    /// "0" - "3" ("0" is always the default settlement host.)
                    /// </summary>                    
                    public string n
                    {
                        get { return _n; }
                        set
                        {
                            int num = int.Parse(value); //will throw exceptions if not numeric
                            if ((num < 0) || (num > 3))
                                throw new ArgumentOutOfRangeException("n", "The table entry must be between 0 and 3.");
                            _n = value;
                        }
                    }

                    /// <summary>
                    /// Host type.
                    /// "0" - SPS / ADS
                    /// "1" - EasyHost (with or without Alt. Track support).
                    /// "2" - Paymentech
                    /// "3" - Global
                    /// "5" - First Data (Reserved but not functional in UN02.00)
                    /// </summary>                    
                    public string s0
                    {
                        get { return _p[0]; }
                        set
                        {
                            int num = int.Parse(value); //will throw exceptions if not numeric                            
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Primary telephone number.
                    /// ASCII telephone number.  The length of the string can be up to 26 characters.
                    /// The first character of the string can be "T" for tone dialing and "P" for pulse dialing.  
                    /// If none is specified, tone dialing is set.
                    /// Each "D" or "d" character embedded within this string causes a 2 second delay.  
                    /// The "-" character can be embedded and has no effect.  
                    /// Each "W" embedded within this string will cause the modem dialing to pause and wait for dial tone.
                    /// </summary>                    
                    public string s1
                    {
                        get { return _p[1]; }
                        set
                        {
                            if (value.Length > 26)
                                throw new ArgumentOutOfRangeException("s1", "The primary telephone number has a maximum length of 26 characters.");
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Secondary telephone number.
                    /// ASCII telephone number.  The length of the string can be up to 26 characters.
                    /// The first character of the string can be "T" for tone dialing and "P" for pulse dialing.  
                    /// If none is specified, tone dialing is set.
                    /// Each "D" or "d" character embedded within this string causes a 2 second delay.  
                    /// The "-" character can be embedded and has no effect.  
                    /// Each "W" embedded within this string will cause the modem dialing to pause and wait for dial tone.
                    /// </summary>    
                    public string s2
                    {
                        get { return _p[2]; }
                        set
                        {
                            if (value.Length > 26)
                                throw new ArgumentOutOfRangeException("s2", "The secondary telephone number has a maximum length of 26 characters.");
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s3
                    {
                        get { return _p[3]; }
                        set
                        {                            
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s4
                    {
                        get { return _p[4]; }
                        set
                        {
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s5
                    {
                        get { return _p[5]; }
                        set
                        {
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s6
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s7
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Misc. host-specific parameter
                    /// </summary>
                    public string s8
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Synchronize system clock with host clock. (Added UN02.51)
                    /// Blank = no synchronization (starting in UN02.51) (default)
                    /// +23 to -23 = Enable sync +/- 23 hours (sign character required)
                    /// </summary>
                    public string s9
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    /// <summary>
                    /// Host Modem baud rate (Added in UN01.11).
                    /// This baud rate is selected every time this host is dialed. 
                    /// This baud rate is used for both the primary and secondary host numbers. 
                    /// The value can be 300, 1200 or 2400 (default). If blank, then 2400 baud is used.
                    /// </summary>
                    public string s10
                    {
                        get { return _p[10]; }
                        set
                        {
                            _p[10] = value;
                        }
                    }

                    private string[] _p = new string[11];
                    private string _n = "";

                    /// <summary>
                    /// Creates and populates a SETH_Data object
                    /// 
                    /// </summary>
                    /// <param name="incoming">Expecting: "n=s0,s1,s2,..."</param>
                    /// <returns></returns>
                    public static SETH_Data Parse(string incoming)
                    {
                        /*string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");*/
                        string[] nf = incoming.Split('=');
                        string the_n = nf[0];
                        incoming = nf[1];


                        string[] fields = SeparateFields(incoming);

                        SETH_Data response = new SETH_Data
                        {
                            n = the_n,
                            s0 = fields[0].Trim(),
                            s1 = fields[1].Trim(),
                            s2 = fields[1].Trim(),
                            s3 = fields[2].Trim(),
                            s4 = fields[3].Trim(),
                            s5 = fields[4].Trim(),
                            s6 = fields[5].Trim(),
                            s7 = fields[6].Trim(),
                            s8 = fields[7].Trim(),
                            s9 = fields[8].Trim(),
                            s10 = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                    
                    /// <summary>
                    /// custom parsing because of this note in the spec:
                    /// When sending a list of parameters, use "" (quote-quote) in the fields (between the comma delimiters) you do not want modified.
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    private static string[] SeparateFields(string incoming)
                    {
                        List<string> fields = new List<string>();
                        StringBuilder accum = new StringBuilder();
                        bool inQuote = false;
                        foreach (char c in incoming)
                        {
                            if (c == ',')
                            {
                                if (!inQuote)
                                {
                                    fields.Add(accum.ToString());
                                    accum = new StringBuilder();
                                }
                            }
                            accum.Append(c);
                            if (c == '"')
                                inQuote = !inQuote;
                        }
                        fields.Add(accum.ToString());
                        return fields.ToArray();
                    }
                }

                /// <summary>
                /// Get UNAC Parameter List.
                /// 
                /// This causes the UNAC to return its list of basic parameters.
                ///                 
                /// </summary>
                /// <param name="prm">the PRM response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void SetNSETH(int table_entry, out SETH_Data seth, bool optionalCloseConn = false)
                {
                    string command = "NSETH" + table_entry;
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {                        
                        seth = SETH_Data.Parse(resps[0].Substring(4));
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
                #region 3.2 Retrieve NAC Config Settings - Server I/F.
                /// <summary>
                /// Class used to hold data of PRM command
                /// </summary>
                public class PRM_Data
                {
                    
                    /// <summary>
                    /// Primary Telephone Number for Settlement Host (#0). 
                    /// (First character is "T" or "P" for tone or pulse dialing.  Pause character is "d".)  
                    /// Max length is 26 characters.
                    /// </summary>                    
                    public string p0
                    {
                        get { return _p[0]; }
                        set
                        {
                            if (value.Length > 26)
                                throw new ArgumentOutOfRangeException("p0", "Exceeded maximum field length of 26 characters");
                            else
                                _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Secondary Telephone Number for Settlement Host (#0). 
                    /// (First character is "T" or "P" for tone or pulse dialing.  Pause character is "d".)  
                    /// Max length is 26 characters.
                    /// </summary>
                    public string p1
                    {
                        get { return _p[1]; }
                        set
                        {
                            if (value.Length > 26)
                                throw new ArgumentOutOfRangeException("p1", "Exceeded maximum field length of 26 characters");
                            else
                                _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Terminal ID Number for Settlement Host (#0). Max. 20 characters).
                    /// </summary>
                    public string p2
                    {
                        get { return _p[2]; }
                        set
                        {
                            if (value.Length > 20)
                                throw new ArgumentOutOfRangeException("p2", "Exceeded maximum field length of 20 characters");
                            else
                                _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Company ID Number for Settlement Host (#0). Max. 20 characters).
                    /// </summary>
                    public string p3
                    {
                        get { return _p[3]; }
                        set
                        {
                            if (value.Length > 26)
                                throw new ArgumentOutOfRangeException("p3", "Exceeded maximum field length of 20 characters");
                            else
                                _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Batch Close Time "hhmm".
                    /// </summary>
                    public string p4
                    {
                        get { return _p[4]; }
                        set
                        {
                            ValidateHHMMstr(value);                            
                            _p[4] = value;                            
                        }
                    }

                    /// <summary>
                    /// Send Time 1 "hhmm".
                    /// </summary>
                    public string p5
                    {
                        get { return _p[5]; }
                        set
                        {
                            ValidateHHMMstr(value);                               
                            _p[5] = value;                            
                        }
                    }

                    /// <summary>
                    /// Send Time 2 "hhmm".
                    /// </summary>
                    public string p6
                    {
                        get { return _p[6]; }
                        set
                        {
                            ValidateHHMMstr(value);
                            _p[6] = value;   
                        }
                    }

                    /// <summary>
                    /// Send Time 3 "hhmm".
                    /// </summary>
                    public string p7
                    {
                        get { return _p[7]; }
                        set
                        {
                            ValidateHHMMstr(value);
                            _p[7] = value;   
                        }
                    }

                    /// <summary>
                    /// UNAC Software Version (8 characters max.).
                    /// </summary>
                    public string p8
                    {
                        get { return _p[8]; }
                        set
                        {
                            if (value.Length > 8)
                                throw new ArgumentOutOfRangeException("p8", "Exceeded maximum field length of 8 characters");
                            else
                                _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// UNAC Software part number (8 characters max.).
                    /// </summary>
                    public string p9
                    {
                        get { return _p[9]; }
                        set
                        {
                            if (value.Length > 8)
                                throw new ArgumentOutOfRangeException("p9", "Exceeded maximum field length of 8 characters");
                            else
                                _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates an 
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PRM_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields, expecting 10 found " + fields.Length);

                        PRM_Data response = new PRM_Data
                        {
                            p0 = fields[0].Trim(),
                            p1 = fields[1].Trim(),
                            p2 = fields[2].Trim(),
                            p3 = fields[3].Trim(),
                            p4 = fields[4].Trim(),
                            p5 = fields[5].Trim(),
                            p6 = fields[6].Trim(),
                            p7 = fields[7].Trim(),
                            p8 = fields[8].Trim(),
                            p9 = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p0,p1,p2,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }

                    #region private helper functions
                    /// <summary>
                    /// Throw an exception if the string is not valid
                    /// </summary>
                    /// <param name="hhmm"></param>
                    private void ValidateHHMMstr(string hhmm)
                    {
                        if (hhmm.Length != 4)
                            throw new ArgumentOutOfRangeException("hhmm", "The field length is not the required length of 4 characters.");

                        int hh = int.Parse(hhmm.Substring(0, 2));
                        if ( (hh > 23) || (hh < 0) )
                            throw new ArgumentOutOfRangeException("hhmm", "The field indicates an invalid time.");

                        int mm = int.Parse(hhmm.Substring(2, 2));
                        if ( (mm > 59) || (mm < 0) )
                            throw new ArgumentOutOfRangeException("hhmm", "The field indicates an invalid time.");
                    }
                    #endregion

                }

                /// <summary>
                /// Get UNAC Parameter List.
                /// 
                /// This causes the UNAC to return its list of basic parameters.
                ///                 
                /// </summary>
                /// <param name="prm">the PRM response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPRM(out PRM_Data prm, bool optionalCloseConn = false)
                {
                    string command = "NPRM";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        prm = PRM_Data.Parse(Vals[1]);
                        
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
                /// Class used to hold data of PR1 command
                /// </summary>
                public class PR1_Data
                {

                    /// <summary>
                    /// "E" for normal UNAC operation.
                    /// "B" for unlimited demo mode.
                    /// "P" for Production Test Mode.
                    /// "T" for Engineering Test Mode.
                    /// </summary>                    
                    public string d
                    {
                        get { return _p[0]; }
                        set
                        {                            
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// # of rings to answer incoming call ("1" to "14").
                    /// </summary>
                    public string r
                    {
                        get { return _p[1]; }
                        set
                        {
                            int rings = int.Parse(value); //will throw exceptions if not numeric
                            if ((rings < 1) || (rings > 14))
                                throw new ArgumentOutOfRangeException("r", "The number of rings to answer must have a value between 1 and 14");

                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Type of concatenation.
                    /// "D" for daily.
                    /// "W" for weekly.
                    /// "M" for monthly.
                    /// NULL for none.
                    /// </summary>
                    public string t
                    {
                        get { return _p[2]; }
                        set
                        {                            
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// v1 (optional). 
                    /// Specifies days of week ("0" to "6") or days of month ("1" to "31") the concatenated file is to be sent to host. 
                    /// NULL when field not used.
                    /// </summary>
                    public string v1
                    {
                        get { return _p[3]; }
                        set
                        {
                            if ( (value != null) && (value.Length > 0) )
                                int.Parse(value); //will throw exceptions if not numeric
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// v2 (optional). 
                    /// Specifies days of week ("0" to "6") or days of month ("1" to "31") the concatenated file is to be sent to host. 
                    /// NULL when field not used.
                    /// </summary>
                    public string v2
                    {
                        get { return _p[4]; }
                        set
                        {
                            if ((value != null) && (value.Length > 0))
                                int.Parse(value); //will throw exceptions if not numeric
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// v3 (optional). 
                    /// Specifies days of week ("0" to "6") or days of month ("1" to "31") the concatenated file is to be sent to host. 
                    /// NULL when field not used.
                    /// </summary>
                    public string v3
                    {
                        get { return _p[5]; }
                        set
                        {
                            if ((value != null) && (value.Length > 0))
                                int.Parse(value); //will throw exceptions if not numeric
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p6
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p7
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p8
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p9
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR3_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PR1_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length < 3)
                            throw new ArgumentOutOfRangeException("incoming", "Too few fields. Expecting at least 3, found " + fields.Length + ".");

                        PR1_Data response = new PR1_Data
                        {
                            d = fields[0].Trim(),
                            r = fields[1].Trim(),
                            t = fields[2].Trim()
                        };

                        if (fields.Length > 3)
                            response.v1 = fields[3];
                        if (fields.Length > 4)
                            response.v2 = fields[4];
                        if (fields.Length > 5)
                            response.v3 = fields[5];

                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Modem and Concatenate Status.
                /// This causes the status of the modem control and concatenation control functions to be returned.
                /// 
                /// </summary>
                /// <param name="pr1">the PR1 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPR1(out PR1_Data pr1, bool optionalCloseConn = false)
                {
                    string command = "NPR1";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pr1 = PR1_Data.Parse(Vals[1]);

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
                /// Class used to hold data of PR2 command
                /// </summary>
                public class PR2_Data
                {

                    /// <summary>
                    /// "N" for not in Portable UNAC Mode.                    
                    /// </summary>                    
                    public string p
                    {
                        get { return _p[0]; }
                        set
                        {
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// "E" for beeps enabled
                    /// "D" for beeps disabled
                    /// </summary>
                    public string b
                    {
                        get { return _p[1]; }
                        set
                        {
                            if ((value != "E") && (value != "D"))
                                throw new ArgumentOutOfRangeException("b", "Beeps must be either set to E or D");
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p2
                    {
                        get { return _p[2]; }
                        set
                        {
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p3
                    {
                        get { return _p[3]; }
                        set
                        {                            
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p4
                    {
                        get { return _p[4]; }
                        set
                        {                            
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p5
                    {
                        get { return _p[5]; }
                        set
                        {                            
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p6
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p7
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p8
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p9
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR2_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PR2_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        PR2_Data response = new PR2_Data
                        {
                            p = fields[0].Trim(),
                            b = fields[1].Trim(),
                            p2 = fields[2].Trim(),
                            p3 = fields[3].Trim(),
                            p4 = fields[4].Trim(),
                            p5 = fields[5].Trim(),
                            p6 = fields[6].Trim(),
                            p7 = fields[7].Trim(),
                            p8 = fields[8].Trim(),
                            p9 = fields[9].Trim()
                        };

                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Portable UNAC and Beep Control Status.
                /// This causes the status of the portable mode operation and the beep control function to be returned.
                /// 
                /// </summary>
                /// <param name="pr2">the PR2 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPR2(out PR2_Data pr2, bool optionalCloseConn = false)
                {
                    string command = "NPR2";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pr2 = PR2_Data.Parse(Vals[1]);

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
                /// Class used to hold data of PR3 command
                /// </summary>
                public class PR3_Data
                {

                    /// <summary>
                    /// Number of host attempts currently set (="0").
                    /// </summary>                    
                    public string ha
                    {
                        get { return _p[0]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Number of months in hot list.
                    /// </summary>
                    public string hlm
                    {
                        get { return _p[1]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Number of entries in hot list.
                    /// </summary>
                    public string hnbr
                    {
                        get { return _p[2]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Number of entries in batch.
                    /// </summary>
                    public string bnbr
                    {
                        get { return _p[3]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Number of entries in message file.
                    /// </summary>
                    public string mnbr
                    {
                        get { return _p[4]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p5
                    {
                        get { return _p[5]; }
                        set
                        {
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p6
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p7
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p8
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string p9
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR3_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PR3_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        PR3_Data response = new PR3_Data
                        {
                            ha = fields[0].Trim(),
                            hlm = fields[1].Trim(),
                            hnbr = fields[2].Trim(),
                            bnbr = fields[3].Trim(),
                            mnbr = fields[4].Trim(),
                            p5 = fields[5].Trim(),
                            p6 = fields[6].Trim(),
                            p7 = fields[7].Trim(),
                            p8 = fields[8].Trim(),
                            p9 = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Host Retries, Hot List, and Batch Status.
                /// This causes the following status to be returned.
                /// 
                /// </summary>
                /// <param name="pr3">the PR3 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPR3(out PR3_Data pr3, bool optionalCloseConn = false)
                {
                    string command = "NPR3";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pr3 = PR3_Data.Parse(Vals[1]);

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
                /// Class used to hold data of PR7 command
                /// </summary>
                public class PR7_Data
                {

                    /// <summary>
                    /// Settlement Host Protocol Type
                    /// "0" 	- SPS / ADS
                    /// "1"     - EasyHost (with or without Alt. Track support)
                    /// "2"     - Paymentech
                    /// "3"     - Global
                    /// "5"     - First Data (Reserved)
                    /// </summary>                    
                    public string p
                    {
                        get { return _p[0]; }
                        set
                        {
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// This is the number of transactions remaining in Production Test Mode.  
                    /// This will be blank if not in Production Test Mode. (the range can be from "0" to "99").
                    /// NULL or "0" 	- Not supported
                    /// </summary>
                    public string t
                    {
                        get { return _p[1]; }
                        set
                        {
                            if (value != null)
                            {
                                int num = int.Parse(value);
                                if ((num > 99) || (num < 0))
                                    throw new ArgumentOutOfRangeException("t", "Invalid number of transactions remaining.");
                            }
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// undefined
                    /// </summary>
                    public string c
                    {
                        get { return _p[2]; }
                        set
                        {
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// undefined
                    /// </summary>
                    public string d
                    {
                        get { return _p[3]; }
                        set
                        {
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// undefined
                    /// </summary>
                    public string e
                    {
                        get { return _p[4]; }
                        set
                        {
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// undefined
                    /// </summary>
                    public string f
                    {
                        get { return _p[5]; }
                        set
                        {
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// undefined
                    /// </summary>
                    public string g
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string h
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string i
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string j
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR7_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PR7_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        PR7_Data response = new PR7_Data
                        {
                            p = fields[0].Trim(),
                            t = fields[1].Trim(),
                            c = fields[2].Trim(),
                            d = fields[3].Trim(),
                            e = fields[4].Trim(),
                            f = fields[5].Trim(),
                            g = fields[6].Trim(),
                            h = fields[7].Trim(),
                            i = fields[8].Trim(),
                            j = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Host Configuration Status.
                /// This causes the various settings that control operation of the host communication port to be returned.
                /// 
                /// </summary>
                /// <param name="pr7">the PR7 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPR7(out PR7_Data pr7, bool optionalCloseConn = false)
                {
                    string command = "NPR7";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pr7 = PR7_Data.Parse(Vals[1]);

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
                /// Class used to hold data of PRF command
                /// </summary>
                public class PRF_Data
                {

                    /// <summary>
                    /// Offline Proprietary Card Feature.
                    /// NULL or "0" 	- Not supported
                    /// </summary>                    
                    public string a
                    {
                        get { return _p[0]; }
                        set
                        {
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Portable NAC mode.
                    /// NULL or "0" 	- Not supported
                    /// </summary>
                    public string b
                    {
                        get { return _p[1]; }
                        set
                        {
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// UNAC System Query Reporting Feature
                    /// NULL or "0" 	- Not supported
                    /// </summary>
                    public string c
                    {
                        get { return _p[2]; }
                        set
                        {
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Supported Settlement Host Protocols.
                    /// NULL or "0"	- ADS
                    /// "1"		- EasyHost Protocol & SPS
                    /// "2"		- ADS, EZH, & Paymentech
                    /// "3" 	- ADS, EZH, Paymentech, & Global
                    /// "4"		- EZH
                    /// "5"		- EZH, Global
                    /// "6"		- ADS, EZH with Alt. Track support
                    /// "7"		- EZH with Alt. Track, Paymentech, & Global
                    /// "8"		- EZH with Alt. Track & Paymentech
                    /// "1xxx"	- ASCII Hex bit map - See following table (Added UN02.00)
                    /// </summary>
                    public string d
                    {
                        get { return _p[3]; }
                        set
                        {
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Supported Authorization Host Protocols.
                    /// NULL	- Host types same as d, above.
                    /// "0"		- ADS
                    /// "1"		- EasyHost Protocol & SPS
                    /// "2"		- ADS, EZH, & Paymentech
                    /// "3" 	- ADS, EZH, Paymentech, & Global
                    /// "4"		- EZH
                    /// "5"		- EZH, Global
                    /// "6"		- ADS, EZH with Alt. Track support
                    /// "7"		- EZH with Alt. Track, Paymentech, & Global
                    /// "8"		- EZH with Alt. Track & Paymentech
                    /// "1xxx"	- ASCII Hex bit map - See following table (Added UN02.00)
                    /// </summary>
                    public string e
                    {
                        get { return _p[4]; }
                        set
                        {
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// UNAC LAN Support
                    /// Null or "0"	- No LAN
                    /// "1"		- LAN
                    /// </summary>
                    public string f
                    {
                        get { return _p[5]; }
                        set
                        {
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Alternate Settlement Host Support (Added UN02.00)
                    /// Null or "0"	- No Alt. Host support.
                    /// "1"		- Alt. Host supported.
                    /// </summary>
                    public string g
                    {
                        get { return _p[6]; }
                        set
                        {
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string h
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string i
                    {
                        get { return _p[8]; }
                        set
                        {
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Undefined
                    /// </summary>
                    public string j
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PRF_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PRF_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields, expecting 10 found " + fields.Length);

                        PRF_Data response = new PRF_Data
                        {
                            a = fields[0].Trim(),
                            b = fields[1].Trim(),
                            c = fields[2].Trim(),
                            d = fields[3].Trim(),
                            e = fields[4].Trim(),
                            f = fields[5].Trim(),
                            g = fields[6].Trim(),
                            h = fields[7].Trim(),
                            i = fields[8].Trim(),
                            j = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "a,b,c,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get List of UNAC Features.
                /// There are various different compiled versions of the Version 1 UNAC.  
                /// The supported features in each version of UNAC can be identified as follows: Command added at UN01.04.
                /// 
                /// </summary>
                /// <param name="prf">the PRF response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPRF(out PRF_Data prf, bool optionalCloseConn = false)
                {
                    string command = "NPRF";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        prf = PRF_Data.Parse(Vals[1]);

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
                /// Class used to hold data of UDT command
                /// </summary>
                public class UDT_Data
                {

                    /// <summary>
                    /// mmddyyhhjjw string 
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// </summary>                    
                    public string datetimestring
                    {
                        get { return _datetimestring; }
                        set
                        {
                            ConvertUNAC_DateTime(value); //will throw exceptions if not proper
                            _datetimestring = value;
                        }
                    }

                    public DateTime UNAC_DateTime
                    {
                        get { return ConvertUNAC_DateTime(datetimestring); }
                        set { _datetimestring = ConvertDateTimeToUNAC_String(value);  }
                    }


                    private string _datetimestring;
                    /// <summary>
                    /// Creates and populates a PR3_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static UDT_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 1)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 1, found " + fields.Length + ".");

                        UDT_Data response = new UDT_Data
                        {
                            datetimestring = fields[0].Trim(),                            
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return datetimestring;
                    }

                    #region private helper functions
                    /// <summary>
                    /// Converts mmddyyhhiiw string to DateTime
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// </summary>
                    /// <param name="datetime"></param>
                    /// <returns></returns>
                    static public DateTime ConvertUNAC_DateTime(string datetime)
                    {
                        if (datetime.Length != "mmddyyhhjjw".Length)
                            throw new ArgumentException("Invalid date time string.", datetime);

                        //add year 2000 padding
                        datetime = datetime.Substring(0, "mmdd".Length) + "20" + datetime.Substring("mmdd".Length);

                        //remove day of week
                        datetime = datetime.Substring(0, "mmddyyyyhhii".Length);

                        //see: http://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
                        return DateTime.ParseExact(datetime, "MMddyyyyHHmm", null); //, CultureInfo.InvariantCulture);
                    }

                    /// <summary>
                    /// Converts mmddyyhhiiw string to DateTime
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// </summary>
                    /// <param name="datetime"></param>
                    /// <returns></returns>
                    public static string ConvertDateTimeToUNAC_String(DateTime date)
                    {
                        string Month, Day, Year, Hour, Minute, DayOfWeek;

                        Month = date.Month.ToString();
                        if (Month.Length < 2)
                            Month = "0" + Month;

                        Day = date.Day.ToString();
                        if (Day.Length < 2)
                            Day = "0" + Day;

                        Year = date.Year.ToString();
                        if (Year.Length == 4)
                            Year = Year.Substring(2);

                        Hour = date.Hour.ToString();
                        if (Hour.Length < 2)
                            Hour = "0" + Hour;

                        Minute = date.Minute.ToString();
                        if (Minute.Length < 2)
                            Minute = "0" + Minute;

                        DayOfWeek = ((int)date.DayOfWeek).ToString();                        

                        String retString = Month + Day + Year + Hour + Minute + DayOfWeek;

                        return retString;
                    }
                    #endregion
                }

                /// <summary>
                /// Get Current Date and Time Setting.
                /// This command causes the settings in the clock/calendar to be returned.
                /// 
                /// </summary>
                /// <param name="udt">the NDR response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NDR(out UDT_Data udt, bool optionalCloseConn = false)
                {
                    string command = "NDR";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        udt = UDT_Data.Parse(Vals[1]);

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
                /// Class used to hold data of UDT command
                /// </summary>
                public class UDTS_Data
                {

                    /// <summary>
                    /// mmddyyhhiiwss string 
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// ss = Seconds (00 - 59)
                    /// </summary>                    
                    public string datetimestring
                    {
                        get { return _datetimestring; }
                        set
                        {
                            ConvertUNAC_DateTime(value); //will throw exceptions if not proper
                            _datetimestring = value;
                        }
                    }

                    public DateTime UNAC_DateTime
                    {
                        get { return ConvertUNAC_DateTime(datetimestring); }
                        set { _datetimestring = ConvertDateTimeToUNAC_String(value); }
                    }


                    private string _datetimestring;
                    /// <summary>
                    /// Creates and populates a PR3_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static UDTS_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 1)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 1, found " + fields.Length + ".");

                        UDTS_Data response = new UDTS_Data
                        {
                            datetimestring = fields[0].Trim(),
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return datetimestring;
                    }

                    #region private helper functions
                    /// <summary>
                    /// Converts mmddyyhhiiwss string to DateTime
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// ss = Seconds (00-59)
                    /// </summary>
                    /// <param name="datetime"></param>
                    /// <returns></returns>
                    static public DateTime ConvertUNAC_DateTime(string datetime)
                    {
                        if (datetime.Length != "mmddyyhhjjwss".Length)
                            throw new ArgumentException("Invalid date time string.", datetime);

                        //add year 2000 padding
                        datetime = datetime.Substring(0, "mmdd".Length) + "20" + datetime.Substring("mmdd".Length);

                        //remove day of week
                        datetime = datetime.Substring(0, "mmddyyyyhhii".Length) + datetime.Substring("mmddyyyyhhiiw".Length); ;

                        //see: http://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
                        return DateTime.ParseExact(datetime, "MMddyyyyHHmmss", null); //, CultureInfo.InvariantCulture);
                    }

                    /// <summary>
                    /// Converts mmddyyhhiiw string to DateTime
                    /// mm = Month (01-12)
                    /// dd = Day (01-31)
                    /// yy = Year (00-99)
                    /// hh = Hour (00-23)
                    /// ii = Minutes (00-59)
                    /// w = Day of Week (0-6, 0 = Sunday)
                    /// ss = Seconds (00 - 59)
                    /// </summary>
                    /// <param name="datetime"></param>
                    /// <returns></returns>
                    public static string ConvertDateTimeToUNAC_String(DateTime date)
                    {
                        string Month, Day, Year, Hour, Minute, DayOfWeek, Second;

                        Month = date.Month.ToString();
                        if (Month.Length < 2)
                            Month = "0" + Month;

                        Day = date.Day.ToString();
                        if (Day.Length < 2)
                            Day = "0" + Day;

                        Year = date.Year.ToString();
                        if (Year.Length == 4)
                            Year = Year.Substring(2);

                        Hour = date.Hour.ToString();
                        if (Hour.Length < 2)
                            Hour = "0" + Hour;

                        Minute = date.Minute.ToString();
                        if (Minute.Length < 2)
                            Minute = "0" + Minute;

                        DayOfWeek = ((int)date.DayOfWeek).ToString();

                        Second = date.Second.ToString();
                        if (Second.Length < 2)
                            Second = "0" + Second;

                        String retString = Month + Day + Year + Hour + Minute + DayOfWeek + Second;

                        return retString;
                    }
                    #endregion
                }

                /// <summary>
                /// Get Current Date and Time Setting with Seconds.
                /// This command causes the settings in the clock/calendar to be returned with the seconds. Command added at UN01.04.
                /// 
                /// </summary>
                /// <param name="udts">the NDRS response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NDRS(out UDTS_Data udts, bool optionalCloseConn = false)
                {
                    string command = "NDRS";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        udts = UDTS_Data.Parse(Vals[1]);

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

                #region 3.3 Retrieve and Manage NAC Registers - Server I/F


                /// <summary>
                /// Class used to hold data of PR5 command
                /// </summary>
                public class PR5_Data
                {

                    /// <summary>
                    /// Current batch size
                    /// </summary>                    
                    public string bsz
                    {
                        get { return _p[0]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Current hot list size
                    /// </summary>
                    public string hsz
                    {
                        get { return _p[1]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Maximum batch size allowed
                    /// </summary>
                    public string bmax
                    {
                        get { return _p[2]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Maximum hot list size allowed
                    /// </summary>
                    public string hmax
                    {
                        get { return _p[3]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Batch record size
                    /// </summary>
                    public string brec
                    {
                        get { return _p[4]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Hot list record size
                    /// </summary>
                    public string hrec
                    {
                        get { return _p[5]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Total Memory in UNAC (1=128K, 4=512K)
                    /// </summary>
                    public string mem
                    {
                        get { return _p[6]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Memory available for both batch and hot list.
                    /// </summary>
                    public string avail
                    {
                        get { return _p[7]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Date of software build (mm/dd/yy).
                    /// </summary>
                    public string sdate
                    {
                        get { return _p[8]; }
                        set
                        {
                            string[] datefields = sdate.Split('/');
                            int month = int.Parse(datefields[0]);
                            int day = int.Parse(datefields[1]);
                            int year = int.Parse(datefields[2]);
                            if ((month < 0) || (month > 12) || (day < 0) || (day > 31) || (year < 0) || (year > 99) || (datefields.Length != 3))
                                throw new ArgumentOutOfRangeException("sdate", "Invalid software build date.");

                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Time of software build (hh:mm:ss).
                    /// </summary>
                    public string stime
                    {
                        get { return _p[9]; }
                        set
                        {
                            string[] timefields = sdate.Split(':');
                            int hour = int.Parse(timefields[0]);
                            int minute = int.Parse(timefields[1]);
                            int second = int.Parse(timefields[2]);
                            if ((hour < 0) || (hour > 23) || (minute < 0) || (minute > 59) || (second < 0) || (second > 59) || (timefields.Length != 3))
                                throw new ArgumentOutOfRangeException("sdate", "Invalid software build date.");

                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR5_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PR5_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        PR5_Data response = new PR5_Data
                        {
                            bsz = fields[0].Trim(),
                            hsz = fields[1].Trim(),
                            bmax = fields[2].Trim(),
                            hmax = fields[3].Trim(),
                            brec = fields[4].Trim(),
                            hrec = fields[5].Trim(),
                            mem = fields[6].Trim(),
                            avail = fields[7].Trim(),
                            sdate = fields[8].Trim(),
                            stime = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Memory Status.
                /// This causes the status of the memory used for batch and hot list to be returned.
                /// 
                /// </summary>
                /// <param name="pr5">the PR5 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPR5(out PR5_Data pr5, bool optionalCloseConn = false)
                {
                    string command = "NPR5";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pr5 = PR5_Data.Parse(Vals[1]);

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
                /// Class used to hold data of PM1 command
                /// </summary>
                public class PM1_Data
                {

                    /// <summary>
                    /// Modem Firmware Part Number (max. 8 char.)
                    /// </summary>                    
                    public string s1
                    {
                        get { return _p[0]; }
                        set
                        {
                            if (value.Length > 8)
                                throw new ArgumentOutOfRangeException("s1", "Modem firmware part number can be a maximum of 8 characters in length.");
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Modem Firmware Version (max. 8 char.)
                    /// </summary>
                    public string s2
                    {
                        get { return _p[1]; }
                        set
                        {
                            if (value.Length > 8)
                                throw new ArgumentOutOfRangeException("s1", "Modem firmware version can be a maximum of 8 characters in length.");
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Modem Firmware Compile date (mmm dd yyyy). (ie. "Oct 6 1999")
                    /// </summary>
                    public string s3
                    {
                        get { return _p[2]; }
                        set
                        {                            
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Modem type (max. 8 char.)
                    /// </summary>
                    public string s4
                    {
                        get { return _p[3]; }
                        set
                        {
                            if (value.Length > 8)
                                throw new ArgumentOutOfRangeException("s1", "Modem type can be a maximum of 8 characters in length.");                            
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// spare (null)
                    /// </summary>
                    public string s5
                    {
                        get { return _p[4]; }
                        set
                        {                            
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Status of UNAC modem use ("0" = modem not in use by UNAC, 
                    /// "1" = modem is in use, or about to be used by UNAC).
                    /// </summary>
                    public string s6
                    {
                        get { return _p[5]; }
                        set
                        {                            
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Dialing/Answer Enable
                    /// = "0", dialing/answering enabled.
                    /// = "1" to "999", number of minutes remaining in hold-off time.
                    /// </summary>
                    public string s7
                    {
                        get { return _p[6]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// spare (Null)
                    /// </summary>
                    public string s8
                    {
                        get { return _p[7]; }
                        set
                        {                            
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// spare (Null)
                    /// </summary>
                    public string s9
                    {
                        get { return _p[8]; }
                        set
                        {                            
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// spare (Null)
                    /// </summary>
                    public string s10
                    {
                        get { return _p[9]; }
                        set
                        {                            
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR5_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static PM1_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        PM1_Data response = new PM1_Data
                        {
                            s1 = fields[0].Trim(),
                            s2 = fields[1].Trim(),
                            s3 = fields[2].Trim(),
                            s4 = fields[3].Trim(),
                            s5 = fields[4].Trim(),
                            s6 = fields[5].Trim(),
                            s7 = fields[6].Trim(),
                            s8 = fields[7].Trim(),
                            s9 = fields[8].Trim(),
                            s10 = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// Get Status of Modem Board.
                /// This retrieves various status concerning the attached modem board.
                /// 
                /// </summary>
                /// <param name="pm1">the PM1 response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NPM1(out PM1_Data pm1, bool optionalCloseConn = false)
                {
                    string command = "NPM1";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        pm1 = PM1_Data.Parse(Vals[1]);

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
                /// Class used to hold data of RSR command
                /// </summary>
                public class RSR_Data
                {

                    /// <summary>
                    /// Emergency shutdown.
                    /// "O", for OK.
                    /// "T", for host time error.
                    /// "M", for terminal or Merchant ID error.
                    /// "U", for unexpected card type.
                    /// "7", for invalid transaction.
                    /// "D", for Modem failure
                    /// "p", for Production Test Mode expired.
                    /// "z", internal UNAC error (misc)
                    /// </summary>                    
                    public string r1
                    {
                        get { return _p[0]; }
                        set
                        {
                            _p[0] = value;
                        }
                    }

                    /// <summary>
                    /// Number of available slots in the Batch File for new transactions ("0" to "9999").
                    /// </summary>
                    public string r2
                    {
                        get { return _p[1]; }
                        set
                        {
                            int num = int.Parse(value); //will throw exceptions if not numeric
                            if ((num < 0) || (num > 9999))
                                throw new ArgumentOutOfRangeException("r2", "The number of available slots must be between 0 and 9999.");
                            _p[1] = value;
                        }
                    }

                    /// <summary>
                    /// Dial Status.
                    /// "N", for not in delay state.
                    /// "D", for in dial delay state.
                    /// </summary>
                    public string r3
                    {
                        get { return _p[2]; }
                        set
                        {
                            if ((value != "N") && (value != "D"))
                                throw new ArgumentOutOfRangeException("r3", "Dial status must be either N or D.");
                            _p[2] = value;
                        }
                    }

                    /// <summary>
                    /// Dial delay seconds remaining.
                    /// </summary>
                    public string r4
                    {
                        get { return _p[3]; }
                        set
                        {
                            int.Parse(value); //will throw exceptions if not numeric
                            _p[3] = value;
                        }
                    }

                    /// <summary>
                    /// Accept UCR Transactions State.
                    /// "O", NAC is accepting transactions from UCR.
                    /// "F", Halted, Batch full.
                    /// "S", Halted, NAC in Shutdown mode.
                    /// Halted = No "UOK" sent to UCR for Transaction.
                    /// </summary>
                    public string r5
                    {
                        get { return _p[4]; }
                        set
                        {
                            _p[4] = value;
                        }
                    }

                    /// <summary>
                    /// Additional shutdown information. When UNAC is in shutdown due to host response, 
                    /// this holds the host type and system reason code returned from the host. 
                    /// If not in shutdown, this field is blank. (Added UN01.11)
                    /// Example: 	if shutdown code is "M" caused by EZH then this location might have "1EB" in it.
                    /// "1" = EZH, "EB" = returned system reason code.
                    /// </summary>
                    public string r6
                    {
                        get { return _p[5]; }
                        set
                        {                            
                            _p[5] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string r7
                    {
                        get { return _p[6]; }
                        set
                        {                            
                            _p[6] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string r8
                    {
                        get { return _p[7]; }
                        set
                        {
                            _p[7] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string r9
                    {
                        get { return _p[8]; }
                        set
                        {                            
                            _p[8] = value;
                        }
                    }

                    /// <summary>
                    /// Not defined
                    /// </summary>
                    public string r10
                    {
                        get { return _p[9]; }
                        set
                        {
                            _p[9] = value;
                        }
                    }

                    private string[] _p = new string[10];

                    /// <summary>
                    /// Creates and populates a PR5_Data object
                    /// </summary>
                    /// <param name="incoming"></param>
                    /// <returns></returns>
                    public static RSR_Data Parse(string incoming)
                    {
                        string[] fields = incoming.Split(',');
                        if (fields.Length != 10)
                            throw new ArgumentOutOfRangeException("incoming", "Incorrect number of fields. Expecting 10, found " + fields.Length + ".");

                        RSR_Data response = new RSR_Data
                        {
                            r1 = fields[0].Trim(),
                            r2 = fields[1].Trim(),
                            r3 = fields[2].Trim(),
                            r4 = fields[3].Trim(),
                            r5 = fields[4].Trim(),
                            r6 = fields[5].Trim(),
                            r7 = fields[6].Trim(),
                            r8 = fields[7].Trim(),
                            r9 = fields[8].Trim(),
                            r10 = fields[9].Trim()
                        };
                        return response;
                    }


                    /// <summary>
                    /// Format the command parameters: "p,t,..."                
                    /// </summary>
                    /// <returns>The formatted command parameters</returns>
                    public override string ToString()
                    {
                        return String.Join(",", _p);
                    }
                }

                /// <summary>
                /// UNAC Operating Status.
                /// This command causes the current operating status of the UNAC to be returned.
                /// 
                /// </summary>
                /// <param name="rsr">the RSR response data</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void NRSR(out RSR_Data rsr, bool optionalCloseConn = false)
                {
                    string command = "NRSR";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        rsr = RSR_Data.Parse(Vals[1]);

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
            }



            public class WinSIPserver : CommandHandler
            {                

                /// <summary>
                /// constructor for when an STXETX handler is already in place
                /// </summary>
                /// <param name="stxetxClient"></param>
                /// <param name="optionalTS"></param>
                public WinSIPserver(IProtocolHandler stxetxClient, TraceSource optionalTS = null)
                    : base(stxetxClient, optionalTS)
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
                /// How many entries are there in the BNAC table?
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="entries">the number of BNAC table entries</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void BNUM(out int entries, bool optionalCloseConn = false)
                {
                    string command = "BNUM";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)                        
                            throw new ResponseException("Invalid response received.", command, resps);                        

                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else
                            entries = int.Parse(response);
                                                
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
                /// Set the BNAC table size (currently only supports growing the BNAC table)
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="entries">the number of BNAC table entries</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void BSET(int entries, bool optionalCloseConn = false)
                {
                    string command = "BSET=" + entries.ToString();
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)                        
                            throw new ResponseException("Invalid response received.", command, resps);                            

                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (entries != int.Parse(response))
                            throw new ResponseException("Request to increase BNAC table size to " + entries + " entries " 
                                + "resulted in an unexpected increase to " + int.Parse(response) + " entries.", command, resps);
                        
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
                /// Count the active socket connections
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="connections">the number of active socket connections</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void CCON(out int connections, bool optionalCloseConn = false)
                {
                    string command = "CCON";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else
                            connections = int.Parse(response);                            

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


                /// <summary>
                /// Request BNAC server status information
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="memory_used">number of bytes currently allocated in the BNAC server process</param>
                /// <param name="max_worker">maximum number of worker threads allowed</param>
                /// <param name="max_port">maximum number of asynchronous port threads allowed</param>
                /// <param name="avail_worker">max worker – worker threads in use</param>
                /// <param name="avail_port">max port – number of asynchronous port threads in use</param>
                /// <param name="min_worker">number of worker threads that can be allocated on demand; 
                /// after this number is exceeded an algorithm will slowly create more worker threads 
                /// (up to max worker) on the order of one thread per half second, this decreases performance.</param>
                /// <param name="min_port">number of port threads that can be allocated on demand; 
                /// after this number is exceeded an algorithm will slowly create more worker threads 
                /// (up to max worker) on the order of one thread per half second, this decreases performance.</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void STAT(out int memory_used, out int max_worker, out int max_port, out int avail_worker, out int avail_port, out int min_worker, out int min_port, bool optionalCloseConn = false)
                {
                    string command = "STAT";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);
                            
                        string[] responses = Vals[1].Trim().Split(',');
                        if (responses.Length != 7)
                            throw new ResponseException("Invalid response received.", command, resps);

                        memory_used = int.Parse(responses[0]);
                        max_worker = int.Parse(responses[1]);
                        max_port = int.Parse(responses[2]);
                        avail_worker = int.Parse(responses[3]);
                        avail_port = int.Parse(responses[4]);
                        min_worker = int.Parse(responses[5]);
                        min_port = int.Parse(responses[6]);
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
                /// Request BNAC Server version
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="version_str">The version string (not yet standardized at the time of writing this function</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void VER(out string version_str, bool optionalCloseConn = false)
                {
                    string command = "VER";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        version_str = Vals[1].Trim();
                        
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

                #region BNAC TABLE COMMANDS

                /// <summary>
                /// Get BNAC status information
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="index">the index of the BNAC in the BNAC table</param>
                /// <param name="status">the current status of the BNAC</param>
                /// <param name="lastCheckin"> the last time the BNAC checked in (in GMT)</param>
                /// <param name="sms_remaining">the number of monthly SMS messages remain (only relevant for cellular BNACs)</param>                
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void BSTA(int index, out BNAC_StateTable.Entry status, bool optionalCloseConn = false)
                {
                    string command = "BSTA" + index.ToString() + "?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string[] responses = Vals[1].Trim().Split(',');

                        string status_str = responses[0].Trim();
                        if (status_str == "E")
                            throw new ResponseErrorCodeException("This BNAC table entry is empty.", command, resps);
                        else if (status_str == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (responses.Length != 3)
                            throw new ResponseException("Invalid response received.", command, resps);

                        status = new BNAC_StateTable.Entry(index, responses);

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
                /// Read BNAC table entry
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the entry to read</param>
                /// <param name="idType">the type of ID</param>
                /// <param name="tableEntry">the entry found</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param>
                /// <returns>DON'T USE THE RETURN VALUE, in the future this function will be type void</returns>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
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
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
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


                /// <summary>
                /// Read back the first used BNAC table entry at or after next index
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the entry to read</param>
                /// <param name="idType">the type of ID</param>
                /// <param name="tableEntry">the entry found</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param> 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void RBTN(string ID, BNAC_Table.ID_Type idType, out BNAC_Table.Entry tableEntry, bool optionalCloseConn = false)
                {
                    string command = "RBTN" + BNAC_Table.CreateID(ID, idType) + "?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "-1")
                            throw new ResponseErrorCodeException("No more used BNAC table entries.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else
                            tableEntry = new BNAC_Table.Entry(response.Split(','));
                        
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
                /// Check in command used by UNAC to check in with WinSIP Server
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the UNAC</param>
                /// <param name="idType">the type of ID</param>
                /// <param name="CheckingOnly">If false, no passthrough connection will be initiated if a request is present</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param> 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void CIN(string ID, BNAC_Table.ID_Type idType, bool CheckingOnly, out BNAC_StateTable.BNAC_Status status, bool optionalCloseConn = false)
                {
                    string command = "CIN=" + BNAC_Table.CreateID(ID, idType);
                    if (CheckingOnly)
                        command = command + ",1";
                    
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string response = Vals[1].Trim();
                        if (response == "UNKNOWN")
                            throw new ResponseErrorCodeException("The requested UNAC is not registered with the server.", command, resps);
                        else if (response == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else
                            status = BNAC_StateTable.ParseBNAC_Status(response);

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
                /// Request a passthrough connection to a UNAC
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the entry to read</param>
                /// <param name="idType">the type of ID</param>
                /// <param name="status">the UNAC status</param>
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param> 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void CONB(string ID, BNAC_Table.ID_Type idType, out BNAC_StateTable.Entry status, bool optionalCloseConn = false)
                {
                    string command = "CONB=" + BNAC_Table.CreateID(ID, idType);
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);

                    try
                    {
                        string[] Vals = resps[0].Split('=');
                        if (Vals.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        string[] responses = Vals[1].Trim().Split(',');

                        string status_str = responses[0].Trim();
                        if (status_str == "I")
                            throw new ResponseErrorCodeException("Connection request failed due to an internal server error. Try again later.", command, resps);
                        else if (status_str == "M")
                            throw new ResponseErrorCodeException("Memory or unexpected error.", command, resps);
                        else if (responses.Length != 2)
                            throw new ResponseException("Invalid response received.", command, resps);

                        status = new BNAC_StateTable.Entry(0, responses);

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
                /// Delete the table BNAC entry corresponding to ID
                /// 
                /// Exceptions thrown: ResponseException, ResponseErrorCodeException, UnresponsiveConnectionException
                /// </summary>
                /// <param name="ID">the ID of the entry to read</param>
                /// <param name="idType">the type of ID</param>                
                /// <param name="optionalCloseConn">set to true if the connection should be closed after calling this function</param> 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public void DBT(string ID, BNAC_Table.ID_Type idType, bool optionalCloseConn = false)
                {
                    string command = "DBT=" + BNAC_Table.CreateID(ID, idType);
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
