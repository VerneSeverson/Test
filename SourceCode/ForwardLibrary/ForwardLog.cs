using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    namespace Log
    {
        using System;
        using System.Collections.Concurrent;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.IO;
        using System.IO.Compression;
        using System.Linq;
        using System.Runtime.Serialization.Formatters.Binary;
        using System.Text;



        public class ForwardLog 
        {
            
            public long MaxLogEntries = 1500;

            public LogEntry[] Entries
            {
                get
                {
                    return theLog.ToArray();
                }
            }

            private ConcurrentQueue<LogEntry> theLog = new ConcurrentQueue<LogEntry>();
            public void Enqueue(LogEntry entry)
            {
                theLog.Enqueue(entry);
                lock (this)
                {
                    LogEntry overflow;
                    while (theLog.Count > MaxLogEntries)
                        theLog.TryDequeue(out overflow);
                }
            }
            public void ReadLogIn(Stream logStream)
            {
                BinaryFormatter deserializer = new BinaryFormatter();
                theLog = (ConcurrentQueue<LogEntry>)deserializer.Deserialize(logStream);
                logStream.Close();
            }

            public void ReadLogIn(String filename, Boolean Compressed = false)
            {
                if (Compressed == false)
                    ReadLogIn(new FileStream(filename, FileMode.Open));
                else
                {
                    GZipStream compressionStream = new GZipStream(new FileStream(filename, FileMode.Open), CompressionMode.Decompress);
                    ReadLogIn(compressionStream);
                }
            }


            public void WriteLogOut(Stream logStream)
            {

                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(logStream, theLog);
                logStream.Close();
            }

            public void WriteLogOut(String filename, Boolean Compress = false)
            {
                if (Compress == false)
                    WriteLogOut(new FileStream(filename, FileMode.Create));
                else
                {
                    GZipStream compressionStream = new GZipStream(new FileStream(filename, FileMode.Create), CompressionMode.Compress);
                    WriteLogOut(compressionStream);
                }
            }

            /// <summary>
            /// Expose a stream that can be used to read the contents of theLog. 
            /// 
            /// If the stream Close() function is not called, a temp file will be left in the temp folder.
            /// </summary>
            /// <returns></returns>
            public Stream ExposeStream(Boolean Compress = false)
            {
                string tempFile = Path.GetTempFileName();
                WriteLogOut(tempFile, Compress);
                return new ForwardLogStream(tempFile);
            }




            private class ForwardLogStream : FileStream
            {
                String fileName;
                public ForwardLogStream(string file) : base(file, FileMode.Open)
                {                    
                    fileName = file;
                }

                public override void Close()
                {
                    base.Close();
                    File.Delete(fileName);
                } 
            }

            


        }


        public class ForwardTraceListener : TraceListener
        {
            //public ConcurrentQueue<LogEntry> theLog;
            public ForwardLog theLog;
            public DateTime LastWriteTime = DateTime.Now;

            /*static int tempInt = 0;
            private string name;
            StreamWriter wr;*/
            /*public ForwardTraceListener(ConcurrentQueue<LogEntry> LogToUse)
            {
                theLog = LogToUse;
            }*/

            public ForwardTraceListener(ForwardLog log)
            {
                theLog = log;

                //TEMP DEBUG:
                /*name = "C:\\BNAC_ServerLogFolder\\FW_LISTENER_Log_" + tempInt.ToString() + ".txt";
                tempInt++;
                wr = new StreamWriter(new FileStream(name, FileMode.Create));
                wr.AutoFlush = true;*/
            }

            LogEntry currentEntry = null;
            protected virtual void LogUpdated()
            {

            }

            public override void Write(String value)
            {
                try
                {                    
                    currentEntry = new LogEntry(value);
                    theLog.Enqueue(currentEntry);
                    LastWriteTime = DateTime.Now;
                    /*wr.WriteLine("Write() called with value: " + value);
                    wr.WriteLine("Fields now equal: " + currentEntry.ToString());*/
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
                    /*wr.WriteLine("Write() called with value: " + value);
                    wr.WriteLine("Fields now equal: " + currentEntry.ToString());*/
                    LogUpdated();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to write to log item for " + value + ". Caught exception {0}.", e);
                }
            }

        }

        [Serializable]
        public class LogEntry
        {
            public string source { get; set; }
            public string eventType { get; set; }
            public string eventID { get; set; }
            public string Msg { get; set; }
            public string ProcessID { get; set; }
            public string ThreadID { get; set; }
            public string DateTime { get; set; }
            public string Timestamp { get; set; }
            public string Callstack { get; set; }

            //create a log entry through a shallow clone of an already existing log entry
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
                    val = val + "DateTime=" + DateTime + ";";
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
                    DateTime dt = System.DateTime.Parse(DateTime);
                    res = "Sent (" + dt.ToString("HH:mm:ss.ff") + ")--> " + Msg.Substring(13);
                }
                else if (Msg.StartsWith("STXETX RCVD: "))
                {
                    DateTime dt = System.DateTime.Parse(DateTime);
                    res = "Rcvd (" + dt.ToString("HH:mm:ss.ff") + ")<-- " + Msg.Substring(13);
                }
                else
                {
                    //some kind of system status message
                    DateTime dt = System.DateTime.Parse(DateTime);
                    res = "(" + dt.ToString("HH:mm:ss.ff") + ") " + Msg;
                }

                return res;                
            }

            public LogEntry(LogEntry log)
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

            public LogEntry(String entry)
            {
                string[] fields = entry.Split(':');
                string nameAndType = fields[0];
                string[] subFields = nameAndType.Split(' ');
                source = subFields[0];
                eventType = subFields[1];
                eventID = fields[1].Trim();
            }
            public void AddInfo(string info)
            {
                info = info.Trim();
                if (info.StartsWith("ProcessId="))
                    ProcessID = info.Split('=')[1].Trim();
                else if (info.StartsWith("ThreadId="))
                    ThreadID = info.Split('=')[1].Trim();
                else if (info.StartsWith("DateTime="))
                    DateTime = info.Split('=')[1].Trim();
                else if (info.StartsWith("Timestamp="))
                    Timestamp = info.Split('=')[1].Trim();
                else if (info.StartsWith("Callstack="))
                    Callstack = info.Split('=')[1].Trim();
                else
                    Msg = info;

            }
        }

        
    }
}
