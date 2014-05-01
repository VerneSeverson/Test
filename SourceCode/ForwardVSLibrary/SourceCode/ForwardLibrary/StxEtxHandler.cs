using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

namespace ForwardLibrary
{
    namespace Communications
    {
        namespace STXETX
        {
            public class CommandAndFunction
            {
                public delegate void CommandFunction(MemoryStream theCommand, StxEtxHandler source);
                public delegate bool CommandPermission(MemoryStream theCommand, StxEtxHandler source);
                public delegate bool CommandSecurity(MemoryStream theCommand, StxEtxHandler source);
                string command;
                CommandFunction function;
                CommandPermission permission;
                CommandSecurity security;

                public String Command { get { return command; } }                
                public CommandFunction theFunction { get { return function; } }
                public CommandPermission getPermission { get { return permission; } }
                public CommandSecurity checkSecurity { get { return security; } }

                /// <summary>
                /// Default constructor, sets the CommandPermission and CommandSecurity delegates
                /// so that they always return true (okay)
                /// </summary>
                /// <param name="theCommand"></param>
                /// <param name="theFunc"></param>
                public CommandAndFunction(String theCommand, CommandFunction theFunc)
                {
                    command = theCommand;
                    function = theFunc;
                    
                    permission = delegate(MemoryStream a, StxEtxHandler b) {return true;} ;
                    security = delegate(MemoryStream a, StxEtxHandler b) { return true; }; 
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="theCommand"></param>
                /// <param name="theFunc"></param>
                /// <param name="permissionCheck">this function is called before theCommand(); return true to indicate that the STX handler has permission to call theCommand()</param>
                /// <param name="securityCheck">this function is called before theCommand(); return true to indicate that the STX handler's ClientContext is secure enought to call theCommand()</param>
                public CommandAndFunction(String theCommand, CommandFunction theFunc, 
                            CommandPermission permissionCheck, CommandSecurity securityCheck)
                {
                    command = theCommand;
                    function = theFunc;

                    permission = permissionCheck;
                    security = securityCheck;
                }



                /// <summary>
                /// Constructor for when any level of security is okay.
                /// </summary>
                /// <param name="theCommand"></param>
                /// <param name="theFunc"></param>
                /// <param name="permissionCheck">this function is called before theCommand(); return true to indicate that the STX handler has permission to call theCommand()</param>
                public CommandAndFunction(String theCommand, CommandFunction theFunc,
                            CommandPermission permissionCheck)
                {
                    command = theCommand;
                    function = theFunc;

                    permission = permissionCheck;
                    security = delegate(MemoryStream a, StxEtxHandler b) { return true; }; 
                }
            }

            public class StxEtxHandler
            {
                /// <summary>
                /// Communication object being used
                /// </summary>
                public ClientContext CommContext = null;

                /// <summary>
                /// If false, a received command must be present in the CommandTable
                /// or an ERM will be returned. Set to true to support the ReceiveData 
                /// methods.
                /// </summary>
                public bool bAllowAllCmds;

                /// <summary>
                /// The command table used to look up incoming commands and call 
                /// their corresponding functions. This does not need to be set
                /// in the case that the ReceiveData methods are used.
                /// </summary>
                public LinkedList<CommandAndFunction> CommandTable;

                const int STX_SEARCH = 0, ETX_SEARCH = 1, ACK_SEARCH = 2;

                private static Random randObj = new Random(1);

                int StxEtxAck = STX_SEARCH;
                MemoryStream ReceivedData = new MemoryStream();
                SendContext SendingContext = new SendContext();

                StringBuilder ReceiveDatLock = new StringBuilder(randObj.Next().ToString());
                StringBuilder ReceiveAPI_Lock = new StringBuilder(randObj.Next().ToString());
                StringBuilder DisconnectDatLock = new StringBuilder(randObj.Next().ToString());
                bool bDisconnectEventHandled = false;

                private ConcurrentQueue<ReceivedMsgLog> RxMsgs = new ConcurrentQueue<ReceivedMsgLog>();
                public AutoResetEvent NewMsgEvt = new AutoResetEvent(false);

                /// <summary>
                /// Used for storing STX ETX messages that have been received
                /// </summary>
                class ReceivedMsgLog
                {
                    public string msg;
                    public DateTime time;

                    public ReceivedMsgLog(string msg)
                    {
                        this.msg = msg;
                        time = DateTime.Now;
                    }

                    public ReceivedMsgLog(string msg, DateTime time)
                    {
                        this.msg = msg;
                        this.time = time;
                    }
                }

                class SendContext
                {
                    public const int DEF_NUM_RETRIES = 3;
                    public const int DEF_RETRY_TIMEOUT_MS = 3000;

                    public AutoResetEvent AckFound = null;

                    public void AddStxEtx(out byte[] dataOut, byte[] dataIn)
                    {
                        MemoryStream dat = new MemoryStream();
                        dat.WriteByte(0x02);
                        if (dataIn != null)
                            dat.Write(dataIn, 0, dataIn.Length);
                        dat.WriteByte(0x03);
                        dataOut = new byte[dat.Length];
                        dat.Seek(0, SeekOrigin.Begin);
                        dat.Read(dataOut, 0, (int)dat.Length);
                    }
                    /// <summary>
                    /// Call this upon client disconnect to make sure SendCommand gives up quickly
                    /// </summary>
                    public void Dispose()
                    {
                        try { AckFound.Dispose(); }
                        catch { }
                    }
                }

                /// <summary>
                /// Constructor -- adds the EventHandler delegate to context
                /// when an StxEtxClient is done being used, a DisconnectEvent
                /// MUST BE GENERATED in order for resources to be 
                /// properly freed up.
                /// </summary>
                /// <param name="context"></param>            
                /// <param name="context"></param>            
                public StxEtxHandler(ClientContext context, bool bAllowAllCmds = false, LinkedList<CommandAndFunction> CommandTable = null)
                {
                    this.bAllowAllCmds = bAllowAllCmds;
                    this.CommandTable = CommandTable;
                    CommContext = context;
                    context.EventCallback = OnClientEvent;
                }

                /// <summary>
                /// Force a clean up of the resources (not safe to use after this is called)
                /// </summary>
                public void Dispose()
                {

                    if (CommContext.bConnected)
                        try { CommContext.Close(); }
                        catch { }


                }

                #region Receive Commands
                /// <summary>
                /// Get latest STX ETX command in a FIFO manner.
                /// This function blocks until either a command is available or a timeout occurs.
                /// An exception is thrown if the connection goes down or any other error prevents successful completion.
                /// NOTE: this function is synchronized, so it is thread safe, however it will block until all previous
                /// function calls complete.
                /// </summary>
                /// <param name="theData">The received data</param>
                /// <param name="timeRcvd">The time that this command was received</param>
                /// <param name="optionalTimeout">Amount of time (in ms) before function gives up and returns</param>
                /// <returns>True if another command is present, otherwise false</returns>
                public bool ReceiveData(out string theData, out DateTime timeRcvd, int optionalTimeout = 30000)
                {
                    bool bRetVal = false;
                    theData = null;
                    timeRcvd = DateTime.Now;

                    lock (ReceiveAPI_Lock)
                    {
                        bool bSignaled = false;
                        if (RxMsgs.IsEmpty)
                        {
                            //should be impossible for RxMsgs to be empty and NewMsgEvent to be set... NewMsgEvent.Reset();
                            if (CommContext.bConnected == false)
                                throw new System.InvalidOperationException("Unable to read data, the connection is down.");

                            bSignaled = NewMsgEvt.WaitOne(optionalTimeout);
                        }
                        if (RxMsgs.IsEmpty)
                        {
                            bRetVal = false;

                            if (CommContext.bConnected == false)
                                throw new System.InvalidOperationException("Unable to read data, the connection is down.");

                            if (bSignaled == true)
                                throw new System.InvalidOperationException("Unable to read data due to unknown error.");

                        }
                        else
                        {
                            ReceivedMsgLog msg;
                            bool result = RxMsgs.TryDequeue(out msg);
                            if (result == true)
                            {
                                theData = msg.msg;
                                timeRcvd = msg.time;
                                try { NewMsgEvt.Reset(); }
                                catch { }
                            }
                            else
                                throw new System.InvalidOperationException("Unable to dequeue message, try calling again.");

                            bRetVal = !RxMsgs.IsEmpty;
                        }
                    }

                    return bRetVal;
                }

                /// <summary>
                /// Get latest STX ETX command in a FIFO manner.
                /// This function blocks until either a command is available or a timeout occurs.
                /// An exception is thrown if the connection goes down or any other error prevents successful completion.
                /// NOTE: this function is synchronized, so it is thread safe, however it will block until all previous
                /// function calls complete.
                /// </summary>
                /// <param name="theData">The received data</param>
                /// <param name="optionalTimeout">Amount of time (in ms) before function gives up and returns</param>
                /// <returns>True if another command is present, otherwise false</returns>
                public bool ReceiveData(out string theData, int optionalTimeout = 30000)
                {
                    DateTime timeRcvd;
                    return ReceiveData(out theData, out timeRcvd, optionalTimeout);
                }

                #endregion


                #region Send Commands
                /// <summary>
                /// Send an STX ETX formatted command (STX and ETX are added here). 
                /// Blocks until ack received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if no ACK is received</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if an ACK was recieved, otherwise false</returns>
                public bool SendCommand(byte[] data, int optionalRetries = SendContext.DEF_NUM_RETRIES, int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS)
                {
                    bool bFoundAck = false;
                    #region Add STX ETX, call new byte array "theData"
                    byte[] theData;                    
                    SendingContext.AddStxEtx(out theData, data);
                    #endregion

                    lock (SendingContext)
                    {
                        SendingContext.AckFound = new AutoResetEvent(false);
                        StxEtxAck = ACK_SEARCH;

                        try
                        {
                            while (optionalRetries >= 0)
                            {
                                string dat;
                                if (data != null)
                                    dat = System.Text.Encoding.Default.GetString(data);
                                else
                                    dat = "";

                                CommContext.LogMsg(TraceEventType.Verbose, "STXETX SENT: <STX>" + dat + "<ETX>");
                                CommContext.Write(theData);

                                bool reply = SendingContext.AckFound.WaitOne(optionalRetryTime);
                                if (reply == true)  //got an ack!
                                {
                                    bFoundAck = true;
                                    break;
                                }
                                else
                                    optionalRetries -= 1;
                            }
                        }
                        /* want the calling function to catch the exceptions... catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw e;
                        }*/
                        catch (Exception ex)
                        {
                            if (data != null)
                                CommContext.LogMsg(TraceEventType.Verbose, "Caught exception when waiting for an ACK to this message: <STX>" + System.Text.Encoding.Default.GetString(data.ToArray()) + "<ETX>. optionalRetries: " + optionalRetries.ToString() + ". Exception: " + ex.ToString());
                            else
                                CommContext.LogMsg(TraceEventType.Verbose, "Caught exception when waiting for an ACK to this message: <STX><ETX>. optionalRetries: " + optionalRetries.ToString() + ". Exception: " + ex.ToString());
                        }
                        finally
                        {
                            try { SendingContext.AckFound.Dispose(); }
                            catch { }
                            SendingContext.AckFound = null;

                            //important to prevent the STX/ETX handler from locking
                            if (StxEtxAck == ACK_SEARCH)
                                StxEtxAck = STX_SEARCH;
                        }

                    }
                    return bFoundAck;
                }

                /// <summary>
                /// Send an STX ETX formatted command (STX and ETX are added here). 
                /// Blocks until ack received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if no ACK is received</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if an ACK was recieved, otherwise false</returns>
                public bool SendCommand(string data, int optionalRetries = SendContext.DEF_NUM_RETRIES, int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS)
                {
                    return SendCommand(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime);
                }


                #region NON-BLOCKING SEND CALLS
                delegate bool AsyncSendCommandCaller(byte[] data, int optionalRetries = SendContext.DEF_NUM_RETRIES, int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS);
                delegate bool AsyncSendCommandCallerStr(string data, int optionalRetries = SendContext.DEF_NUM_RETRIES, int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS);

                /// <summary>
                /// Asynchronous function call for SendCommand()
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <param name="callback"></param>
                /// <param name="state"></param>
                /// <returns></returns>
                public IAsyncResult BeginSendCommand(byte[] data,
                                                    int optionalRetries = SendContext.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS,
                                                    AsyncCallback callback = null,
                                                    Object state = null)
                {
                    AsyncSendCommandCaller caller = new AsyncSendCommandCaller(this.SendCommand);
                    IAsyncResult result = caller.BeginInvoke(data, optionalRetries, optionalRetryTime, callback, state);
                    return result;
                }

                /// <summary>
                /// Asynchronous function call for SendCommand()
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <param name="callback"></param>
                /// <param name="state"></param>            
                /// <returns></returns>
                public IAsyncResult BeginSendCommand(string data,
                                                    int optionalRetries = SendContext.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS,
                                                    AsyncCallback callback = null,
                                                    Object state = null)
                {
                    AsyncSendCommandCallerStr caller = new AsyncSendCommandCallerStr(this.SendCommand);
                    IAsyncResult result = caller.BeginInvoke(data, optionalRetries, optionalRetryTime, callback, state);
                    return result;
                }

                /// <summary>
                /// Call to end asynchronous sending
                /// </summary>
                /// <param name="ar"></param>
                /// <returns></returns>
                public bool EndSendCommand(IAsyncResult ar)
                {
                    AsyncResult result = (AsyncResult)ar;
                    AsyncSendCommandCaller caller = (AsyncSendCommandCaller)result.AsyncDelegate;
                    return caller.EndInvoke(ar);
                }

                //
                /// <summary>
                /// Non-blocking send command where the user doesn't care if the message gets sent
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                public void AsyncSendCommand(byte[] data,
                                                    int optionalRetries = SendContext.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS)
                {
                    BeginSendCommand(data,
                        optionalRetries, optionalRetryTime,
                        callback: delegate(IAsyncResult ar)
                    {
                        try { EndSendCommand(ar); }
                        catch (Exception e)
                        {
                            if (CommContext.bConnected == false)
                                CommContext.LogMsg(TraceEventType.Verbose, "Sending failed. This socket is disconnected, which is most likely the reason that the Sending function failed: " + e.ToString());
                            else
                                CommContext.LogMsg(TraceEventType.Error, "Sending failed and the CommContext thinks the socket is still connected: " + e.ToString());
                        }
                    });
                }
                public void AsyncSendCommand(string data,
                                                    int optionalRetries = SendContext.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = SendContext.DEF_RETRY_TIMEOUT_MS)
                {
                    AsyncSendCommand(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime);
                }
                #endregion

                #endregion

                #region Event handling
                /// <summary>
                /// Used to handle communication events
                /// This function is thread safe and blocks if a previous event of the same type has not finished
                /// </summary>
                /// <param name="ev"></param>
                protected void OnClientEvent(ClientEvent ev)
                {
                    #region The client has disconnected
                    if (ev is ClientDisconnectedEvent)
                        DisconnectEvent(ev as ClientDisconnectedEvent);
                    #endregion
                    #region New data has been received from the client
                    else if (ev is ClientReceivedDataEvent)
                        ReceivedDataEvent(ev as ClientReceivedDataEvent);

                    #endregion
                    #region Write completed
                    /*else if (ev.Event is ClientWroteDataEvent)
            {             
            }*/

                    #endregion
                }

                protected virtual void DisconnectEvent(ClientDisconnectedEvent theEvent)
                {
                    if (bDisconnectEventHandled == false)
                    {
                        bDisconnectEventHandled = true;

                        lock (DisconnectDatLock)
                        {
                            try
                            {
                                NewMsgEvt.Set();    //set this so that any blocking call to ReceiveData is released
                            }
                            catch { }
                            try { SendingContext.Dispose(); }
                            catch { }
                        }
                    }
                }

                protected virtual void ReceivedDataEvent(ClientReceivedDataEvent Revent)
                {
                    bool bEnd = false;
                    lock (ReceiveDatLock)
                    {
                        foreach (byte theByte in Revent.RcvDat)
                        {
                            if (bEnd)
                                break;

                            switch (StxEtxAck)
                            {
                                case STX_SEARCH:
                                    if (theByte == 0x02)
                                    {
                                        ReceivedData = new MemoryStream();
                                        StxEtxAck = ETX_SEARCH;
                                    }
                                    break;

                                case ETX_SEARCH:
                                    if (theByte == 0x03)
                                    {
                                        //command is finished     
                                        CommContext.LogMsg(TraceEventType.Verbose, "STXETX RCVD: <STX>" + System.Text.Encoding.Default.GetString(ReceivedData.ToArray()) + "<ETX>");

                                        CommContext.Write(new byte[1] { 0x06 });    //send ACK                                        
                                        CommContext.LogMsg(TraceEventType.Verbose, "STXETX SENT: <ACK>"); 

                                        StxEtxAck = STX_SEARCH; //go back to looking for an STX

                                        //process the parsed command
                                        ProcessCommand(ReceivedData);                                        

                                        //protocol only allows one command and then an ack, so toss the rest of the data                                        
                                        bEnd = true;
                                        break;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            ReceivedData.WriteByte(theByte);
                                        }
                                        catch (Exception ex)
                                        {
                                            CommContext.LogMsg(TraceEventType.Error, ex);
                                            //something is wrong with the memory stream, get rid of it and dump all data                                        
                                            StxEtxAck = STX_SEARCH;
                                        }
                                    }

                                    break;

                                case ACK_SEARCH:
                                    if (theByte == 0x06)
                                    {
                                        CommContext.LogMsg(TraceEventType.Verbose, "STXETX RCVD: <ACK>");
                                        StxEtxAck = STX_SEARCH;
                                        if (SendingContext.AckFound != null)
                                        {
                                            try
                                            {
                                                SendingContext.AckFound.Set(); //release the thread that was writing
                                            }
                                            catch (Exception ex)
                                            {
                                                CommContext.LogMsg(TraceEventType.Error, ex);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }

                    }
                }


                #endregion

                /// <summary>
                /// Process the command received. If the command exists in the 
                /// command look up table, then call its corresponding function
                /// otherwise, either store in the receive log for future 
                /// retrieval or send an ERM based on the value of bLogMsgs
                /// </summary>
                /// <param name="theCommand">The command received (STX ETX removed)</param>
                /// <returns></returns>
                protected bool ProcessCommand(MemoryStream theCommand)
                {
                    String cmd = System.Text.Encoding.Default.GetString(theCommand.ToArray());

                    //moved up one level to get this message before ACK: CommContext.LogMsg(TraceEventType.Verbose, "STXETX RCVD: <STX>" + cmd + "<ETX>") ;

                    bool inCmdTable = false;
                    if (CommandTable != null)
                        inCmdTable = DoCmdTable(theCommand);
                    if (!inCmdTable && bAllowAllCmds)
                    {
                        ReceivedMsgLog msglog = new ReceivedMsgLog(cmd);
                        RxMsgs.Enqueue(msglog);
                        NewMsgEvt.Set();
                    }
                    else if (!inCmdTable && !bAllowAllCmds && cmd.Length > 0)
                        AsyncSendCommand(System.Text.Encoding.ASCII.GetBytes("ERM"));


                    return true;
                }

                bool DoCmdTable(MemoryStream theCommand)
                {
                    String cmd = System.Text.Encoding.Default.GetString(theCommand.ToArray());
                    CommandAndFunction[] entries = CommandTable.ToArray<CommandAndFunction>();

                    bool bFound = false;
                    bool bSecurity = false;
                    bool bPermission = false;

                    foreach (CommandAndFunction entry in entries)
                    {
                        String compare;
                        if (cmd.Length >= entry.Command.Length)
                            compare = cmd.Substring(0, entry.Command.Length);
                        else
                            continue;

                        if (compare.Equals(entry.Command) == true)
                        {
                            try
                            {
                                bFound = true;

                                //okay, do we have the right security?
                                if (entry.checkSecurity(theCommand, this))
                                {
                                    bSecurity = true;
                                    if (entry.getPermission(theCommand, this))
                                        bPermission = true;
                                }

                                if (bSecurity && bPermission)
                                    entry.theFunction(theCommand, this);
                            }
                            catch (Exception e)
                            {
                                CommContext.LogMsg(TraceEventType.Error, "Executing lookup table command " + cmd + " caused exception: " + e.ToString());
                                AsyncSendCommand(System.Text.Encoding.ASCII.GetBytes("ERM"));
                            }

                            break;
                        }

                    }

                    return (bFound && bSecurity && bPermission);        //true indicates data was sent
                }
            }
        }
    }
}
