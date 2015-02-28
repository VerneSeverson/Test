using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    namespace Exceptions
    {
        /// <summary>
        /// Thrown when a checksum or hash mismatch has occurred.
        /// </summary>
        public class ChecksumMismatchException : Exception
        {
            public ChecksumMismatchException(string message)
                : base(message)
            { }
        }

        public class EntryNotFoundException : Exception
        {
            public EntryNotFoundException(string message)
                : base(message)
            { }
        }

        public class CredentialMismatchException : Exception
        {
            public CredentialMismatchException(string message)
                : base(message)
            { }
        }

        /// <summary>
        /// Thrown when there is no valid password.
        /// </summary>
        public class NoValidPasswordException : Exception
        {
            public NoValidPasswordException(string message)
                : base(message)
            { }
        }

        /// <summary>
        /// Thrown when a presented password violates a rule
        /// </summary>
        public class PasswordRuleException : Exception
        {
            public PasswordRuleException(string message)
                : base(message)
            { }
        }

        /// <summary>
        /// Exception produced when a specific action cannot be taken because a user 
        /// has been revoked
        /// </summary>
        public class UserRevokedException : Exception
        {
            public UserRevokedException(string message)
                : base(message)
            { }
        }

        /// <summary>
        /// Exception produced when a user cannot be authenticated because they are locked
        /// out (due to too many successive failed login attempts within 30 minutes
        /// </summary>
        public class UserLockedOutException : Exception
        {
            public DateTime lastAttempt = DateTime.Now;
            public UserLockedOutException(string message, DateTime lastAttempt)
                : base(message)
            {
                this.lastAttempt = lastAttempt;
            }

            public UserLockedOutException(string message)
                : base(message)
            { } //just use the current date time then

            public override string ToString()
            {
                /*return "Sent: " + DataSent 
                    + "\r\nReceived: " + String.Join("\r\n", ResponsesReceived.ToArray()) 
                    + "\r\n" + base.ToString();*/
                StringBuilder description = new StringBuilder();
                description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);
                description.AppendFormat("\r\nLast failed authentication attempt: {0}\r\n", lastAttempt.ToString());

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
        /// Use when exceptions are accumulated and not allowed to interrupt the flow
        /// </summary>
        public class MultiException : Exception
        {
            public List<Exception> Exceptions = new List<Exception>();

            public MultiException(Exception e)
                : base()
            {
                Exceptions.Add(e);
            }

            public void AddException(Exception e)
            {
                Exceptions.Add(e);
            }

            public override string ToString()
            {
                string s = "";
                foreach (Exception e in Exceptions)
                {
                    s = s + "EXCEPTION : " + e.ToString();
                }
                return s;
            }

        }


        public class HexLineException : Exception
        {
            public string HexLine;

            public HexLineException(string message, string hexLine)
                : base(message)
            {
                HexLine = hexLine;
            }

            public HexLineException(string message, string hexLine, Exception innerException)
                : base(message, innerException)
            {
                HexLine = hexLine;
            }


            public override string ToString()
            {
                StringBuilder description = new StringBuilder();
                description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);
                description.AppendFormat("\r\nHex Line: {0}", HexLine);

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

    }
}
