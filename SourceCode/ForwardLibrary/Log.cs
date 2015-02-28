using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    namespace Log
    {
        using ForwardLibrary.Default;
        using ForwardLibrary.Exceptions;
        using System;
        using System.Collections.Concurrent;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.IO;
        using System.IO.Compression;
        using System.Linq;
        using System.Runtime.Serialization;
        using System.Runtime.Serialization.Formatters.Binary;
        using System.Security.Cryptography;
        using System.Text;

        /// <summary>
        /// Thrown to report a log rule violation
        /// </summary>
        public class LogRuleException : Exception
        {
            public ILogRule Rule;

            public LogRuleException(string message, ILogRule rule)
                : base(message)
            {
                Rule = rule;
            }

            public LogRuleException(string message, ILogRule rule, Exception innerException)
                : base(message, innerException)
            {
                Rule = rule;
            }


            public override string ToString()
            {
                StringBuilder description = new StringBuilder();
                description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);
                description.AppendFormat("\r\nRule: {0}", Rule.ToString());

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

        #region log rules
        /// <summary>
        /// Log rules. Some rules will throw an exception when
        /// violated, others will simply correct the problem.
        /// </summary>
        public interface ILogRule
        {
            void CheckLog(ConcurrentQueue<ILogEntry> theLog);            
        }        


        /// <summary>
        /// Log rule that enforces a maximum number of enforces
        /// by flushing out the oldest entries if the log
        /// exceeds the maximum number of entries (until the log
        /// is down to size)
        /// </summary>
        public class MaxEntryRuleFlush : ILogRule
        {

            private long _MaxLogEntries = 1500;
            public long MaxLogEntries
            {
                get { return _MaxLogEntries; }
                set { _MaxLogEntries = value; }
            }            

            /// <summary>
            /// Check the number of entries in the log. If there are too many
            /// remove the first ones added
            /// </summary>
            /// <param name="theLog"></param>
            public void CheckLog(ConcurrentQueue<ILogEntry> theLog)
            {
                ILogEntry overflow;
                while (theLog.Count > MaxLogEntries)
                    theLog.TryDequeue(out overflow);
            }

        }

        /// <summary>
        /// Throws an exception if the log exceeds the maximum number
        /// of entries
        /// </summary>
        public class MaxEntryRule : ILogRule
        {
            private long _MaxLogEntries = 1500;
            public long MaxLogEntries
            {
                get { return _MaxLogEntries; }
                set { _MaxLogEntries = value; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="theLog"></param>
            /// <exception cref="LogRuleException">Thrown when the log exceeds the maximum number of entries</exception>
            public void CheckLog(ConcurrentQueue<ILogEntry> theLog)
            {
                if (theLog.Count > MaxLogEntries)
                    throw new LogRuleException(String.Format(
                        "The log contains {0} entries, which exceeds the maximum number of {1}.", theLog.Count, MaxLogEntries),
                        this);
            }
        }

        /// <summary>
        /// Throws an exception if the concatenation of the Log Msg fields
        /// exceeds a maximum length.
        /// </summary>
        public class TotalMsgLengthRule : ILogRule
        {
            private long _MaxLength = 1024 * 1024 * 1; //1MB limit

            /// <summary>
            /// The maximum number of characters that can be stored in the combined log
            /// Msg field without violating this rule.
            /// </summary>
            public long MaxLength
            {
                get { return _MaxLength; }
                set { _MaxLength = value; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="theLog"></param>
            /// <exception cref="LogRuleException">Thrown when the combined log Msg strings contain more characters than MaxLength</exception>
            public void CheckLog(ConcurrentQueue<ILogEntry> theLog)
            {
                long Count = 0;
                foreach (ILogEntry entry in theLog)
                    Count += entry.Msg.Length;

                if (Count > _MaxLength)
                    throw new LogRuleException(String.Format(
                        "The log message contains {0} characters, which exceeds the maximum number of {1}.", Count, MaxLength),
                        this);
            }
        }


        /// <summary>
        /// Deletes entries which are older than EntryLifeTime.
        /// Assumes that the log was populated in chronological order 
        /// (oldest entries are first out).
        /// </summary>
        public class FlushOldEntriesRule : ILogRule
        {           
            public TimeSpan EntryLifeTime { get; set; }
            
            void ILogRule.CheckLog(ConcurrentQueue<ILogEntry> theLog)
            {
                ILogEntry entry;                
                theLog.TryPeek(out entry);
                while ((theLog.Count > 1) && (EntryLifeTime.CompareTo(DateTime.Now - entry.DateTime) < 0))
                {
                    theLog.TryDequeue(out entry);
                    theLog.TryPeek(out entry);
                }                             
            }
        }
        #endregion

        #region log string formatters
        /// <summary>
        /// Interface for log string formatters
        /// </summary>
        public interface ILogStringFormatter
        {
            /// <summary>
            /// Determines if this log formatter applies to this log entry
            /// </summary>
            /// <param name="entry"></param>
            /// <returns></returns>
            Boolean DoesFormatterApply(ILogEntry entry);

            /// <summary>
            /// Formats the log entry as a string
            /// </summary>
            /// <param name="entry"></param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException">Thrown if this formatter does not apply to this log entry</exception>
            string PrintEntry(ILogEntry entry);
        }

        /// <summary>
        /// Creates a string that only consists of the message field
        /// </summary>
        public class MsgLogString : ILogStringFormatter
        {
            public bool DoesFormatterApply(ILogEntry entry)
            {
                return true;
            }

            public string PrintEntry(ILogEntry entry)
            {
                return entry.Msg;
            }
        }

        /// <summary>
        /// Create a string by using the entry's ToString() function.
        /// </summary>
        public class EntryToString : ILogStringFormatter
        {
            public bool DoesFormatterApply(ILogEntry entry)
            {
                return true;
            }

            public string PrintEntry(ILogEntry entry)
            {
                return entry.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Wrapper class to contain the log, expose a stream to it,
        /// convert the log to a string, and provide rule checking logic
        /// for when new entries are added to the log, and 
        /// </summary>
        public class LogContainer
        {
            /// <summary>
            /// The rules for the log entries. The order matters. 
            /// Entries which appear first in the array are used first.
            /// 
            /// For example:  suppose rule (a) flushes old entries and 
            /// rule (b) throws an exception if two many entries are
            /// present. If rule (a) is first, it will discard any old
            /// entries before rule (b) is called, potentially avoiding
            /// rule (b) throwing an exception.
            /// </summary>
            public ILogRule[] Rules { get; set; }            

            /// <summary>
            /// The log entries
            /// </summary>
            public ILogEntry[] Entries
            {
                get
                {
                    return theLog.ToArray();
                }
            }

            protected ConcurrentQueue<ILogEntry> theLog = new ConcurrentQueue<ILogEntry>();

            /// <summary>
            /// Create the log with the default rules
            /// </summary>
            public LogContainer()
            {                
                Rules = new ILogRule[] {new MaxEntryRuleFlush()};
            }

            /// <summary>
            /// Create the log with the specified rules. The rules should be
            /// listed in the order that they should be checked when a new
            /// entry is added.
            /// </summary>
            /// <param name="rules"></param>
            public LogContainer(ILogRule[] rules)
            {
                Rules = rules;
            }

            /// <summary>
            /// Add a new entry to the log.
            /// </summary>
            /// <param name="entry"></param>
            /// <exception cref="LogRuleException">Thrown when adding the entry failed because a log maintenance rule was violated</exception>
            public virtual void Enqueue(ILogEntry entry)
            {
                CheckTheLog();
                theLog.Enqueue(entry);                
            }

            /// <summary>
            /// Replaces the log with data from logStream.
            /// 
            /// Note that the stream is not closed by the function. It is
            /// up to the calling application to properly dispose of the stream
            /// (for example, through a using block).
            /// </summary>
            /// <param name="logStream"></param>
            public void ReadLogIn(Stream logStream)
            {
                BinaryFormatter deserializer = new BinaryFormatter();
                theLog = (ConcurrentQueue<ILogEntry>)deserializer.Deserialize(logStream);                
            }

            /// <summary>
            /// Replaces the log with data from logStream
            /// 
            /// Note that the stream is not closed/disposed by the function. It is
            /// up to the calling application to properly dispose of the stream
            /// (for example, through a using block).
            /// </summary>
            /// <param name="logStream"></param>
            public void ReadLogIn(Stream logStream, Boolean Compressed = false)
            {
                if (Compressed == false)
                    ReadLogIn(logStream);

                else
                {
                    //using block insures that GZipStream is properly disposed
                    using (GZipStream compressionStream = new GZipStream(logStream, CompressionMode.Decompress))
                    {
                        ReadLogIn(compressionStream);
                    }
                }
            }

            /// <summary>
            /// Replaces the log with data from filename
            /// </summary>
            /// <param name="filename"></param>
            /// <param name="Compressed"></param>
            public void ReadLogIn(String filename, Boolean Compressed = false)
            {                
                if (Compressed == false)
                    using (FileStream f = new FileStream(filename, FileMode.Open))
                    {
                        ReadLogIn(f);
                    }
                else
                {
                    //using block insures that GZipStream is properly disposed
                    using (GZipStream compressionStream = 
                        new GZipStream(new FileStream(filename, FileMode.Open), CompressionMode.Decompress))                    
                    {
                        ReadLogIn(compressionStream);
                    }
                }                
            }

            /// <summary>
            /// Serialized the log and writes it to a stream
            /// 
            /// Note that the stream is not closed/disposed by the function. It is
            /// up to the calling application to properly dispose of the stream
            /// (for example, through a using block).
            /// </summary>
            /// <param name="logStream"></param>
            public void WriteLogOut(Stream logStream)
            {

                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(logStream, theLog);                
            }

            /// <summary>
            /// Serialized the log and writes it to a stream which optionally is compressed
            /// </summary>
            /// <param name="logStream"></param>
            /// <param name="Compress"></param>
            public void WriteLogOut(Stream logStream, Boolean Compress = false)
            {
                if (Compress == false)
                    WriteLogOut(logStream);
                else
                {
                    GZipStream compressionStream = new GZipStream(logStream, CompressionMode.Compress, true);
                    WriteLogOut(compressionStream);
                    compressionStream.Dispose();
                }
            }

            /// <summary>
            /// Serialized the log and saves it to a file.
            /// </summary>
            /// <param name="filename"></param>
            /// <param name="Compress"></param>
            public void WriteLogOut(String filename, Boolean Compress = false)
            {
                if (Compress == false)
                    using (FileStream f = new FileStream(filename, FileMode.Create))
                    {
                        WriteLogOut(f);
                    }
                else
                {
                    using (GZipStream compressionStream = new GZipStream(new FileStream(filename, FileMode.Create), CompressionMode.Compress))
                    {
                        WriteLogOut(compressionStream);
                    }
                }
            }

            /// <summary>
            /// Expose a stream that can be used to read the contents of theLog. 
            /// 
            /// It is the caller's responsibility to ensure that the stream is properly
            /// disposed (i.e. through a using block) to prevent a temporary memory leak
            /// (until the garbage collector runs).
            /// </summary>
            /// <returns></returns>
            public Stream ExposeStream(Boolean Compress = false)
            {                
                MemoryStream ms = new MemoryStream();
                WriteLogOut(ms, Compress);
                return ms;
            }

            /// <summary>
            /// Constructs a string of the log entries by using the ToString()
            /// function of each log entry.
            /// 
            /// This is done sequentially on each entry in Entries starting at the
            /// beginning of Entries. 
            /// </summary>
            /// <param name="ConcatString"></param>
            /// <returns>Inserted between entries. If null or empty, this is skipped</returns>
            public string ToString(string ConcatString)
            {
                return ToString(new EntryToString(), ConcatString);                
            }

            /// <summary>
            /// Constructs a string of the log entries by using the formatter
            /// specified.
            /// 
            /// This is done sequentially on each entry in Entries starting at the
            /// beginning of Entries. PrintEntry of the formatter (if it applies) is 
            /// called on the each entry.
            /// </summary>
            /// <param name="formatters"></param>
            /// <param name="ConcatString">Inserted between entries. If null or empty, this is skipped</param>
            /// <returns></returns>
            public string ToString(ILogStringFormatter formatter, string ConcatString)
            {
                ILogStringFormatter[] formatters = { formatter };
                return ToString(formatters, ConcatString);
            }

            /// <summary>
            /// Constructs a string of the log entries by using the formatters
            /// specified in formatters.
            /// 
            /// This is done sequentially on each entry in Entries starting at the
            /// beginning of Entries. PrintEntry of formatter (which applies) is 
            /// called on the entry in the order that the formatter appears in formatters.
            /// </summary>
            /// <param name="formatters"></param>
            /// <param name="ConcatString">Inserted between entries. If null or empty, this is skipped</param>
            /// <returns></returns>
            public string ToString(ILogStringFormatter[] formatters, string ConcatString)
            {                
                StringBuilder building = new StringBuilder();

                foreach (ILogEntry entry in Entries)
                {
                    foreach (ILogStringFormatter formatter in formatters)
                    {
                        if (formatter.DoesFormatterApply(entry))
                        {
                            if ( (building.Length > 0) && (!String.IsNullOrEmpty(ConcatString)) )
                                building.Append(ConcatString);
                            building.Append(formatter.PrintEntry(entry));
                        }
                    }
                }
                return building.ToString();
            }

            

            /// <summary>
            /// Call this to run all of the check rules. An exception is thrown in the first rule that fails.
            /// </summary>
            /// <exception cref="LogRuleException">Thrown if a log maintenance rule was violated and the log should not accept new entries</exception>
            public void CheckTheLog()
            {
                lock (this)
                {
                    foreach (ILogRule rule in Rules)
                    {
                        rule.CheckLog(theLog);
                    }
                }
            }                        
        }

        /// <summary>
        /// The FPS trace listener used for application logging
        /// </summary>
        public class ForwardTraceListener : TraceListener
        {
            public LogContainer theLog;
            public DateTime LastWriteTime = DateTime.Now;


            public ForwardTraceListener(LogContainer log)
            {
                theLog = log;
            }

            AppLogEntry currentEntry = null;
            protected virtual void LogUpdated()
            {

            }

            public override void Write(String value)
            {
                try
                {                    
                    currentEntry = new AppLogEntry(value);
                    theLog.Enqueue(currentEntry);
                    LastWriteTime = DateTime.Now;                    
                    LogUpdated();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to create / enqueue log item for " + value + ". Caught exception {0}.", e);
                }
            }

            public override void WriteLine(String value)
            {
                try
                {                    
                    currentEntry.AddInfo(value);
                    LastWriteTime = DateTime.Now;                    
                    LogUpdated();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to write to log item for " + value + ". Caught exception {0}.", e);
                }
            }

        }
        
        /// <summary>
        /// Interface for log entries
        /// </summary>
        public interface ILogEntry : ISerializable
        {
            DateTime DateTime { get; set; }
            string Msg { get; set; }
        }

        /// <summary>
        /// Log entry used for logging simple device messages
        /// </summary>
        [Serializable]
        public class DeviceLogEntry : ILogEntry
        {
            public UInt32 SequenceNumber { get; set; }
            public DateTime DateTime { get; set; }
            public string Msg { get; set; }

            /// <summary>
            /// Create a log entry from a shallow copy
            /// </summary>
            /// <param name="log"></param>
            public DeviceLogEntry(DeviceLogEntry log)
            {
                SequenceNumber = log.SequenceNumber;
                DateTime = log.DateTime;
                Msg = log.Msg;
            }

            /// <summary>
            /// Create an empty log entry
            /// </summary>
            /// <param name="log"></param>
            public DeviceLogEntry()
            {
                DateTime = DateTime.Now;
            }

            /// <summary>
            /// Convert entry to string using the default format
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Msg;
            }
            

            #region Functions necessary for serialization and deserialization
            /// <summary>
            /// Implement this method to serialize data. The method is called  
            /// on serialization. 
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Use the AddValue method to specify serialized values.
                info.AddValue("SequenceNumber", SequenceNumber, typeof(UInt32));
                info.AddValue("DateTime", DateTime, typeof(DateTime));
                info.AddValue("Msg", Msg, typeof(string));                
            }

            /// <summary>
            /// The special constructor is used to deserialize values. 
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public DeviceLogEntry(SerializationInfo info, StreamingContext context)
            {
                // Reset the property value using the GetValue method.
                SequenceNumber = (UInt32)info.GetValue("SequenceNumber", typeof(UInt32));
                DateTime = (DateTime)info.GetValue("DateTime", typeof(DateTime));
                Msg = (string)info.GetValue("Msg", typeof(string));                
            }            
            #endregion
        }

        /// <summary>
        /// Custom creation factory for DeviceLogEntries. Used to create
        /// these entries from commands.
        /// </summary>
        public class DeviceLogEntryFactory
        {
            /// <summary>
            /// Create a log entry from a command string. The following commands are currently supported
            /// ALMSG=[SeqNum],[Hash],[Msg]
            /// SeqNum: 32bit sequence number
            /// Hash: SHA256 hash of the MSG field: 64 characters of ascii-coded hex
            /// Msg: The data being logged: may contain any type of characters
            /// </summary>
            /// <param name="incoming"></param>
            /// <exception cref="System.ArgumentException">Thrown when parsing failed. The parameter field indicates which field had a problem.</exception>
            /// <exception cref="ForwardLibrary.Default.ChecksumMismatchException">Thrown when the hashes mismatch.</exception>
            public static DeviceLogEntry FromCommand(string incoming)
            {
                DeviceLogEntry entry;
                string[] type = incoming.Split('=');
                if (type[0].StartsWith("ALMSG"))
                    entry = CreateFromALMSG(type[1]);
                else
                    throw new ArgumentException("Unsupported command format found.");

                return entry;
            }

            /// <summary>
            /// Create a log entry from the fields of an ALMSG command. The following commands are currently supported
            /// [SeqNum],[Hash],[Msg]
            /// SeqNum: 32bit sequence number
            /// Hash: SHA256 hash of the MSG field: 64 characters of ascii-coded hex
            /// Msg: The data being logged: may contain any type of characters
            /// </summary>
            /// <param name="incoming"></param>
            /// <exception cref="System.ArgumentException">Thrown when parsing faild. The parameter field indicates which field had a problem.</exception>
            /// <exception cref="ForwardLibrary.Default.ChecksumMismatchException">Thrown when the hashes mismatch.</exception>
            private static DeviceLogEntry CreateFromALMSG(string incoming)
            {
                string[] fields = incoming.Split(',');
                byte[] hashRcvd;
                
                //assign the sequence number
                UInt32 SeqNum = ConvertToSeqNum(fields[0]);
                
                //get the hash
                hashRcvd = ConvertToHash(fields[1]);

                //now find the message string, it will start after the second comma                
                string msg = GetMsgStr(incoming);
                
                //check the hash
                VerifyHash(hashRcvd, msg);

                //Create the object
                DeviceLogEntry entry = new DeviceLogEntry()
                {
                    Msg = msg,
                    SequenceNumber = SeqNum,
                    DateTime = DateTime.Now
                };
                return entry;
            }

            /// <summary>
            /// Match the hash received against a hesh of the Msg field
            /// </summary>
            /// <param name="hashRcvd"></param>
            /// <exception cref="ForwardLibrary.Default.ChecksumMismatchException">Thrown when the hashes mismatch.</exception>
            private static void VerifyHash(byte[] hashRcvd, string Msg)
            {
                SHA256 sha256Hash = SHA256Managed.Create();
                byte[] hashCalc = sha256Hash.ComputeHash(System.Text.Encoding.Default.GetBytes(Msg));
                if (!hashRcvd.SequenceEqual(hashCalc))
                    throw new ChecksumMismatchException("SHA256 hash of Msg does not match the hash received.");

            }


            /// <summary>
            /// Parses the message string
            /// </summary>
            /// <param name="incoming"></param>
            /// <exception cref="System.ArgumentException">Thrown when parsing failed.</exception>
            private static string GetMsgStr(string incoming)
            {
                string Msg;
                //Msg string will start after the second comma
                try
                {
                    int Index = incoming.IndexOf(",");
                    Index = incoming.IndexOf(",", Index + 1);
                    Msg = incoming.Substring(Index + 1);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Failed to find the Msg field", "Msg", ex);
                }
                return Msg;
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="seqString"></param>
            /// <exception cref="System.ArgumentException">Failed to parse the sequence number field.</exception>
            private static UInt32 ConvertToSeqNum(string seqString)
            {
                UInt32 SequenceNumber;
                try
                {
                    SequenceNumber = UInt32.Parse(seqString.Trim());
                }
                catch(Exception ex)
                {
                    throw new ArgumentException("Failed to interpret the sequence number field", "SeqNum", ex);
                }
                return SequenceNumber;
            }

            /// <summary>
            /// Converts the 
            /// </summary>
            /// <param name="hashField"></param>
            /// <returns></returns>
            private static byte[] ConvertToHash(string hashField)
            {
                hashField = hashField.Trim();
                byte[] hash;
                if (hashField.Length != 64)
                    throw new ArgumentException("Received invalid hash length.", "hash");

                try
                {
                    hash = FPS_LibFuncs.AsciiEncodedHexStringToByteArray(hashField);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Failed to interpret the hash field", "hash", ex);
                }
                return hash;
            }
        }

        /// <summary>
        /// This class is used for application log events generated by a trace source
        /// </summary>
        [Serializable]
        public class AppLogEntry : ILogEntry
        {
            public string source { get; set; }
            public string eventType { get; set; }
            public string eventID { get; set; }
            public string Msg { get; set; }
            public string ProcessID { get; set; }
            public string ThreadID { get; set; }
            public DateTime DateTime { get; set; }
            public string Timestamp { get; set; }
            public string Callstack { get; set; }


            /// <summary>
            /// Create a log entry from shallow clone of another object
            /// </summary>
            /// <param name="log"></param>
            public AppLogEntry(AppLogEntry log)
            {
                source = log.source;
                eventType = log.eventType;
                eventID = log.eventID;
                Msg = log.Msg;
                ProcessID = log.ProcessID;
                ThreadID = log.ThreadID;
                DateTime = log.DateTime;
                Timestamp = log.Timestamp;
                Callstack = log.Callstack;
            }

            /// <summary>
            /// Create a log entry from a trace source event
            /// </summary>
            /// <param name="entry"></param>
            public AppLogEntry(String entry)
            {
                string[] fields = entry.Split(':');
                string nameAndType = fields[0];
                string[] subFields = nameAndType.Split(' ');
                source = subFields[0];
                eventType = subFields[1];
                eventID = fields[1].Trim();
            }

            /// <summary>
            /// Add information from a trace source event
            /// </summary>
            /// <param name="info"></param>
            public void AddInfo(string info)
            {
                info = info.Trim();
                if (info.StartsWith("ProcessId="))
                    ProcessID = info.Split('=')[1].Trim();
                else if (info.StartsWith("ThreadId="))
                    ThreadID = info.Split('=')[1].Trim();
                else if (info.StartsWith("DateTime="))
                    DateTime = DateTime.Parse(info.Split('=')[1].Trim());
                else if (info.StartsWith("Timestamp="))
                    Timestamp = info.Split('=')[1].Trim();
                else if (info.StartsWith("Callstack="))
                    Callstack = info.Split('=')[1].Trim();
                else
                    Msg = info;

            }

            /// <summary>
            /// Custom ToString()
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string val = "";
                if (source != null)
                    val = val + "source=" + source + ";";
                if (eventType != null)
                    val = val + "eventType=" + eventType + ";";
                if (eventID != null)
                    val = val + "eventID=" + eventID + ";";
                if (Msg != null)
                    val = val + "Msg=" + Msg + ";";
                if (ProcessID != null)
                    val = val + "ProcessID=" + ProcessID + ";";
                if (ThreadID != null)
                    val = val + "ThreadID=" + ThreadID + ";";
                if (DateTime != null)
                    val = val + "DateTime=" + DateTime.ToString() + ";";
                if (Timestamp != null)
                    val = val + "Timestamp=" + Timestamp + ";";
                if (Callstack != null)
                    val = val + "Callstack=" + Callstack + ";";

                return val;
            }

            /// <summary>
            /// This function is used to retrieve a string formatted for manual mode
            /// </summary>
            /// <returns></returns>
            public string ToManualString()
            {
                string res;
                //okay, so we know that it is an STXETX message
                if (Msg.StartsWith("STXETX SENT: "))
                {                    
                    res = "Sent (" + DateTime.ToString("HH:mm:ss.ff") + ")--> " + Msg.Substring(13);
                }
                else if (Msg.StartsWith("STXETX RCVD: "))
                {                    
                    res = "Rcvd (" + DateTime.ToString("HH:mm:ss.ff") + ")<-- " + Msg.Substring(13);
                }
                else
                {
                    //some kind of system status message                    
                    res = "(" + DateTime.ToString("HH:mm:ss.ff") + ") " + Msg;
                }

                return res;                
            }

            #region Functions necessary for serialization and deserialization
            /// <summary>
            /// Implement this method to serialize data. The method is called  
            /// on serialization. 
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Use the AddValue method to specify serialized values.
                info.AddValue("source", source, typeof(string));
                info.AddValue("eventType", eventType, typeof(string));
                info.AddValue("eventID", eventID, typeof(string));
                info.AddValue("Msg", Msg, typeof(string));
                info.AddValue("ProcessID", ProcessID, typeof(string));
                info.AddValue("ThreadID", ThreadID, typeof(string));
                info.AddValue("DateTime", DateTime, typeof(DateTime));
                info.AddValue("Timestamp", Timestamp, typeof(string));
                info.AddValue("Callstack", Callstack, typeof(string));
            }

            /// <summary>
            /// The special constructor is used to deserialize values. 
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public AppLogEntry(SerializationInfo info, StreamingContext context)
            {
                // Reset the property value using the GetValue method.
                source = (string)info.GetValue("source", typeof(string));
                eventType = (string)info.GetValue("eventType", typeof(string));
                eventID = (string)info.GetValue("eventID", typeof(string));
                Msg = (string)info.GetValue("Msg", typeof(string));
                ProcessID = (string)info.GetValue("ProcessID", typeof(string));
                ThreadID = (string)info.GetValue("ThreadID", typeof(string));
                DateTime = (DateTime)info.GetValue("DateTime", typeof(DateTime));
                Timestamp = (string)info.GetValue("Timestamp", typeof(string));
                Callstack = (string)info.GetValue("Callstack", typeof(string));
            }            
            #endregion
        }

        
    }
}
