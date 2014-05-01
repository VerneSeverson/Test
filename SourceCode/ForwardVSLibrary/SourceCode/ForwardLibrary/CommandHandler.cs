using ForwardLibrary.WinSIPserver;
using ForwardLibrary.Communications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{

    namespace Communications
    {
        namespace STXETX
        {
            #region Exceptions
            public class ResponseException : Exception
            {
                public string DataSent;
                public List<string> ResponsesReceived;

                public string ExtendedMessage
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

            public class CommandHandler
            {
                public int LogID = 500;

                public TraceSource ts;

                StxEtxHandler stxetxClient = null;

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

                    this.stxetxClient = stxetxClient;
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
                    stxetxClient = new StxEtxHandler(new TCPconnManager(ts).ConnectToServer(hostname, optionalPort), true);
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
                            if (stxetxClient.SendCommand(command))
                            {
                                string reply = null;
                                bool result = true;
                                int giveUp = NumResponses + 3;
                                while (result && Responses.Count < NumResponses && giveUp-- > 0)
                                {
                                    result = stxetxClient.ReceiveData(out reply, optionalTimeout * 1000);
                                    if (reply != null)
                                        Responses.Add(reply);

                                }
                                //see if we found all the responses we were looking for
                                if (Responses.Count < NumResponses)
                                {
                                    ResponseException ex = new ResponseException(
                                        "Did not receive the desired number of responses: found "
                                        + Responses.Count.ToString() + " of " + NumResponses.ToString(),
                                        command, Responses);

                                    LogMsg(TraceEventType.Warning, ex.ToString());
                                    throw ex;
                                }
                                break;
                            }
                            else if (optionalRetries < 1)
                            {
                                UnresponsiveConnectionException ex = new UnresponsiveConnectionException(
                                    "Failed to send the data: connection is unresponsive.", command);
                                LogMsg(TraceEventType.Warning, ex.ToString());
                                throw ex;
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        ResponseException ex = new ResponseException(
                            "Received an exception when trying to send or receive a command",
                            command, Responses, ee);

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

                #region Specific commands
                public void ReadSSET(out string LogUploadPeriod, out string LogMaxSize, out string AppTraceLevel, out string ComTraceLevel, bool optionalCloseConn = false)
                {
                    int _LogUploadPeriod, _LogMaxSize;
                    ReadSSET(out _LogUploadPeriod, out _LogMaxSize, out AppTraceLevel, out ComTraceLevel, optionalCloseConn);
                    LogUploadPeriod = _LogUploadPeriod.ToString();
                    LogMaxSize = _LogMaxSize.ToString();
                }

                public void ReadSSET(out int LogUploadPeriod, out int LogMaxSize, out string AppTraceLevel, out string ComTraceLevel, bool optionalCloseConn = false)
                {
                    List<string> resps = SendCommand("SSET?", 1, optionalCloseConn);
                    string[] Vals = resps[0].Split('=');
                    if (Vals.Length != 2)
                    {
                        ResponseException ex = new ResponseException("Invalid response received.", "SSET?", resps);
                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex; 
                    }

                    Vals = Vals[1].Split(',');

                    if (Vals.Length != 4)
                    {
                        ResponseException ex = new ResponseException("Invalid response received.", "SSET?", resps);
                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                    try
                    {
                        LogUploadPeriod = Convert.ToInt32(Vals[0]);
                        LogMaxSize = Convert.ToInt32(Vals[1]);
                        AppTraceLevel = Vals[2];
                        ComTraceLevel = Vals[3];
                    }
                    catch (Exception ex)
                    {
                        ResponseException exe = new ResponseException("Exception occured when parsing the response received.", "SSET?", resps, ex);
                        LogMsg(TraceEventType.Warning, exe.ToString());
                        throw ex;                        
                    }

                }

                public string SetSSET(int LogUploadPeriod, int LogMaxSize, string AppTraceLevel, string ComTraceLevel, bool optionalCloseConn = false, int optionalRetries = 3)
                {
                    return SetSSET(LogUploadPeriod.ToString(), LogMaxSize.ToString(), AppTraceLevel, ComTraceLevel, optionalCloseConn, optionalRetries);
                }

                public string SetSSET(string LogUploadPeriod, string LogMaxSize, string AppTraceLevel, string ComTraceLevel, bool optionalCloseConn = false, int optionalRetries = 3)
                {
                    string resp;
                    string command = "SSET=" + LogUploadPeriod + ","
                                                        + LogMaxSize + ","
                                                        + AppTraceLevel + ","
                                                        + ComTraceLevel;

                    List<string> resps = SendCommand(command, 1, optionalRetries: optionalRetries);

                    if (resps[0].StartsWith("SSET="))
                        resp = resps[0].Substring(5);
                    else
                    {
                        ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;                         
                    }

                    return resp;
                }

                public string ReadCBT(string ID, BNAC_Table.ID_Type idType, out BNAC_Table.Entry tableEntry, bool optionalCloseConn = false)
                {
                    string response = ReadCBT(ID, idType, optionalCloseConn);
                    if (response == "E" || response == "M")
                        tableEntry = null;      //not found or memory error!
                    else
                        try
                        {
                            tableEntry = new BNAC_Table.Entry(response.Split(','));
                        }
                        catch (Exception e)
                        {
                            ResponseException ex = new ResponseException("Exception occurred when trying to parse the CBT response.", "Inaccessible", response,e);
                            List<string> re = new List<string>();                            
                            LogMsg(TraceEventType.Warning, ex.ToString());
                            throw ex;
                        }
                    return response;
                }

                public string ReadCBT(string ID, BNAC_Table.ID_Type idType, bool optionalCloseConn = false)
                {
                    string command = "CBT=" + BNAC_Table.CreateID(ID, idType) + "?";
                    List<string> resps = SendCommand(command, 1, optionalCloseConn);
                    string[] Vals = resps[0].Split('=');
                    if (Vals.Length != 2)
                    {
                        ResponseException ex = new ResponseException("Invalid response received.", command, resps);
                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                    return Vals[1];
                }
                #endregion

                private void LogMsg(TraceEventType type, string msg)
                {
                    ts.TraceEvent(type, LogID, msg);
                }

                public void Dispose()
                {
                    try
                    {
                        stxetxClient.Dispose();
                        stxetxClient = null;
                    }
                    catch
                    {
                    }
                }





            }

        }
        

    }
}
