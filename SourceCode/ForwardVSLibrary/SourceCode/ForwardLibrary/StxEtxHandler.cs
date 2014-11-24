﻿using System;
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
        public static class ProtocolDefaults
        {
            public const int DEF_NUM_RETRIES = 3;
            public const int DEF_RETRY_TIMEOUT_MS = 3000;            
        }

        public interface IProtocolHandler
        {

            /// <summary>
            /// The last time the user used the protocol handler to successfully send data.
            /// 
            /// Note that periodic ping messages do not cause this to be updated.
            /// </summary>
            DateTime LastSent { get; }

            ClientContext CommContext { get; set; }

            #region Communication events
            /// <summary>
            /// Registers a callback to receive protocol communication events:
            /// ClientReceivedDataEvent, ClientWroteDataEvent,
            /// and ClientDisconnectedEvent. These events are generated
            /// asynchronously and contain protocol-level information. For
            /// example, for STX/ETX handlers, STX, ETX, and ACK will not be
            /// included in the data when these events are created; futhermore
            /// a ClientWroteDataEvent will only be created after the protocol
            /// knows that the client has received the data (ACK received).
            /// </summary>
            /// <param name="EventCallback">The delegate to call</param>
            void AddCommEventHandler(EventNotify EventCallback);

            /// <summary>
            /// Unregisters a callback delegate from receiving protocol 
            /// communication events.
            /// </summary>
            /// <param name="EventCallback"></param>
            void RemoveCommEventHandler(EventNotify EventCallback);

            #endregion

            /// <summary>
            /// This function enables/disables periodically sending STX ETX messages 
            /// to the remote device to ensure that the connection stays alive and that
            /// the peer is present.
            /// </summary>
            /// <param name="enable">Set to true to periodically send STX ETX messages to the peer</param>
            /// <param name="optionalMaxIdleTime">Maximum connection idle time before an STX ETX should be sent</param>
            void PeriodicPing(bool enable, TimeSpan MaxIdleTime);

            #region receive functions
            /// <summary>
            /// Get received data in a FIFO manner.
            /// This function blocks until either a command is available or a timeout occurs.
            /// An exception is thrown if the connection goes down or any other error prevents successful completion.
            /// NOTE: this function is synchronized, so it is thread safe, however it will block until all previous
            /// function calls complete.
            /// </summary>
            /// <param name="theData">The received data</param>
            /// <param name="timeRcvd">The time that this command was received</param>
            /// <param name="optionalTimeout">Amount of time (in ms) before function gives up and returns</param>
            /// <returns>True if another command is present, otherwise false</returns>
            bool ReceiveData(out string theData, out DateTime timeRcvd, int optionalTimeout = 30000);

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
            bool ReceiveData(out string theData, int optionalTimeout = 30000);
            #endregion

            #region send functions
            /// <summary>
            /// Send command (protocol-dependent characters are added here). 
            /// Blocks until protocol determines the message was received or connection fails.
            /// </summary>
            /// <param name="data">the data to send (can be null)</param>
            /// <param name="optionalRetries">Number of retries if protocol supports it</param>
            /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
            /// <returns>True if an ACK was recieved, otherwise false</returns>
            bool SendCommand(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);

            /// <summary>
            /// Send command (protocol-dependent characters are added here). 
            /// Blocks until protocol determines the message was received or connection fails.
            /// </summary>
            /// <param name="data">the data to send (can be null)</param>
            /// <param name="optionalRetries">Number of retries if protocol supports it</param>
            /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
            /// <returns>True if an ACK was recieved, otherwise false</returns>
            bool SendCommand(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);

            /// <summary>
            /// Asynchronous function call for SendCommand()
            /// 
            /// If there is nothing going on with SendCommand(), this function
            /// will first get the data on its way to the comm interface 
            /// before yielding to a worker thread.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="optionalRetries"></param>
            /// <param name="optionalRetryTime"></param>
            /// <param name="callback"></param>
            /// <param name="state"></param>
            /// <returns></returns>
            IAsyncResult BeginSendCommand(byte[] data,
                                                int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS,
                                                AsyncCallback callback = null,
                                                Object state = null);

            
            /// <summary>
            /// Asynchronous function call for SendCommand()
            /// 
            /// If there is nothing going on with SendCommand(), this function
            /// will first get the data on its way to the comm interface 
            /// before yielding to a worker thread.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="optionalRetries"></param>
            /// <param name="optionalRetryTime"></param>
            /// <param name="callback"></param>
            /// <param name="state"></param>            
            /// <returns></returns>
            IAsyncResult BeginSendCommand(string data,
                                                int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS,
                                                AsyncCallback callback = null,
                                                Object state = null);
            
            /// <summary>
            /// Call to end asynchronous sending
            /// </summary>
            /// <param name="ar"></param>
            /// <returns></returns>
            bool EndSendCommand(IAsyncResult ar);


            //
            /// <summary>
            /// Non-blocking send command where the user doesn't care if the message is successfully sent
            /// 
            /// If this function is called before a previous command has finished sending,
            /// the new command will be queued up to sent after the previous command finishes.
            /// 
            /// </summary>
            /// <param name="data"></param>
            /// <param name="optionalRetries"></param>
            /// <param name="optionalRetryTime"></param>
            void SendCommandNB(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);

            //
            /// <summary>
            /// Non-blocking send command where the user doesn't care if the message is successfully sent
            /// 
            /// If this function is called before a previous command has finished sending,
            /// the new command will be queued up to sent after the previous command finishes.
            /// 
            /// </summary>
            /// <param name="data"></param>
            /// <param name="optionalRetries"></param>
            /// <param name="optionalRetryTime"></param>
            void SendCommandNB(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                            int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);
            #endregion

            /// <summary>
            /// Force a clean up of the resources (not safe to use after this is called)
            /// </summary>
            void Dispose();            

        }

        namespace STXETX
        {
            public class CommandAndFunction
            {
                public delegate void CommandFunction(MemoryStream theCommand, IProtocolHandler source);
                public delegate bool CommandPermission(MemoryStream theCommand, IProtocolHandler source);
                public delegate bool CommandSecurity(MemoryStream theCommand, IProtocolHandler source);
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

                    permission = delegate(MemoryStream a, IProtocolHandler b) { return true; };
                    security = delegate(MemoryStream a, IProtocolHandler b) { return true; }; 
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
                    security = delegate(MemoryStream a, IProtocolHandler b) { return true; }; 
                }
            }

            public class StxEtxHandler : IProtocolHandler
            {

                #region properties

                private DateTime _LastSent;
                /// <summary>
                /// The last time the user used the protocol handler to successfully send data.
                /// Note that periodic ping messages do not cause this to be updated.
                /// </summary>
                public DateTime LastSent
                {
                    get { return _LastSent; }
                }

                private ClientContext _CommContext = null;
                /// <summary>
                /// Communication object being used
                /// </summary>
                public ClientContext CommContext
                {
                    get { return _CommContext; }
                    set 
                    { 
                        _CommContext = value;
                        _CommContext.EventCallback = OnClientEvent; 
                    }
                }


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
                private AutoResetEvent NewMsgEvt = new AutoResetEvent(false);

                private LinkedList<EventNotify> EventCallbacks = new LinkedList<EventNotify>();
                private Object EventCallbacksSync = new Object();

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
                    public const int DEF_NB_Safe_TIMEOUT_MS = 60000; //after this much time is passed, NB_Safe is signaled
                    public AutoResetEvent AckFound = null;
                    
                    /// <summary>
                    /// Used for signaling between NB sends and blocking sends
                    /// This is necessary because a NB send tries to get the command
                    /// out before blocking
                    /// </summary>
                    public AutoResetEvent NB_Safe =  new AutoResetEvent(true);

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
                #endregion

                #region constructors
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
                #endregion                

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
                #region helper functions
                void SendDataNB(byte[] data)
                {
                    byte[] theData;
                    SendingContext.AddStxEtx(out theData, data);

                    string dat;
                    if (data != null)
                        dat = System.Text.Encoding.Default.GetString(data);
                    else
                        dat = "";

                    CommContext.LogMsg(TraceEventType.Verbose, "STXETX SENT: <STX>" + dat + "<ETX>");
                    CommContext.Write(theData);
                }

                /// <summary>
                /// Private function called that should ONLY BE CALLED by a SendCommand function 
                /// 
                /// The function assumes that the initial send of data has already taken place
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <returns></returns>
                bool SendDataHandleAck(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS, bool optionalUpdateLastSent = true)
                {
                    bool bFoundAck = false;
                    try
                    {
                        while (optionalRetries >= 0)
                        {
                            bool reply = SendingContext.AckFound.WaitOne(optionalRetryTime);
                            if (reply == true)  //got an ack!
                            {
                                bFoundAck = true;

                                if (optionalUpdateLastSent)
                                    _LastSent = DateTime.Now;

                                //publish successful send to event subscribers
                                PublishEvent(new ClientWroteDataEvent(data, CommContext));
                                break;
                            }
                            else
                            {
                                SendDataNB(data);
                                optionalRetries -= 1;
                            }
                        }
                    }
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
                        //if (StxEtxAck == ACK_SEARCH)
                        //    StxEtxAck = STX_SEARCH;
                    }

                    return bFoundAck;
                }

                /// <summary>
                /// Does the work of SendCommand but gives the option of specifying whether the 
                /// LastSent date time should be updated.
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <param name="UpdateLastSent"></param>
                /// <returns></returns>
                private bool SendCommand(byte[] data, bool UpdateLastSent, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    bool bFoundAck = false;
                    bool bGotNB_Safe = false;
                    try
                    {
                        bGotNB_Safe = SendingContext.NB_Safe.WaitOne(SendContext.DEF_NB_Safe_TIMEOUT_MS);
                        if (!bGotNB_Safe)
                            CommContext.LogMsg(TraceEventType.Error, "Unable to get a lock on the send context after waiting for " + SendContext.DEF_NB_Safe_TIMEOUT_MS / 1000 + " seconds. Breaking the lock.");

                        lock (SendingContext)
                        {
                            SendingContext.AckFound = new AutoResetEvent(false);
                            //StxEtxAck = ACK_SEARCH;

                            SendDataNB(data);
                            bFoundAck = SendDataHandleAck(data, optionalRetries, optionalRetryTime, UpdateLastSent);

                        }
                    }
                    catch (Exception ex)
                    {
                        CommContext.LogMsg(TraceEventType.Error, "Caught an unexpected exception when sending the command: <STX>" + System.Text.Encoding.Default.GetString(data.ToArray()) + "<ETX>. optionalRetries: " + optionalRetries.ToString() + ". Exception: " + ex.ToString());
                    }
                    finally
                    {
                        if (bGotNB_Safe)
                            SendingContext.NB_Safe.Set();
                    }
                    return bFoundAck;
                }
                #endregion

                
                /// <summary>
                /// Send an STX ETX formatted command (STX and ETX are added here). 
                /// Blocks until ack received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if no ACK is received</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if an ACK was recieved, otherwise false</returns>
                public bool SendCommand(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    return SendCommand(data, true, optionalRetries, optionalRetryTime);
                }

                /// <summary>
                /// Send an STX ETX formatted command (STX and ETX are added here). 
                /// Blocks until ack received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if no ACK is received</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if an ACK was recieved, otherwise false</returns>
                public bool SendCommand(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    return SendCommand(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime);
                }


                #region NON-BLOCKING SEND CALLS
                delegate bool AsyncSendCommandCaller(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);
                delegate bool AsyncSendCommandCallerStr(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS);                

                /// <summary>
                /// Asynchronous function call for SendCommand()
                /// 
                /// If there is nothing going on with SendCommand(), this function
                /// will first get the data on its way to the comm interface 
                /// before yielding to a worker thread.
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <param name="callback"></param>
                /// <param name="state"></param>
                /// <returns></returns>
                public IAsyncResult BeginSendCommand(byte[] data,
                                                    int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS,
                                                    AsyncCallback callback = null,
                                                    Object state = null)
                {
                    IAsyncResult result;
                    AsyncSendCommandCaller caller;
                    if (SendingContext.NB_Safe.WaitOne(0))
                    {
                        SendingContext.AckFound = new AutoResetEvent(false);
                        //StxEtxAck = ACK_SEARCH;
                        SendDataNB(data);
                        caller = delegate(byte[] dat, int retry, int retryTime)
                        {
                            bool retVal = false;
                            lock (SendingContext)
                            {
                                retVal = SendDataHandleAck(dat, retry, retryTime);
                                SendingContext.NB_Safe.Set();
                            }
                            return retVal;
                        };                        
                    }
                    else                    
                        caller = new AsyncSendCommandCaller(this.SendCommand);

                    result = caller.BeginInvoke(data, optionalRetries, optionalRetryTime, callback, state);                   
                    return result;
                }

                /// <summary>
                /// Asynchronous function call for SendCommand()
                /// 
                /// If there is nothing going on with SendCommand(), this function
                /// will first get the data on its way to the comm interface 
                /// before yielding to a worker thread.
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                /// <param name="callback"></param>
                /// <param name="state"></param>            
                /// <returns></returns>
                public IAsyncResult BeginSendCommand(string data,
                                                    int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS,
                                                    AsyncCallback callback = null,
                                                    Object state = null)
                {
                    return BeginSendCommand(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime, callback, state);                                        
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
                /// Non-blocking send command where the user doesn't care if the message is successfully sent
                /// 
                /// If this function is called before a previous command has finished sending,
                /// the new command will be queued up to sent after the previous command finishes.
                /// 
                /// </summary>
                /// <param name="data"></param>
                /// <param name="optionalRetries"></param>
                /// <param name="optionalRetryTime"></param>
                public void SendCommandNB(byte[] data,
                                                    int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
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


                public void SendCommandNB(string data,
                                                    int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    SendCommandNB(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime);
                }
                #endregion

                #endregion


                #region Periodic Pinging functions
                /// <summary>
                /// This function enables/disables periodically sending STX ETX messages 
                /// to the remote device to ensure that the connection stays alive and that
                /// the peer is present.
                /// </summary>
                /// <param name="enable">Set to true to periodically send STX ETX messages to the peer</param>
                /// <param name="optionalMaxIdleTime">Maximum connection idle time before an STX ETX should be sent</param>
                public void PeriodicPing(bool enable, TimeSpan MaxIdleTime)
                {
                    PeriodicPingDone.Set(); //end any current pinging
                    if (enable)
                    {                        
                        PeriodicPingDone = new AutoResetEvent(false);
                        PeriodicPingDel caller = this.HandlePeriodicPing;
                        caller.BeginInvoke(MaxIdleTime, delegate(IAsyncResult ar) { caller.EndInvoke(ar); }, null);
                    }                    
                }

                private AutoResetEvent PeriodicPingDone = new AutoResetEvent(false);       //signal this when a helper thread is no longer needed to keep the connection alive
                delegate void PeriodicPingDel(TimeSpan MaxIdleTime);

                /// <summary>
                /// Internal helper function for periodically polling
                /// </summary>
                void HandlePeriodicPing(TimeSpan MaxIdleTime)
                {
                    bool bDone = false;
                    try
                    {
                        while (bDone == false)
                        {
                            TimeSpan timeSinceLast = DateTime.Now - CommContext.lastRcvd;
                            if (timeSinceLast > MaxIdleTime)
                                SendCommand((byte[])null, false);

                            bDone = PeriodicPingDone.WaitOne(MaxIdleTime - timeSinceLast);
                        }
                    }
                    catch
                    {
                        //no need to report this, any excption is most likely due to the session request being canceled
                        //or the client disconnecting
                        //Console.WriteLine(e);
                    }
                }
                #endregion

                #region Protocol communication events
                /// <summary>
                /// Registers a callback to receive protocol communication events:
                /// ClientReceivedDataEvent, ClientWroteDataEvent,
                /// and ClientDisconnectedEvent. These events are generated
                /// asynchronously and contain protocol-level information. For
                /// example, for STX/ETX handlers, STX, ETX, and ACK will not be
                /// included in the data when these events are created; futhermore
                /// a ClientWroteDataEvent will only be created after the protocol
                /// knows that the client has received the data (ACK received).
                /// </summary>
                /// <param name="EventCallback">The delegate to call</param>
                public void AddCommEventHandler(EventNotify EventCallback)
                {
                    lock (EventCallbacksSync)
                    {
                        EventCallbacks.AddLast(EventCallback);
                    }
                }

                /// <summary>
                /// Unregisters a callback delegate from receiving protocol 
                /// communication events.
                /// </summary>
                /// <param name="EventCallback"></param>
                public void RemoveCommEventHandler(EventNotify EventCallback)
                {
                    lock (EventCallbacksSync)
                    {
                        EventCallbacks.Remove(EventCallback);
                    }
                }

                #region private helper functions
                /// <summary>
                /// Called to publish the events in an asynchronous manner
                /// so that we don't slow anything down in the main operation.
                /// </summary>
                /// <param name="theEvent"></param>
                private void PublishEvent(ClientEvent theEvent)
                {
                    lock (EventCallbacksSync)
                    {
                        foreach (EventNotify callback in EventCallbacks)
                        {
                            try
                            {
                                callback.BeginInvoke(theEvent, delegate(IAsyncResult arr) { callback.EndInvoke(arr); }, null);
                            }
                            catch (Exception ex)
                            {
                                CommContext.LogMsg(TraceEventType.Warning, "Protocol callback failed: " + ex.ToString());
                            }
                        }
                    }
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

                        //notify event subscribers
                        PublishEvent(theEvent);
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
                            else
                            {
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
                                        else if (theByte == 0x02)                                        
                                            ReceivedData = new MemoryStream();                                                                                    
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
                                }
                            }
                        }

                    }
                }


                #endregion

                #region Processing a recieved command
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

                    char inCmdTable = 'M';
                    if (CommandTable != null)                    
                        inCmdTable = DoCmdTable(theCommand);                    
                    if ((inCmdTable != '0') && bAllowAllCmds)
                    {
                        ReceivedMsgLog msglog = new ReceivedMsgLog(cmd);
                        RxMsgs.Enqueue(msglog);
                        NewMsgEvt.Set();
                    }
                    else if ((inCmdTable != '0') && !bAllowAllCmds && cmd.Length > 0)
                        SendCommandNB(System.Text.Encoding.ASCII.GetBytes("ER" + inCmdTable));

                    //if we received valid data, publish received data to event subscribers
                    if ((inCmdTable == '0') || (bAllowAllCmds && cmd.Length > 0) )                        
                        PublishEvent(new ClientWroteDataEvent(theCommand.ToArray(), CommContext));

                    return true;
                }


                /// <summary>
                /// Process a command
                /// </summary>
                /// <param name="theCommand"></param>
                /// <returns>a 1 character error code. '0' for no error, 'M' is a generic error, 'L' for an communication link which is too insecure, 'P' for a user account which does not have permission</returns>
                char DoCmdTable(MemoryStream theCommand)
                {
                    String cmd = System.Text.Encoding.Default.GetString(theCommand.ToArray());
                    CommandAndFunction[] entries = CommandTable.ToArray<CommandAndFunction>();

                    bool bFound = false;
                    bool bSecurity = false;
                    bool bPermission = false;
                    char errorCode = '0';
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
                                    else
                                        errorCode = 'P';
                                } 
                                else 
                                    errorCode = 'L';

                                if (bSecurity && bPermission)
                                    entry.theFunction(theCommand, this);
                            }
                            catch (Exception e)
                            {
                                CommContext.LogMsg(TraceEventType.Error, "Executing lookup table command " + cmd + " caused exception: " + e.ToString());                                
                                errorCode = 'M';                                
                            }

                            break;
                        }

                    }

                    if (errorCode == '0' && !bFound)
                        errorCode = 'M';

                    return errorCode; // (bFound && bSecurity && bPermission);        //true indicates data was sent
                }
                #endregion
            }
        }
    }
}
