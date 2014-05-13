using CERTENROLLLib;
using ForwardLibrary.Crypto;
using ForwardLibrary.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace ForwardLibrary
{
    namespace WinSIPserver
    {

        #region Exceptions
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

            public MultiException(Exception e) : base()
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



        #endregion

        public class BNAC_Table
        {
            public enum ID_Type : int { Index, UID, MAC, SIM };

            /// <summary>
            /// Function to determine the ID type and extract the string field
            /// </summary>
            /// <param name="ID"></param>
            /// <param name="newID"></param>
            /// <param name="idType"></param>
            /// <returns></returns>
            public static bool GetIDtype(string ID, out string newID, out ID_Type idType)
            {
                bool bFoundType = true;
                newID = null;
                idType = ID_Type.Index;
                long dummy;

                //UID
                if (ID.StartsWith("UID") == true)
                {
                    newID = ID.Substring(3);
                    idType = ID_Type.UID;
                }
                //Table Index: OBSOLETE BUT SUPPORTED FOR BACKWARDS COMPATIBILITY:
                else if (ID.StartsWith("Index") == true)
                {
                    newID = ID.Substring(5);
                    idType = ID_Type.Index;
                }
                //MAC
                else if (ID.Length == 12)
                {
                    newID = ID;
                    idType = ID_Type.MAC;
                }
                //SIM
                else if (ID.Length == 20)
                {
                    newID = ID;
                    idType = ID_Type.SIM;
                }
                //Table index
                else if (ID.Length != 20 && long.TryParse(ID, out dummy))
                {
                    newID = ID;
                    idType = ID_Type.Index;
                }
                else
                    bFoundType = false;

                return bFoundType;

            }

            /// <summary>
            /// Converts origID into an acceptable ID string. 
            /// Will throw an ArgumentException if origID does not contain acceptable digits
            /// based on the idType field
            /// </summary>
            /// <param name="origID"></param>
            /// <param name="idType"></param>
            /// <returns>A serialized ID that can be used in STX/ETX commands</returns>
            public static string CreateID(string origID, ID_Type idType)
            {
                string retVal = null;
                origID = origID.Trim();
                
                switch (idType)
                {
                    case ID_Type.Index:
                        if (origID.Length > 13)
                            throw new ArgumentException("Length for an index-type ID cannot exceed 13 digits", "origID");
                        if (!System.Text.RegularExpressions.Regex.IsMatch(origID, @"\A\b[0-9]+\b\Z"))
                            throw new ArgumentException("An index-type ID may contain only numeric digits (0-9)", "origID");
                        retVal = origID;
                        break;

                    case ID_Type.MAC:
                        if (origID.Length != 12)
                            throw new ArgumentException("Length for a MAC-type ID must be 12 digits", "origID");
                        if (!System.Text.RegularExpressions.Regex.IsMatch(origID, @"\A\b[0-9a-fA-F]+\b\Z"))
                            throw new ArgumentException("A MAC-type ID may contain only hexidecimal digits (0-9, a-z, A-Z)", "origID");
                        retVal = origID;
                        break;

                    case ID_Type.SIM:
                        if (origID.Length != 20)
                            throw new ArgumentException("Length for an SIM-type ID must be 20 digits", "origID");
                        if (!System.Text.RegularExpressions.Regex.IsMatch(origID, @"\A\b[0-9]+\b\Z"))
                            throw new ArgumentException("A SIM-type ID may contain only numeric digits(0-9)", "origID");
                        retVal = origID;
                        break;

                    case ID_Type.UID:
                        if (!System.Text.RegularExpressions.Regex.IsMatch(origID, @"\A\b[0-9a-zA-Z]+\b\Z"))
                            throw new ArgumentException("A UID-type ID may only contain alpha-numeric characters (0-9, a-z, A-Z)", "origID");
                        if (origID.Length > 13)
                            throw new ArgumentException("Length for a UID-type ID cannot exceed 13 digits", "origID");
                        retVal = "UID" + origID;
                        break;
                }
                return retVal;
            }

            public class Entry
            {
                public virtual long index {get; set;} //= 0;
                public virtual string SIM {get; set;} //= null;
                public virtual string MAC {get; set;} //= null;
                public virtual bool bCell { get; set; } //= false;
                public virtual int SMS_limit { get; set; } //= 0;
                public virtual string phoneNumber { get; set; } //= null;
                public virtual string CustomerID { get; set; } //= null;
                public virtual string GroupID { get; set; } //= null;
                public virtual string UnitID { get; set; } //= null;
                public virtual string extra { get; set; } //= null;

                //create the entry for this particular index
                public Entry(long index)
                {
                    this.index = index;
                }

                //create the entry from stream data
                public Entry(string[] fields)
                {
                    if (fields.Length < 10)
                        throw new ArgumentException("BNAC table entries require at least 10 fields.", "fields");

                    int i = 0;
                    foreach (string param in fields)
                    {
                        string val = param.Trim();
                        switch (i)
                        {
                            case 0:
                                CreateID(val, ID_Type.Index);   //causes an argument exception if the field is invalid
                                index = Convert.ToInt64(val);
                                break;

                            case 1:
                                if (val.Length > 1)
                                    CreateID(val, ID_Type.SIM);   //causes an argument exception if the field is invalid
                                SIM = val;
                                break;

                            case 2:
                                if (val.Length > 1)
                                    CreateID(val, ID_Type.MAC);   //causes an argument exception if the field is invalid
                                MAC = val;
                                break;

                            case 3:
                                if (Convert.ToInt32(val) == 0)
                                    bCell = false;
                                else if (Convert.ToInt32(val) == 1)
                                    bCell = true;
                                else
                                    throw new ArgumentException("The bCell field should be either 1 or 0", "fields[" + i.ToString() + "]");
                                break;

                            case 4:
                                SMS_limit = Convert.ToInt32(val); ;
                                break;

                            case 5:
                                phoneNumber = val;
                                break;

                            case 6:
                                CustomerID = val;
                                break;

                            case 7:
                                GroupID = val;
                                break;

                            case 8:
                                UnitID = val;
                                break;

                            case 9:
                                extra = val;
                                break;

                            default:
                                extra = extra + "," + val;
                                break;
                        }
                        
                        i += 1;
                    }
                }

                //constructors to be implemented child classes:
                public Entry(bool download, long index, bool syncUpload = false)
                {
                    throw new NotImplementedException();
                }
                public Entry(string ID, BNAC_Table.ID_Type idType, bool syncUpload = false)
                {
                    throw new NotImplementedException();
                }

                public override string ToString()
                {
                    string CellString;
                    if (bCell == true)
                        CellString = "1";
                    else
                        CellString = "0";

                    string response = index.ToString() + ","
                            + SIM + ","
                            + MAC + ","
                            + CellString + ","
                            + SMS_limit.ToString() + ","
                            + phoneNumber + ","
                            + CustomerID + ","
                            + GroupID + ","
                            + UnitID + ","
                            + extra;
                    return response;
                }

                /// <summary>
                /// Determines whether the given ID is for this BNAC table entry.
                /// </summary>
                /// <param name="ID"></param>
                /// <returns></returns>
                public virtual bool EqualID(string ID)
                {
                    string newID;
                    BNAC_Table.ID_Type idType;
                    if (GetIDtype(ID, out newID, out idType) == false)
                        return false;
                    
                    bool retVal = false; 
                    switch (idType)
                    {
                        case ID_Type.Index:                                                
                            long idIndex;
                            if (long.TryParse(newID, out idIndex))
                                retVal = (idIndex == index);
                            else
                                retVal = false;
                            break;

                        case ID_Type.MAC:
                            retVal = (MAC == newID);
                            break;

                        case ID_Type.SIM:
                            retVal = (SIM == newID);
                            break;

                        case ID_Type.UID:
                            retVal = (UnitID == newID);
                            break;

                        default:    //should never get here
                            retVal = false;
                            break;
                    }
                    return retVal;
                }
            }
        }

        public class BNAC_StateTable
        {
            public enum BNAC_Status :int
            { IDLE = 0, RDNS_REQUEST = 1 };

            public class Entry
            {

                protected long _index = 0;
                public virtual long index { get {return _index;} set {_index = value;} }

                protected BNAC_Status _PendingRequest = BNAC_Status.IDLE;
                public virtual BNAC_Status PendingRequest { get {return _PendingRequest;} set{ _PendingRequest = value;} }                     

                protected string _LastCheckin = "1231002359000"; //mmddyyhhmmddss
                public virtual string LastCheckin {get {return _LastCheckin; } set {_LastCheckin = value; } } 

                //public string RequestKey = null;
                //public ClientContext contextOn = null;        //the context used to communicate to this BNAC
                //public ClientContext contextTo = null;        //who this BNAC is talking to (ie a context for WINSIP)

                public Entry(long i)
                {
                    index = i;
                }

                public override string ToString()
                {
                    return index.ToString() + "," + PendingRequest.ToString() + "," + LastCheckin;
                }
            }
        }

        public class BNAC_UserTable
        {


            public class Entry
            {

                public const int SALT_SIZE = 32;

                #region Fields                
                private string __userName;
                protected string _userName
                {
                    get { return __userName; }
                    set
                    {
                        Regex rg = new Regex(@"^[a-zA-Z0-9]*$"); //allow only alphanumeric characters
                        if (value.Length > 20)
                            throw new ArgumentException("User Name exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the User Name");
                        __userName = value;
                        //PropertyChanged("userName", _userName);
                    }
                }
                public virtual string userName 
                { 
                    get { return _userName; } 
                }

                protected string _firstName = "";
                public virtual string firstName
                {
                    get { return _firstName; }
                    set {
                        Regex rg = new Regex(@"^[a-zA-Z]*$"); //allow only alpha characters
                        if (value.Length > 20)
                            throw new ArgumentException("First Name exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the First Name");

                        _firstName = value;
                        PropertyChanged("firstName", _firstName);
                    }
                }

                protected string _lastName = "";
                public virtual string lastName
                {
                    get { return _lastName; }
                    set {
                        Regex rg = new Regex(@"^[a-zA-Z]*$"); //allow only alpha characters
                        if (value.Length > 20)
                            throw new ArgumentException("Last Name exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the Last Name");

                        _lastName = value;
                        PropertyChanged("lastName", _lastName);
                    }
                }

                protected string _companyCode = "";
                public virtual string companyCode
                {
                    get { return _companyCode; }
                    set {
                        Regex rg = new Regex(@"^[a-zA-Z0-9]*$"); //allow only alpha characters
                        if (value.Length > 20)
                            throw new ArgumentException("Company Code Exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the Company Code");

                        _companyCode = value;
                        PropertyChanged("companyCode", _companyCode);
                    }
                }

                protected string _groupIDs = ""; 
                /// <summary>
                /// list of ; seperated group names
                /// </summary>
                public virtual string groupIDs
                {
                    get { return _groupIDs; }
                    set {
                        Regex rg = new Regex(@"^[a-zA-Z0-9;]*$"); //allow only alpha characters
                        if (value.Length > 300)
                            throw new ArgumentException("Group IDs exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the Group IDs");

                        _groupIDs = value;
                        PropertyChanged("groupIDs", _groupIDs);
                    }
                }

                protected string _ruleGroupIDs = "";
                /// <summary>
                /// list of ; seperated group names
                /// </summary>
                public virtual string ruleGroupIDs
                {
                    get { return _ruleGroupIDs; }
                    set
                    {
                        Regex rg = new Regex(@"^[a-zA-Z0-9;\\/-]*$"); //allow only alphanumeric, '\', '/', and '-' characters
                        if (value.Length > 300)
                            throw new ArgumentException("Group IDs exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the Group IDs");

                        _ruleGroupIDs = value;
                        PropertyChanged("ruleGroupIDs", _ruleGroupIDs);
                    }
                }

                /*protected string _privilegeCode = "";
                public virtual string privilegeCode
                {
                    get { return _privilegeCode; }
                    set {
                        Regex rg = new Regex(@"^[1-3]*$"); //allow only alpha characters
                        if (value.Length > 1)
                            throw new ArgumentException("Privilege Code exceeded maximum field length");
                        if (!rg.IsMatch(value))
                            throw new ArgumentException("Invalid characters found in the Privilege Code");

                        _privilegeCode = value;
                        PropertyChanged("privilegeCode", _privilegeCode);
                    }
                }*/

                private string __passwordOneTimeSalt = "";
                protected string _passwordOneTimeSalt
                {
                    get { return __passwordOneTimeSalt; }
                    set
                    {
                        __passwordOneTimeSalt = value;
                        PropertyChanged("passwordOneTimeSalt", __passwordOneTimeSalt);
                    }
                }
                public virtual string passwordOneTimeSalt
                {
                    get { return _passwordOneTimeSalt; }
                }
                protected string[] _pwSalts = new string[5] { "", "", "", "", "" };
                public virtual string passwordSalt
                {
                    get { return _pwSalts[0]; }
                }

                public virtual string passwordSaltPrev1
                {
                    get { return _pwSalts[1]; }
                }

                public virtual string passwordSaltPrev2
                {
                    get { return _pwSalts[2]; }
                    set { }
                }

                public virtual string passwordSaltPrev3
                {
                    get { return _pwSalts[3]; }
                    set { }
                }

                public virtual string passwordSaltPrev4
                {
                    get { return _pwSalts[4]; }
                    set { }
                }



                private string __passwordOneTimeHash = "";
                protected string _passwordOneTimeHash
                {
                    get { return __passwordOneTimeHash; }
                    set
                    {
                        __passwordOneTimeHash = value;
                        PropertyChanged("passwordOneTimeHash", __passwordOneTimeHash);
                    }
                }
                public virtual string passwordOneTimeHash
                {
                    get { return _passwordOneTimeHash; }                    
                }

                protected string[] _pwHashes = new string[5] {"", "", "", "", ""};             
                public virtual string passwordHash
                {
                    get { return _pwHashes[0]; }                    
                }
                
                public virtual string passwordHashPrev1
                {
                    get { return _pwHashes[1]; }
                }
                
                public virtual string passwordHashPrev2
                {
                    get { return _pwHashes[2]; }
                    set { }
                }
                
                public virtual string passwordHashPrev3
                {
                    get { return _pwHashes[3]; }
                    set { }
                }
                
                public virtual string passwordHashPrev4
                {
                    get { return _pwHashes[4]; }
                    set { }
                }

                private DateTime __passwordChangeDate;
                protected DateTime _passwordChangeDate
                {
                    get { return __passwordChangeDate; }
                    set
                    {
                        __passwordChangeDate = value;
                        PropertyChanged("passwordChangeDate", __passwordChangeDate);
                    }
                }
                public virtual DateTime passwordChangeDate
                {
                    get { return _passwordChangeDate; }                    
                }


                protected DateTime _userRevokedDate = new DateTime(0);
                public virtual DateTime userRevokedDate
                {
                    get { return _userRevokedDate; }                    
                    set {
                        _userRevokedDate = value;
                        PropertyChanged("userRevokedDate", _userRevokedDate);
                    }                    
                }

                private DateTime __failedLoginDate = new DateTime(0);
                protected DateTime _failedLoginDate
                {
                    get { return __failedLoginDate; }
                    set
                    {
                        __failedLoginDate = value;
                        PropertyChanged("failedLoginDate", __failedLoginDate);
                    }                    

                }
                public virtual DateTime failedLoginDate
                {
                    get { return _failedLoginDate; }                    
                }

                private int __failedLoginCount = 0;
                protected int _failedLoginCount
                {
                    get { return __failedLoginCount; }
                    set
                    {
                        __failedLoginCount = value;
                        PropertyChanged("failedLoginCount", __failedLoginCount);
                    }
                }
                public virtual int failedLoginCount
                {
                    get { return _failedLoginCount; }                    
                }

                private DateTime __successfulLoginDate = new DateTime(0);
                protected DateTime _successfulLoginDate
                {
                    get { return __successfulLoginDate; }
                    set
                    {
                        __successfulLoginDate = value;
                        PropertyChanged("successfulLoginDate", __successfulLoginDate);
                    }
                }
                public virtual DateTime successfulLoginDate
                {
                    get { return _successfulLoginDate; }                    
                }

                #endregion

                public virtual bool IsUserLockedOut
                {
                    get
                    {
                        bool bRet = false;
                        TimeSpan ts = DateTime.Now - failedLoginDate;
                        if ( (ts.TotalMinutes > 30) && (failedLoginCount > 0) )
                            _failedLoginCount = 0;

                        if (failedLoginCount > 6)                                                  
                            bRet = true;
                        
                        return bRet;
                    }
                }

                public Entry(string userName)
                {
                    this._userName = userName;
                }

                public Entry(string[] fields)
                {   //userName, firstName, lastName, companyCode, groupIDs, ruleGroupIDs
                    _userName = fields[0];
                    UpdateFields(fields);
                    /*firstName = fields[1];
                    lastName = fields[2];
                    companyCode = fields[3];
                    groupIDs = fields[4];
                    ruleGroupIDs = fields[5]; */
                    
                }

                public void UpdateFields(string[] fields)
                {
                    if (_userName != fields[0])
                        throw new ArgumentException("Username mismatch");

                    firstName = fields[1];
                    lastName = fields[2];
                    companyCode = fields[3];
                    groupIDs = fields[4];
                    ruleGroupIDs = fields[5]; 
                }
                public override string ToString()
                {
                    return userName + "," + firstName + "," + lastName + "," + companyCode + "," + groupIDs + "," + ruleGroupIDs;
                }


                /// <summary>
                /// Change the password by first verifying an old password
                /// Exceptions are thrown if there isn't a valid password
                /// or if the new password breaks valid password rules
                /// 
                /// If the password does not match the rules a PasswordRuleException
                /// is thrown
                /// </summary>
                /// <param name="oldPassword"></param>
                /// <param name="newPassword"></param>
                /// <returns>True if the password has been updated.</returns>
                public virtual bool ChangePassword(string oldPassword, string newPassword)
                {
                    bool doChange = true;
                    //if there is a onetime password, only compare against that

                    //calculate the password hash
                    string _salt = passwordSalt;
                    if (_passwordOneTimeSalt != "")
                        _salt = _passwordOneTimeSalt;

                    string _passwordTempHash = GetPasswordHash(oldPassword, _salt);                    

                    if (_passwordOneTimeHash != "")
                        doChange = (_passwordOneTimeHash == _passwordTempHash);
                    else
                        doChange = (_pwHashes[0] == _passwordTempHash);

                    if (doChange)
                        SetPassword(newPassword);

                    return doChange;
                }

                /// <summary>
                /// Returns true if the user is authenticated, false if the passwords didn't match
                /// Exceptions are thrown is the user is lockedout, revoked, or has no valid password
                /// 
                /// </summary>
                /// <param name="password"></param>
                /// <returns></returns>
                public virtual bool AuthenticateUser(string password)
                {
                    bool validUser = false;

                    if (IsUserLockedOut == true)
                        throw new UserLockedOutException("The user is currently locked out.", failedLoginDate);

                    if (userRevokedDate != new DateTime(0))
                        throw new UserRevokedException("The user has been revoked since: " + userRevokedDate.ToString());

                    if (_passwordOneTimeHash != "")
                        throw new NoValidPasswordException("A one-time password has been set. The user must change their password before being authenticated.");

                    if (_pwHashes[0].Length != 64)      //256bit hash should be 32 bytes * 2 (ascii-coded)
                        throw new NoValidPasswordException("No valid password is on record. Please contact the system administrator to perform a password reset.");

                    //calculate the password hash
                    string _passwordTempHash = GetPasswordHash(password, passwordSalt);

                    if (_passwordTempHash != _pwHashes[0])
                    {
                        //password mismatch
                        validUser = false;
                        _failedLoginCount++;
                        _failedLoginDate = DateTime.Now;
                    }
                    else
                    {
                        //password matches
                        validUser = true;
                        _failedLoginCount = 0;
                        _successfulLoginDate = DateTime.Now;
                    }

                    return validUser;                    
                }

                /// <summary>
                /// Call this to revoke a user's rights
                /// </summary>
                public virtual void RevokeUser()
                {
                    userRevokedDate = DateTime.Now;
                }

                /// <summary>
                /// Call to set the one-time use password.
                /// This password can only be used to change to a new password,
                /// it will not allow a user to authenticate.
                /// 
                /// If the password does not match the rules a PasswordRuleException
                /// is thrown
                /// </summary>
                /// <param name="password"></param>
                public virtual void SetOnetimePassword(string password)
                {
                    CheckPasswordRules(password);                    

                    if (!CheckAgainstOldPasswords(password))
                        throw new PasswordRuleException("Password must be different from the last four passwords.");

                    //save the new hash (do this first in case an exception occurs)
                    string _passwordTempSalt = CreateSalt();
                    string _passwordTempHash = GetPasswordHash(password, _passwordTempSalt);

                    //update the change time
                    _passwordChangeDate = DateTime.Now;

                    //update the old hashes
                    PushHashAndSalt();

                    _passwordOneTimeSalt = _passwordTempSalt;
                    _passwordOneTimeHash = _passwordTempHash;
                    PasswordHashesChanged();
                }

                /// <summary>
                /// Set the password. Call ChangePassword() to force the
                /// user to verify an old password first.
                /// </summary>
                /// <param name="password"></param>
                public virtual void SetPassword(string password)
                {
                    CheckPasswordRules(password);
                    
                    if (!CheckAgainstOldPasswords(password))
                        throw new PasswordRuleException("Password must be different from the last four passwords.");

                    //save the new hash (do this first in case an exception occurs)
                    string _passwordTempSalt = CreateSalt();
                    string _passwordTempHash = GetPasswordHash(password, _passwordTempSalt);

                    //update the change time
                    _passwordChangeDate = DateTime.Now;

                    //update the old hashes and salts
                    PushHashAndSalt();                    

                    _pwSalts[0] = _passwordTempSalt;
                    _pwHashes[0] = _passwordTempHash;
                    PasswordHashesChanged();
                }


                #region Private helper functions

                /// <summary>
                /// this function pushes the hash and salt arrays back one (frees up the entry at index 0)
                /// </summary>
                protected virtual void PushHashAndSalt()
                {
                    _pwHashes[4] = _pwHashes[3];
                    _pwHashes[3] = _pwHashes[2];
                    _pwHashes[2] = _pwHashes[1];
                    _pwSalts[4] = _pwSalts[3];
                    _pwSalts[3] = _pwSalts[2];
                    _pwSalts[2] = _pwSalts[1];

                    if (_passwordOneTimeHash != "")
                    {
                        _pwHashes[1] = _passwordOneTimeHash;
                        _pwSalts[1] = _passwordOneTimeSalt;
                        _passwordOneTimeHash = "";
                        _passwordOneTimeSalt = "";
                    }
                    else
                    {
                        _pwHashes[1] = _pwHashes[0];
                        _pwSalts[1] = _pwSalts[0];
                    }                    
                }
                private string CreateSalt()
                {
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                    byte[] buff = new byte[SALT_SIZE];
                    rng.GetBytes(buff);
                    return Convert.ToBase64String(buff);
                }

                private bool CheckAgainstOldPasswords(string password)
                {
                    bool unique = true;
                    foreach (string _pwh in _pwHashes)
                    {
                        foreach (string _salt in _pwSalts)
                        {
                            string pwHash = GetPasswordHash(password, _salt);
                            if (pwHash == _pwh)
                            {
                                unique = false;
                                break;
                            }
                        }
                    }
                    if (_passwordOneTimeHash != "")
                    {
                        string pwHash = GetPasswordHash(_passwordOneTimeHash, _passwordOneTimeSalt);
                        if (_passwordOneTimeHash == pwHash)
                            unique = false;
                    }
                    return unique;
                }


                protected virtual bool CheckPasswordRules(string password)
                {
                    Regex rg = new Regex(@"^.*(?=.{7,})(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).*$");  //http://stackoverflow.com/questions/2044136/strong-password-regular-expression-that-matches-any-special-char
                    //password must contain at least one lower case letter, one upper case letter, one special character, one number                    
                    if (password.Length < 7)
                        throw new PasswordRuleException("The password is too short.");
                    if (!rg.IsMatch(password))
                        throw new PasswordRuleException("The password is missing required characters.");
                    //password cannot be the same as the username
                    if (password == userName)
                        throw new PasswordRuleException("The password cannot be the same as the username.");

                    return true;
                }

                /// <summary>
                /// return in bytes the password pre-pended by the salt
                /// </summary>
                /// <param name="password">plain text string</param>
                /// <param name="salt">base 64 encoded string</param>
                /// <returns></returns>
                protected virtual Byte[] ConcatStringSalt(string password, string salt)
                {
                    byte[] plainTextWithSaltBytes = new byte[password.Length + salt.Length];
                    byte[] saltBytes = Convert.FromBase64String(salt);
                    byte[] plainText = Encoding.ASCII.GetBytes(password);
                    int i;

                    for (i = 0; i < saltBytes.Length; i++)
                        plainTextWithSaltBytes[i] = saltBytes[i];
                    for (int n = 0; n < plainText.Length; n++)
                        plainTextWithSaltBytes[i++] = plainText[n];

                    return plainTextWithSaltBytes;
                }

                protected virtual string GetPasswordHash(string password, string salt)
                {
                    //calculate the new hash
                    SHA256 thesha = SHA256Managed.Create();
                    byte[] hashValue = thesha.ComputeHash(ConcatStringSalt(password, salt));
                    StringBuilder hex = new StringBuilder(hashValue.Length * 2);
                    foreach (byte h in hashValue)
                        hex.AppendFormat("{0:x2}", h);

                    return hex.ToString();
                }

                protected virtual void PasswordHashesChanged()
                {
                    Dictionary<string, object> Properties = new Dictionary<string, object>();
                    Properties.Add("passwordHash", passwordHash);
                    Properties.Add("passwordHashPrev1", passwordHashPrev1);
                    Properties.Add("passwordHashPrev2", passwordHashPrev2);
                    Properties.Add("passwordHashPrev3", passwordHashPrev3);
                    Properties.Add("passwordHashPrev4", passwordHashPrev4);

                    Properties.Add("passwordSalt", passwordSalt);
                    Properties.Add("passwordSaltPrev1", passwordSaltPrev1);
                    Properties.Add("passwordSaltPrev2", passwordSaltPrev2);
                    Properties.Add("passwordSaltPrev3", passwordSaltPrev3);
                    Properties.Add("passwordSaltPrev4", passwordSaltPrev4);
                    PropertiesChanged(Properties);
                }

                protected virtual void PropertiesChanged(Dictionary<string, object> Properties)
                {

                }
                protected virtual void PropertyChanged(string name, object newVal)
                {

                }


                #endregion


            }
        }

        public class BNAC_RuleGroupTable
        {

            /// <summary>
            /// SET This so that rules that need BNAC table entries can download them
            /// </summary>
            /// <param name="ID"></param>
            /// <param name="idType"></param>
            /// <returns></returns>
            public delegate BNAC_Table.Entry GetBNAC_TableEntryDel(string ID, BNAC_Table.ID_Type idType);
            static public GetBNAC_TableEntryDel GetBNAC_TableEntry = delegate(string ID, BNAC_Table.ID_Type idType)
            {
                throw new NotImplementedException();
                //return new BNAC_Table.Entry(0);
            };
            public delegate BNAC_UserTable.Entry GetBNAC_UserTableEntryDel(string userName);
            static public GetBNAC_UserTableEntryDel GetBNAC_UserTableEntry = delegate(string userName)
            {
                throw new NotImplementedException();
                //return new BNAC_Table.Entry(0);
            };



            /// <summary>
            /// Call this function to create the correct rule for a given rule string
            /// </summary>
            /// <param name="rule"></param>
            /// <returns></returns>
            static public Rule CreateRule(string rule)
            {
                string[] RuleParams = rule.Split(',');

                //
                if (RuleParams[0] == "PTR") //PassthroughRule.RuleName == RuleParams[0])
                    return new PassthroughRule(rule);
                else if (RuleParams[0] == "RDR")
                    return new RuleGroupDatabaseRule(rule);
                else if (RuleParams[0] == "UDR")
                    return new UserDatabaseRule(rule);
                else if (RuleParams[0] == "CCR")
                    return new ClientCertRule(rule);
                else
                    throw new ArgumentException("No rule exists for rule code '" + RuleParams[0] + "'");
            }

            abstract public class Rule
            {
                public abstract string RuleName { get; }       //child classes must give a name
                //abstract public static const string RuleName = "";      //child classes must give a name
                protected string _rule;
                
                public Rule(string rule)
                {
                    _rule = rule;
                }

                public override string ToString()
                {
 	                 return _rule;
                }

                abstract public bool PermissionForCMD(string cmd, BNAC_UserTable.Entry user);                

            }

            public class RuleGroupDatabaseRule : Rule
            {
                public override string RuleName { get { return "RDR"; } }       //child classes must give a name
                private List<string> Permissions, RuleGroupIDs;

                /// <summary>
                /// Constructor string format: "RDR,[Permissions], [Rule Group IDs]"
                /// All fields are ";" seperated lists
                /// Any field can have a * value which signifies that any value is permissible
                /// Permission values: 'R' to allow read RDE command, 'W' to allow write RDE command, 
                /// 'D' to allow deleting, '*' for all permissions                
                /// </summary>
                /// <param name="rule"></param>
                public RuleGroupDatabaseRule(string rule)
                    : base(rule)
                {
                    string[] RuleParams = rule.Split(',');
                    if (RuleParams[0].Trim() != RuleName)
                        throw new ArgumentException("Wrong rule type -- rule expects '" + RuleName + "' rule");
                    if (RuleParams.Length != 3)
                        throw new ArgumentException("Wrong number of fields.");
                    Permissions = new List<string>(RuleParams[1].Split(';'));
                    RuleGroupIDs = new List<string>(RuleParams[2].Split(';'));                    
                    _rule = RuleName + "," + String.Join(";", Permissions) + "," + String.Join(";", RuleGroupIDs) ;
                }

                public override bool PermissionForCMD(string incmd, BNAC_UserTable.Entry user)
                {
                    //switch based on user database command
                    if (incmd.StartsWith("RDD=") == true)
                        return RDD_Permission(incmd, user);

                    else if (incmd.StartsWith("RDE=") == true)
                        return RDE_Permission(incmd, user);                    

                    else
                        return false;       //not for a command that this rule governs.
                }

                #region private helper functions
                private bool RDD_Permission(string incmd, BNAC_UserTable.Entry user)
                {
                    String ruleGroupID = incmd.Substring(4);

                    if (!Permissions.Contains("D") && !Permissions.Contains("*"))
                        return false;
                    if (RuleGroupIDs.Contains("*") || RuleGroupIDs.Contains(ruleGroupID))
                        return true;

                    return false;
                }

                private bool RDE_Permission(string incmd, BNAC_UserTable.Entry user)
                {
                    string fields = incmd.Substring(4);

                    //if we are reading:
                    if (fields.EndsWith("?"))
                    {                        
                        if ( (Permissions.Contains("R")  || Permissions.Contains("*")) && 
                            (RuleGroupIDs.Contains("*") || RuleGroupIDs.Contains(fields.TrimEnd('?'))) )
                            return true;
                    }
                    //if we are writing:
                    else
                    {
                        if ((Permissions.Contains("W") || Permissions.Contains("*")) &&
                            (RuleGroupIDs.Contains("*") || RuleGroupIDs.Contains(fields.Split(',')[0])))
                            return true;
                    }

                    return false;
                }

                #endregion

            }
            public class UserDatabaseRule : Rule
            {
                public override string RuleName { get { return "UDR"; } }       //child classes must give a name

                private List<string> Permissions, CompanyCodes, GroupIDs, Usernames;

                /// <summary>
                /// Constructor string format: "UDR,[Permissions], [Customer IDs], [Group IDs], [Usernames]"
                /// All fields are ";" seperated lists
                /// Any field can have a * value which signifies that any value is permissible
                /// Permission values: 'P' to allow set one-time use password, 'R' to allow read UDE command, 
                /// 'W' to allow write UDE command, 'D' to allow deleting, 'S' to allow revoke, '*' for all permissions
                /// All users are assumed to have permission to change their own password.
                /// </summary>
                /// <param name="rule"></param>
                public UserDatabaseRule(string rule)
                    : base(rule)
                {
                    string[] RuleParams = rule.Split(',');
                    if (RuleParams[0].Trim() != RuleName)
                        throw new ArgumentException("Wrong rule type -- rule expects '" + RuleName + "' rule");
                    if (RuleParams.Length != 5)
                        throw new ArgumentException("Wrong number of fields.");
                    Permissions = new List<string>(RuleParams[1].Split(';'));
                    CompanyCodes = new List<string>(RuleParams[2].Split(';'));
                    GroupIDs = new List<string>(RuleParams[3].Split(';'));
                    Usernames = new List<string>(RuleParams[4].Split(';'));
                    _rule = RuleName + "," + String.Join(";", Permissions) + "," + String.Join(";", CompanyCodes) + "," + String.Join(";", GroupIDs) + "," + String.Join(";", Usernames);
                }

                public override bool PermissionForCMD(string incmd, BNAC_UserTable.Entry user)
                {
                    //switch based on user database command
                    if (incmd.StartsWith("UDD=") == true)
                        return UDD_Permission(incmd, user);

                    else if (incmd.StartsWith("UDE=") == true)
                        return UDE_Permission(incmd, user);

                    else if (incmd.StartsWith("UDO=") == true)
                        return UDO_Permission(incmd, user);

                    else if (incmd.StartsWith("UDP=") == true)
                        return UDP_Permission(incmd, user);

                    else if (incmd.StartsWith("UDR=") == true)
                        return UDR_Permission(incmd, user);

                    else
                        return false;       //not for a command that this rule governs.
                }

                #region private helper functions
                private bool UDD_Permission(string incmd, BNAC_UserTable.Entry user)
                {                    
                    String userName = incmd.Substring(4);                                       
                    

                    if (!Permissions.Contains("D") && !Permissions.Contains("*"))
                        return false;

                    return MatchUsernameToCriteria(userName);
                }

                private bool UDE_Permission(string incmd, BNAC_UserTable.Entry user)
                {                    
                    String fields = incmd.Substring(4);                                       
                    
                    //is the user trying to read or write?
                    if (fields.EndsWith("?")
                        && (Permissions.Contains("R") || Permissions.Contains("*")))
                    {   //reading
                        return MatchUsernameToCriteria(fields.TrimEnd('?'));
                    }
                    else if (Permissions.Contains("W") || Permissions.Contains("*"))
                    {   //writing -- this is a bit more complicated...   
                        string userName = fields.Split(',')[0];
                        bool bMatch =false;
                        //first see if the entry already exists:       
                        try
                        {
                            bMatch = MatchUsernameToCriteria(userName);
                            if (!bMatch)     //must check this so that a user can't modify a user he doesn't have permission to modify
                                return false;
                            else
                                return MatchUsernameToCriteria(new BNAC_UserTable.Entry(fields.Split(',')));
                        }
                        catch (EntryNotFoundException)
                        {
                            //okay, creating an entry
                            return MatchUsernameToCriteria(new BNAC_UserTable.Entry(fields.Split(',')));                            
                        }                        
                    }
                    else
                        return false;                    
                }

                private bool UDO_Permission(string incmd, BNAC_UserTable.Entry user)
                {
                    String userName = incmd.Substring(4).Split(',')[0];

                    if (!Permissions.Contains("P") && !Permissions.Contains("*"))
                        return false;

                    return MatchUsernameToCriteria(userName);
                }

                private bool UDP_Permission(string incmd, BNAC_UserTable.Entry user)
                {                    
                    return true;
                }

                private bool UDR_Permission(string incmd, BNAC_UserTable.Entry user)
                {
                    String userName = incmd.Substring(4);

                    if (!Permissions.Contains("R") && !Permissions.Contains("*"))
                        return false;

                    return MatchUsernameToCriteria(userName);
                }

                /// <summary>
                /// Downloads the user entry for the specified userName for comparison
                /// </summary>
                /// <param name="userName"></param>
                /// <returns>True if the rule matches this username</returns>
                private bool MatchUsernameToCriteria(string userName)
                {
                    return MatchUsernameToCriteria(GetBNAC_UserTableEntry(userName));
                }
                /// <summary>
                /// Compares based on the user entry provided in tempEntry
                /// </summary>
                /// <param name="tempEntry"></param>
                /// <returns>True if the rule matches this username</returns>
                private bool MatchUsernameToCriteria(BNAC_UserTable.Entry tempEntry)
                {
                    if (!CompanyCodes.Contains("*") && !CompanyCodes.Contains(tempEntry.companyCode))
                        return false;

                    if (!GroupIDs.Contains("*"))
                    {
                        bool bFoundGroup = false;
                        foreach (string gID in tempEntry.groupIDs.Split(';'))
                            if (GroupIDs.Contains(gID))
                            {
                                bFoundGroup = true;
                                break;
                            }
                        if (!bFoundGroup)
                            return false;
                    }

                    if (!Usernames.Contains("*") && !Usernames.Contains(tempEntry.userName))
                        return false;

                    //weeded out all other options, must be a match
                    return true;
                }
                #endregion
            }

            
            public class PassthroughRule : Rule
            {
                public override string RuleName { get { return "PTR"; } }       //child classes must give a name
                //override public static const string RuleName = "PTR";

                private string GroupID, CustomerID, IDfield;

                /// <summary>
                /// Constructor string format: "PTR,[Customer ID], [Group ID],[IDfield]"
                /// Any field can have a * value which signifies a wildcard
                /// </summary>
                /// <param name="rule"></param>
                public PassthroughRule(string rule) : base(rule)
                {
                    string [] RuleParams = rule.Split(',');
                    if (RuleParams[0].Trim() != RuleName)
                        throw new ArgumentException("Wrong rule type -- Passthrough rule expects 'PTR' rule");
                    GroupID = RuleParams[1].Trim();
                    CustomerID = RuleParams[2].Trim();
                    IDfield = RuleParams[3].Trim();
                    _rule = "PTR," + CustomerID + "," + GroupID + "," + IDfield;
                }

                public override bool PermissionForCMD(string incmd, BNAC_UserTable.Entry user)
                {
                    //only applicable to a "CONB" command:
                    if (incmd.StartsWith("CONB") == false)
                        return false;

                    String cmd = incmd.Substring(4);
                    String[] Vals = cmd.Split(new char[] { '=' });
                    String ID = Vals[1];                    
                    string newID;
                    BNAC_Table.ID_Type idType;
                    BNAC_Table.Entry tempEntry;
                    if (GroupID == "*" && CustomerID == "*" && IDfield == "*")
                        return true;
                    else
                    {
                        //need to get the requested BNAC
                        if (BNAC_Table.GetIDtype(ID, out newID, out idType) == false)
                            return false;       //unable to parse the ID field

                        tempEntry = GetBNAC_TableEntry(newID, idType);
                        //okay, got it, now let's compare:
                        if (GroupID != "*" && GroupID != tempEntry.GroupID) 
                            return false;
                        if (CustomerID != "*" && CustomerID != tempEntry.CustomerID)
                            return false;
                        if (IDfield != "*" && !tempEntry.EqualID(IDfield)) 
                            return false;
                    }

                    //weeded out all other options, must be a match
                    return true;
                }

            }

            public class ClientCertRule : Rule
            {
                public override string RuleName { get { return "CCR"; } }       //child classes must give a name
                //override public static const string RuleName = "CCR";
                

                /// <summary>
                /// Constructor string format: "CCR"                
                /// </summary>
                /// <param name="rule"></param>
                public ClientCertRule(string rule)
                    : base(rule)
                {                    
                    if (rule.Trim() != RuleName)
                        throw new ArgumentException("Wrong rule type -- ClientCert rule expects 'CCR' rule");

                    _rule = "CCR";
                }

                public override bool PermissionForCMD(string incmd, BNAC_UserTable.Entry user)
                {
                    //only applicable to these commands: CSP, CDSR, CUCC, CCDB
                    if ((incmd.StartsWith("CSP") == false)
                            && (incmd.StartsWith("CDSR") == false)
                            && (incmd.StartsWith("CUCC") == false)
                            && (incmd.StartsWith("CCDB") == false))
                        return false;
                    else  //weeded out all other options, must be a match
                        return true;
                                                                                
                }
            }

            public class Entry
            {

                protected string _groupID;
                public string groupID
                {
                    get { return _groupID; }
                }

                protected Rule[] _rules;
                public Rule[] rules
                {
                    get { return _rules; }
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="nameAndRules">ID plus (optionally): ,Rules</param>
                public Entry(string IDandRules)
                {
                    string[] vals = IDandRules.Split(',');
                    string ID = vals[0];
                    Regex rg = new Regex(@"^[a-zA-Z0-9\\/-]*$"); //allow only alphanumeric, '\', '/', and '-' characters
                    if (ID.Length > 30)
                        throw new ArgumentException("Rule Group ID name exceeded maximum field length");
                    if (!rg.IsMatch(ID))
                        throw new ArgumentException("Invalid characters found in the Rule Group name");
                    _groupID = ID;
                    //get rules substring
                    if (vals.Length > 1)
                    {
                        string rules = IDandRules.Substring(ID.Length+1);
                        SetRules(rules);
                    }
                }

                public Entry(string ID, string rules)
                {
                    Regex rg = new Regex(@"^[a-zA-Z0-9\\/-]*$"); //allow only alphanumeric characters, '\', '/', and '-' characters
                    if (ID.Length > 30)
                        throw new ArgumentException("Rule Group ID name exceeded maximum field length");
                    if (!rg.IsMatch(ID))
                        throw new ArgumentException("Invalid characters found in the Rule Group name");
                    _groupID = ID;
                    SetRules(rules);
                }

                /// <summary>
                /// 
                /// </summary>                
                /// <param name="rules">semi-colon seperated list of rules</param>
                public virtual void SetRules(string rules)
                {
                    Regex rg = new Regex(@"^[a-zA-Z0-9,;*]*$"); //allow only alphanumeric characters plus comma
                    if (!rg.IsMatch(rules))
                        throw new ArgumentException("Invalid characters found in the Rules field");

                    List<Rule> ruleObjs = new List<Rule>();
                    string[] strRules = rules.Split(';');                    
                    foreach (string strRule in strRules)                    
                        ruleObjs.Add(CreateRule(strRule));
                   
                    _rules = ruleObjs.ToArray();
                }

                public string RulesString()
                {
                    string accumRules = "";
                    foreach (Rule rule in rules)
                        accumRules = accumRules + ";" + rule.ToString();

                    accumRules = accumRules.Substring(1);   //remove first semi-colon
                    return accumRules;
                }

                public override string ToString()
                {
                    return groupID + "=" + RulesString();
                }

                /// <summary>
                /// Test to see if the rules allow this command to be executed.
                /// If no rule granted permission and exceptions were thrown,
                /// these exceptions are rethrown inside a MultiException.
                /// Note that if a rule gave permission, any exceptions are ignored.
                /// </summary>
                /// <param name="cmd"></param>
                /// <returns>Whether or not a command can be run</returns>
                public virtual bool PermissionForCMD(string cmd, BNAC_UserTable.Entry user)
                {
                    MultiException me = null;
                    foreach (Rule rule in rules)
                    {
                        try
                        {
                            if (rule.PermissionForCMD(cmd, user) == true)
                                return true;
                        }
                        catch (Exception e)
                        {
                            if (me == null)
                                me = new MultiException(e);
                            else
                                me.AddException(e);                                
                        }
                    }

                    //okay, no rule gave permission, so report exceptions if there are any:
                    if (me != null)
                        throw me;

                    return false;
                }

            }

        }

        public class CertificateRequestTable
        {
            public class Entry
            {
                
                protected TimeSpan PinLifeTime = new TimeSpan(2, 0, 0);

                #region Properties
                protected X509Certificate2 __SignedCertificate = null;
                protected X509Certificate2 _SignedCertificate
                {
                    set
                    {
                        __SignedCertificate = value;
                        PropertyChanged("SignedCertificate", __SignedCertificate);
                    }
                    get { return __SignedCertificate; }
                }
                /// <summary>
                /// The signed certificate.
                /// </summary>            
                public X509Certificate2 SignedCertificate
                {
                    get { return _SignedCertificate; }
                }

                protected string __CertificateRequest = null;
                protected string _CertificateRequest
                {
                    set
                    {
                        __CertificateRequest = value;
                        PropertyChanged("CertificateRequest", __CertificateRequest);
                    }
                    get { return __CertificateRequest; }
                }
                /// <summary>
                /// The signed certificate.
                /// </summary>            
                public string CertificateRequest
                {
                    get { return _CertificateRequest; }
                }

                protected int _PinCode;
                /// <summary>
                /// The pin number for this entry
                /// </summary>
                public int PinCode
                {
                    get { return _PinCode; }
                }
                

                protected DateTime _PinCodeExpires;
                /// <summary>
                /// The pin number for this entry
                /// </summary>
                public DateTime PinCodeExpires
                {
                    get { return _PinCodeExpires; }
                }                

                protected string __CertificateID;
                protected string _CertificateID
                {
                    set
                    {
                        __CertificateID = value;
                        PropertyChanged("CertificateID", __CertificateID);
                    }
                    get
                    {                            
                        return __CertificateID;
                    }
                }

                /// <summary>
                /// The certificate ID
                /// </summary>
                public string CertificateID
                {
                    get
                    {    
                        //if ( (CertificateID == null) && (MachineID != null) )
                        //    GenerateCertificateID();

                        return _CertificateID;
                    }
                }

                protected string __MachineID = null;
                protected string _MachineID
                {
                    set
                    {
                        if (__MachineID == null)
                        {
                            __MachineID = value;
                            PropertyChanged("MachineID", _MachineID);
                            GenerateCertificateID();
                        }
                        else
                            throw new InvalidOperationException("MachineID already defined for this pin code.");
                    }
                    get { return __MachineID; }
                }

                /// <summary>
                /// The machine descriptive ID. This can only be set if 
                /// SignedCertificate and CertificateRequest are both null,
                /// otherwise an exception is thrown.
                /// </summary>
                public string MachineID
                {
                    set
                    {
                        if ((SignedCertificate != null) || (CertificateRequest != null))
                            throw new InvalidOperationException("Cannot set MachineID when a certificate already exists");
                        else if (DateTime.Compare(PinCodeExpires, DateTime.Now) < 0)
                            throw new InvalidOperationException("Pin code has expired");
                        else
                        {
                            _MachineID = value;                            
                        }
                    }
                    get
                    {
                        return _MachineID;
                    }
                }

                #endregion

                public Entry()
                {
                    CryptoRandom rng = new CryptoRandom();
                    _PinCode = rng.Next(0, 999999);
                    _PinCodeExpires = DateTime.Now + PinLifeTime;                
                }

                /// <summary>
                /// Load the certificate request
                /// </summary>
                /// <param name="certReq">PEM formatted certificate request ("BEGIN CERTIFICATE REQUEST...")</param>
                public virtual void CertRequest(string certReq)
                {
                    //if (PinCodeExpires > DateTime.Now)
                    if (DateTime.Compare(PinCodeExpires, DateTime.Now) < 0)
                        throw new InvalidOperationException("Pin code is no longer valid");

                    if (CertificateRequest != null)
                        throw new InvalidOperationException("Certificate request already uploaded");

                    try
                    {
                        //X509Certificate2 tempCert = CStoredCertificate.GetCertificateReqFromPEM(certReq);
                        CX509CertificateRequestPkcs10 request;

                        try
                        {
                            request = new CX509CertificateRequestPkcs10();
                            request.InitializeDecode(certReq, EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                            request.CheckSignature();
                        }
                        catch (Exception e)
                        {
                            throw new CryptographicException("Unable to create the CSR from the PEM-formatted string", e);
                        }
                        string subject = ((CX500DistinguishedName)request.Subject).Name;

                        //check to make sure it has the right CN
                        if (!subject.Contains(DesiredCN())) //tempCert.GetNameInfo(X509NameType.SimpleName, false) != DesiredCN())
                            throw new CredentialMismatchException("Invalid common name. Expected: " + DesiredCN());
                        if (request.PublicKey.Length < 2048)
                            throw new CryptographicException("Public key does not meet minumum requirements (too short)");

                        _CertificateRequest = certReq;
                    }
                    catch (Exception e)
                    {
                        _CertificateRequest = "invalidated"; //give it a dummy certificate to prevent the certifcate request from being uploaded again
                        //(you only get one try with a pin code)

                        throw e;
                    }

                    

                }

                /// <summary>
                /// Load the signed certificate
                /// </summary>
                /// <param name="cert">PEM formatted signed certificate ("BEGIN CERTIFICATE...")</param>
                public virtual void CertResponse(string cert)
                {
                    if (DateTime.Compare(PinCodeExpires, DateTime.Now) < 0)
                        throw new InvalidOperationException("Pin code is no longer valid");

                    if (SignedCertificate != null)
                        throw new InvalidOperationException("Certificate already uploaded");

                    try
                    {
                        X509Certificate2 tempCert = CStoredCertificate.GetCertificateFromPEM(cert);

                        if (CertificateRequest == null)
                            throw new InvalidOperationException("Cannot set a certificate response if a request has not been made");

                        //check to make sure it has the right CN
                        if (tempCert.GetNameInfo(X509NameType.SimpleName, false) != DesiredCN())
                            throw new CredentialMismatchException("Invalid common name. Expected: " + DesiredCN());

                        //if (tempCert.Thumbprint != CertificateRequest.Thumbprint)
                        //    throw new CredentialMismatchException("Certificate thumbprint does not match the certificate request thumbprint.");

                        _SignedCertificate = tempCert;
                    }
                    catch (Exception e)
                    {
                        _SignedCertificate = new X509Certificate2(); //give it a dummy certificate to prevent another certifcate from being uploaded
                        //(you only get one try with a pin code)

                        throw e;
                    }
                }

                

                //private helper functions

                protected string DesiredCN()
                {
                    return "PIN" + PinCode + "-MID" + MachineID + "-CID" + CertificateID;
                }

                /// <summary>
                /// Make a certificate ID (uses the MachineID and a random number)
                /// </summary>
                protected void GenerateCertificateID()
                {
                    byte[] rand_buff = new byte[32];                    
                    
                    byte[] MachineIDbytes = Encoding.ASCII.GetBytes(MachineID + PinCode.ToString());
                    byte[] full_buff = new byte[32 + MachineIDbytes.Length];
                    CryptoRandom rng = new CryptoRandom();
                    
                    while (true)
                    {
                        rng.NextBytes(rand_buff);
                        int i;
                        for (i = 0; i < rand_buff.Length; i++)
                            full_buff[i] = rand_buff[i];
                        for (int n = 0; n < MachineIDbytes.Length; n++)
                            full_buff[i++] = MachineIDbytes[n];

                        //calculate the new hash
                        SHA256 thesha = SHA256Managed.Create();
                        byte[] hashValue = thesha.ComputeHash(full_buff);

                        string temp = Base32Encoding.ToString(hashValue);
                        string tID = temp.Substring(0, 12);
                        if (IsCertificateID_Unique(tID))
                        {
                            _CertificateID = tID;
                            PropertyChanged("CertificateID", _CertificateID);
                            break;
                        }
                    }       
                }

                protected virtual void PropertiesChanged(Dictionary<string, object> Properties)
                {

                }
                protected virtual void PropertyChanged(string name, object newVal)
                {

                }

                /// <summary>
                /// Override to provide uniqueness checking of the certificate ID
                /// The likelyhood of a repeated certificate ID is extremely low
                /// since it generated with random data.
                /// </summary>
                /// <param name="certID"></param>
                /// <returns></returns>
                protected virtual bool IsCertificateID_Unique(string certID)
                {
                    return true;
                }
            }
        }
    }
    
}
