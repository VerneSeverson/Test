using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using CERTENROLLLib;
using CERTCLILib;

namespace ForwardLibrary
{
    namespace Crypto
    {
        public class SimpleCrypto
        {
            // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
            // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
            // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
            public string initVector = "Z5sr1e8z320t1dsp";
                                              
            // This constant is used to determine the keysize of the encryption algorithm.
            private int keysize = 256;
            
            public byte[] Encrypt(byte[] plainTextBytes, string passPhrase)
            {
                byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);                
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                byte[] cipherTextBytes = memoryStream.ToArray();
                memoryStream.Close();
                cryptoStream.Close();
                return cipherTextBytes;
            }

            public string Encrypt(string plainText, string passPhrase)
            {                
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(Encrypt(plainTextBytes, passPhrase));
                /*byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                byte[] cipherTextBytes = memoryStream.ToArray();
                memoryStream.Close();
                cryptoStream.Close();
                return Convert.ToBase64String(cipherTextBytes);*/                
            }

            public byte[] Decrypt(byte[] cipherTextBytes, string passPhrase)
            {
                byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                byte[] finalPlainTextBytes = new byte[decryptedByteCount];
                Array.Copy(plainTextBytes, finalPlainTextBytes, decryptedByteCount);
                memoryStream.Close();
                cryptoStream.Close();
                return finalPlainTextBytes;
            }
            public string Decrypt(string cipherText, string passPhrase)
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);                
                return Encoding.UTF8.GetString(Decrypt(cipherTextBytes, passPhrase));

                /*byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                memoryStream.Close();
                cryptoStream.Close();
                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);*/
            }
        }

        public class CCertificateRequest
        {

            public string CommonName = "test.cert.com";
            public string Country = "US";
            public string State = "Minnesota";
            public string Locality = "Eden Prairie";
            public string Organization = "Forward Pay Systems, Inc.";
            public string OrganizationalUnit = "Forward Pay";

            public StoreLocation Location = StoreLocation.CurrentUser;

            public int KeyLength = 2048;

            string providerName = "Microsoft Enhanced Cryptographic Provider v1.0";

            //public X509Certificate2 CreatedCert;

            public string Subject
            {
                get
                {
                    return "C=" + Country + "; ST=" + State + "; L=" + Locality + "; O=" + Organization + "; OU=" + OrganizationalUnit + "; CN=" + CommonName;
                }
            }


            /// <summary>
            /// Function used to create a certificate signing request using the OS.
            /// Note that this function will place a certificate in the "Certificate Enrollment Requests" folder
            /// of the certificate store specified in loc. You can view this by running either 
            /// certmgr or mmc from the command line.
            /// </summary>
            /// <param name="loc">Location to put certificate</param>
            /// <param name="subject_line">The subject line of the certificate, fields should be ; seperated, i.e.: "C=US; ST=Minnesota; L=Eden Prairie; O=Forward Pay Systems, Inc.; OU=Forward Pay; CN=fps.com"</param>
            /// <returns>The certificate signing request, if successful in PEM format</returns>
            public string GenerateRequest()
            {
                //code originally came from: http://blogs.msdn.com/b/alejacma/archive/2008/09/05/how-to-create-a-certificate-request-with-certenroll-and-net-c.aspx
                //modified version of it is here: http://stackoverflow.com/questions/16755634/issue-generating-a-csr-in-windows-vista-cx509certificaterequestpkcs10
                //here is the standard for certificates: http://www.ietf.org/rfc/rfc3280.txt


                //the PKCS#10 certificate request (http://msdn.microsoft.com/en-us/library/windows/desktop/aa377505.aspx)
                CX509CertificateRequestPkcs10 objPkcs10 = new CX509CertificateRequestPkcs10();

                //assymetric private key that can be used for encryption (http://msdn.microsoft.com/en-us/library/windows/desktop/aa378921.aspx)
                CX509PrivateKey objPrivateKey = new CX509PrivateKey();

                //access to the general information about a cryptographic provider (http://msdn.microsoft.com/en-us/library/windows/desktop/aa375967.aspx)
                CCspInformation objCSP = new CCspInformation();

                //collection on cryptographic providers available: http://msdn.microsoft.com/en-us/library/windows/desktop/aa375967(v=vs.85).aspx
                CCspInformations objCSPs = new CCspInformations();

                CX500DistinguishedName objDN = new CX500DistinguishedName();

                //top level object that enables installing a certificate response http://msdn.microsoft.com/en-us/library/windows/desktop/aa377809.aspx
                CX509Enrollment objEnroll = new CX509Enrollment();
                CObjectIds objObjectIds = new CObjectIds();
                CObjectId objObjectId = new CObjectId();
                CObjectId objObjectId2 = new CObjectId();
                CX509ExtensionKeyUsage objExtensionKeyUsage = new CX509ExtensionKeyUsage();
                CX509ExtensionEnhancedKeyUsage objX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();

                string csr_pem = null;

                //  Initialize the csp object using the desired Cryptograhic Service Provider (CSP)

                objCSPs.AddAvailableCsps();

                //Provide key container name, key length and key spec to the private key object
                objPrivateKey.ProviderName = providerName;
                objPrivateKey.Length = KeyLength;
                objPrivateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE; //Must flag as XCN_AT_KEYEXCHANGE to use this certificate for exchanging symmetric keys (needed for most SSL cipher suites)
                objPrivateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;                
                if (Location == StoreLocation.LocalMachine)
                    objPrivateKey.MachineContext = true;
                else
                    objPrivateKey.MachineContext = false; //must set this to true if installing to the local machine certificate store

                objPrivateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;    //must set this if we want to be able to export it later. (for WinSIP maybe we don't want to be able to ever export the key??)
                objPrivateKey.CspInformations = objCSPs;

                //  Create the actual key pair
                objPrivateKey.Create();

                //  Initialize the PKCS#10 certificate request object based on the private key.
                //  Using the context, indicate that this is a user certificate request and don't
                //  provide a template name
                if (Location == StoreLocation.LocalMachine)
                    objPkcs10.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, objPrivateKey, "");
                else
                    objPkcs10.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser, objPrivateKey, "");

                //Set has to sha256
                CObjectId hashobj = new CObjectId();
                hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID, ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY, AlgorithmFlags.AlgorithmFlagsNone, "SHA256");
                objPkcs10.HashAlgorithm = hashobj;

                // Key Usage Extension -- we only need digital signature and key encipherment for TLS:
                //  NOTE: in openSSL, I didn't used to request any specific extensions. Instead, I let the CA add them
                objExtensionKeyUsage.InitializeEncode(
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
                    CERTENROLLLib.X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE
                );
                objPkcs10.X509Extensions.Add((CX509Extension)objExtensionKeyUsage);

                // Enhanced Key Usage Extension
                objObjectId.InitializeFromValue("1.3.6.1.5.5.7.3.1"); // OID for Server Authentication usage (see this: http://stackoverflow.com/questions/17477279/client-authentication-1-3-6-1-5-5-7-3-2-oid-in-server-certificates)
                objObjectId2.InitializeFromValue("1.3.6.1.5.5.7.3.2"); // OID for Client Authentication usage (see this: http://stackoverflow.com/questions/17477279/client-authentication-1-3-6-1-5-5-7-3-2-oid-in-server-certificates)
                objObjectIds.Add(objObjectId);
                objObjectIds.Add(objObjectId2);
                objX509ExtensionEnhancedKeyUsage.InitializeEncode(objObjectIds);
                objPkcs10.X509Extensions.Add((CX509Extension)objX509ExtensionEnhancedKeyUsage);

                //  Encode the name in using the Distinguished Name object
                // see here: http://msdn.microsoft.com/en-us/library/windows/desktop/aa379394(v=vs.85).aspx
                /*objDN.Encode(
                    "C=US, ST=Minnesota, L=Eden Prairie, O=Forward Pay Systems; Inc., OU=Forward Pay, CN=ERIC_CN",
                    X500NameFlags.XCN_CERT_NAME_STR_NONE
                );*/
                objDN.Encode(
                    Subject,
                    X500NameFlags.XCN_CERT_NAME_STR_SEMICOLON_FLAG
                ); //"C=US; ST=Minnesota; L=Eden Prairie; O=Forward Pay Systems, Inc.; OU=Forward Pay; CN=ERIC_CN"

                //  Assing the subject name by using the Distinguished Name object initialized above
                objPkcs10.Subject = objDN;

                //suppress extra attributes:
                objPkcs10.SuppressDefaults = true;

                // Create enrollment request
                objEnroll.InitializeFromRequest(objPkcs10);
                csr_pem = objEnroll.CreateRequest(
                    EncodingType.XCN_CRYPT_STRING_BASE64
                );
                csr_pem = "-----BEGIN CERTIFICATE REQUEST-----\r\n" + csr_pem + "-----END CERTIFICATE REQUEST-----";

                return csr_pem;
            }

            /// <summary>
            /// Load the response from the CA -- just the signed certificate, not the signers.
            /// </summary>
            /// <param name="pem_response">Signed certificate</param>
            /// <param name="loc">Note that a service app can install to LocalMachine, while a regular app can only install to CurrentUser</param>
            /// <returns>The full certificate</returns>
            public static X509Certificate2 LoadResponse(string pem_response, StoreLocation loc)
            {
                X509Certificate2 cert;

                CX509Enrollment objEnroll = new CX509Enrollment();

                if (loc == StoreLocation.LocalMachine)
                    objEnroll.Initialize(X509CertificateEnrollmentContext.ContextMachine);
                else
                    objEnroll.Initialize(X509CertificateEnrollmentContext.ContextUser);

                objEnroll.InstallResponse(
                    InstallResponseRestrictionFlags.AllowUntrustedRoot,
                    pem_response,
                    EncodingType.XCN_CRYPT_STRING_BASE64HEADER,
                    null
                );

                string pfx_string = objEnroll.CreatePFX("dummypw", PFXExportOptions.PFXExportEEOnly, EncodingType.XCN_CRYPT_STRING_BASE64);
                byte[] pfx_binary_data = System.Convert.FromBase64String(pfx_string);
                cert = new X509Certificate2(pfx_binary_data, "dummypw", X509KeyStorageFlags.Exportable);

                //CreatedCert = cert;
                return cert;
            }

            /// <summary>
            /// Remove the generated certificate from the certificate store.
            /// </summary>
            public static void RemoveCertFromStore(X509Certificate2 cert, StoreLocation loc)
            {
                X509Store store = new X509Store(loc);
                store.Open(OpenFlags.ReadWrite);
                store.Remove(cert);
                store.Close();
            }

        }

        public interface IStoredCertificate
        {
            /// <summary>
            /// The certificate.
            /// </summary>
            X509Certificate2 Certificate
            {
                get;
            }

            /// <summary>
            /// The signers of the certificate
            /// </summary>
            X509Certificate2Collection Signers
            {
                get;
            }

            /// <summary>
            /// The store location for the certificate
            /// </summary>
            StoreLocation Location
            {
                get;
            }

            /// <summary>
            /// Construct a new empty certificate that will be stored to the location
            /// specified.
            /// </summary>
            /// <param name="loc">The store location</param>
            //public IStoredCertificate(StoreLocation loc);

            /// <summary>
            /// Creates a new StoredCertificate object by loading the certificate containing
            /// specified substring in its subject. If multiple certificates are found with
            /// this substring in the subject line, an expection is thrown.
            /// </summary>
            /// <param name="loc">The name of the certificate store location</param>
            /// <param name="substrSubject">The substring to search for</param>
            //public IStoredCertificate(StoreLocation loc, string substrSubject);

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="base64pfx">The certificate if a pfx file with base64 encoding</param>            
            /// <param name="password">Set to null if no password is required</param>
            void SetCert(string base64pfx, SecureString password);

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="base64pfx">The certificate if a pfx file with base64 encoding</param>            
            /// <param name="password">Set to null if no password is required</param>
            //void SetCert(string base64pfx, string password);

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="cert">The certificate</param>                        
            void SetCert(X509Certificate2 cert);

            /// <summary>
            /// Set the certificate signers. This takes a PEM file (in string form) as an 
            /// input. The first certificate in the string should be the original signer
            /// of the primary certificate and should continue in order until the final
            /// certificate which should be a self-signed root.
            /// </summary>
            /// <param name="PEM_string">The PEM formatted string of signers</param>
            /// <param name="checkChain">Set to true if the function should verify the certificate chain.</param>
            void SetCertSigners(string PEM_string, bool checkChain);

            string ExportSignersToPEM();

                  
        }

        
        public class CStoredCertificate : IStoredCertificate
        {
            #region properties
            /// <summary>
            /// The certificate.
            /// </summary>            
            public X509Certificate2 Certificate
            {
                get { return _Certificate; }
            }

            /// <summary>
            /// The signers of the certificate
            /// </summary>
            public X509Certificate2Collection Signers
            {
                get { return _Signers; }
            }

            /// <summary>
            /// The store location for the certificate
            /// </summary>
            public StoreLocation Location
            {
                get { return _Location; }
            }

            //internal variables:
            X509Certificate2 _Certificate;
            X509Certificate2Collection _Signers;
            StoreLocation _Location;
            #endregion

            /// <summary>
            /// Construct a new empty certificate that will be stored to the location
            /// specified.
            /// </summary>
            /// <param name="loc">The store location</param>
            public CStoredCertificate(StoreLocation loc)
            {
                _Location = loc;
            }

            /// <summary>
            /// Creates a new StoredCertificate object by loading the certificate containing
            /// specified substring in its subject. If multiple certificates are found with
            /// this substring in the subject line, an expection is thrown.
            /// </summary>
            /// <param name="loc">The name of the certificate store location</param>
            /// <param name="substrSubject">The substring to search for</param>
            public CStoredCertificate(StoreLocation loc, string substrSubject)
            {
                _Location = loc;

                //now find the certificate
                _Certificate = GetCertificateFromStore(loc, substrSubject);

                //now attempt to build the chain, but catch any exception found and do not report it
                //as this does not need to succeed:
                try
                {
                    _Signers = CollectCertSigners(Certificate);
                }
                catch { }
            }

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="base64pfx">The certificate if a pfx file with base64 encoding</param>
            /// <param name="hasPrivateKey">Set to true if a private key is present</param>
            /// /// <param name="password">password for the pfx file</param>
            public virtual void SetCert(string base64pfx, SecureString password)
            {
                byte[] binary_pfx_data = System.Convert.FromBase64String(base64pfx);
                X509Certificate2 cert = new X509Certificate2(binary_pfx_data, password, X509KeyStorageFlags.Exportable);

                //make sure that the certificate meets our requirements:
                //if (cert.HasPrivateKey == false)
                //    throw new CryptographicException("Missing private key");

                // check hash: if (cert.SignatureAlgorithm.FriendlyName 

                //check key strength
                //if (cert.PrivateKey.KeySize < 2048)
                //    throw new CryptographicException("Key size too small");

                //if we got to here, the ceritificate is okay
                //let's first install it                
                InstallCert(cert, Location, StoreName.My);

                //now remove the old certificate
                if (Certificate != null)
                    RemoveCert(Certificate, Location, StoreName.My);

                //update our field
                _Certificate = cert;
            }

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="base64pfx">The certificate if a pfx file with base64 encoding</param>
            /// <param name="hasPrivateKey">Set to true if a private key is present</param>
            /// /// <param name="password">password for the pfx file</param>
            /*public virtual void SetCert(string base64pfx, string password)
            {
                SecureString pw = new SecureString();
                foreach (char c in password)
                    pw.AppendChar(c);
                SetCert(base64pfx, pw);
            }*/

            /// <summary>
            /// Set the certificate. This will update the store at the location specified
            /// when this object was created. If a previous certificate existed, it will
            /// be replaced.
            /// </summary>
            /// <param name="cert">The certificate</param>                        
            public virtual void SetCert(X509Certificate2 cert)
            {                
                //let's first install it                
                InstallCert(cert, Location, StoreName.My);

                //now remove the old certificate
                if (Certificate != null)
                    RemoveCert(Certificate, Location, StoreName.My);

                //update our field
                _Certificate = cert;
            }

            /// <summary>
            /// Set the certificate signers. This takes a PEM file (in string form) as an 
            /// input. The first certificate in the string should be the original signer
            /// of the primary certificate and should continue in order until the final
            /// certificate which should be a self-signed root.
            /// </summary>
            /// <param name="PEM_string">The PEM formatted string of signers</param>
            /// <param name="checkChain">Set to true if the function should verify the certificate chain.</param>
            public virtual void SetCertSigners(string PEM_string, bool checkChain)
            {
                X509Certificate2Collection collection = GetCertCollectionFromPEM(PEM_string);

                if (collection.Count == 0)
                    throw new CryptographicException("NoCertFound: No valid certificates found");

                //now verifiy that the certificates sign each other
                if (checkChain)                    
                    ChainCertificateAndSigners(Certificate, collection, false); // this will throw an exception if it fails

                //Install the certificate chain:
                InstallCertificateChain(Location, collection);

                _Signers = collection;
                
            }

            /// <summary>
            /// Exports the signer certificates to a PEM string. Each certificate is enclosed
            /// inside "-----BEGIN CERTIFICATE----- ... -----END CERTIFICATE-----"
            /// The first certificate corresponds to the first signer of the primary certificate
            /// and so on until the final certificate is the self-signed root.
            /// </summary>
            /// <returns>The PEM formatted string.</returns>
            public virtual string ExportSignersToPEM()
            {
                StringBuilder pem_strb = new StringBuilder();
                foreach (X509Certificate2 cert in Signers)
                {
                    pem_strb.Append(CStoredCertificate.ExportCertificateToPEM(cert));
                    if (cert != Signers[Signers.Count - 1])
                        pem_strb.Append("\r\n");
                }

                return pem_strb.ToString();
            }

            /// <summary>
            /// Call this function to remove the attached certificate from the windows certificate store
            /// </summary>
            public void RemoveTheCert()
            {
                if (Certificate == null)
                    throw new InvalidOperationException("There is currently no certificate.");
                RemoveCert(Certificate, Location, StoreName.My);
                
            }

            #region Public static supporting functions
            public static void RemoveCert(X509Certificate2 cert, StoreLocation location, StoreName name)
            {
                X509Store store = new X509Store(name, location);
                store.Open(OpenFlags.ReadWrite);
                store.Remove(cert);
                store.Close();
            }

            /// <summary>
            /// This function constructs the X509Chain for the primary_certificate from the signers passed in,
            /// ignoring any revoked certificates and key usage intentions. This can be used to verify that these
            /// signers do indeed sign the primary_cert as the function will throw an exception if they do not. 
            /// However, think about this logic more before trusting it in a critical environment.
            /// Make sure that signers has the certificates in the correct order (index 0 is the cert that signed
            /// the primary certificate and the final index is the root)
            /// </summary>
            /// <param name="primary_certificate">the certificate to be verified</param>
            /// <param name="signers">the certificates (in order) the caller thinks signed it</param>
            /// <param name="requireTrustedRoot">set to false if the root does not need to be installed in windows certificate store</param>
            /// <returns>The constructed X509Chain object consisting of the signers if successful, otherwise throws a cryptographic exception with an error code</returns>
            public static X509Chain ChainCertificateAndSigners(X509Certificate2 primary_certificate, X509Certificate2Collection signers, bool requireTrustedRoot)
            {
                X509Chain chain = new X509Chain();
                foreach (X509Certificate2 cert in signers)
                    chain.ChainPolicy.ExtraStore.Add(cert);

                //alter how the chain is built/validated:
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;
                if (!requireTrustedRoot)
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                //do the preliminary validation: -- with the above settings, this only makes sure that the certs are valid
                //and assembles a chain from wherever it can find valid certs from (whether that is ExtraStore or in the 
                //main certificate store
                if (!chain.Build(primary_certificate))
                    throw new CryptographicException("ChainBuildFail: chain.Build() failed, status of element 0 is: " + chain.ChainStatus[0].ToString());
                //return false;

                //see if the number of elements in the newly assembled chain matches the number that added            
                if (chain.ChainElements.Count != chain.ChainPolicy.ExtraStore.Count + 1)
                    throw new CryptographicException("SignerCountMismatch: The constructed chain consists of "
                        + (chain.ChainElements.Count - 1).ToString() + " signers but "
                            + chain.ChainPolicy.ExtraStore.Count.ToString() + " signers were expected");

                //the numbers match, so now let's see if the validated chain contains the same certificates that we added
                //by checking that the thumbprints of the CAs match up.
                //The first one should be 'primaryCert', leading up to the root CA.
                for (int i = 1; i < chain.ChainElements.Count; i++)
                    if (chain.ChainElements[i].Certificate.Thumbprint != chain.ChainPolicy.ExtraStore[i - 1].Thumbprint)
                        throw new CryptographicException("IncorrectSigner: an incorrect (or out of order) signer was included, expected: "
                        + chain.ChainElements[i].Certificate.Subject + " but found "
                            + chain.ChainPolicy.ExtraStore[i - 1].Subject);

                //now make sure that the last certificate signed itself
                if (chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Issuer != chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Subject)
                    throw new CryptographicException("MissingRootCert: the final issuer did not sign itself: "
                        + chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Issuer + " is not the same as "
                            + chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Subject);

                return chain;
            }

            /// <summary>
            /// Get the signers for a certificate
            /// </summary>
            /// <param name="cert">the primary certificate</param>
            /// <returns>A collection of signers of this certificate</returns>
            public static X509Certificate2Collection CollectCertSigners(X509Certificate2 cert)
            {
                X509Certificate2Collection theSigners = new X509Certificate2Collection();
                X509Chain chain = new X509Chain();
                if (chain.Build(cert))
                {
                    //successful, now grab the collection                     
                    foreach (X509ChainElement elem in chain.ChainElements)
                        if (elem.Certificate != cert)
                            theSigners.Add(elem.Certificate);

                }
                else
                    throw new CryptographicException("ChainBuildFail: unable to construct a certificate chain for the certificate");

                return theSigners;
            }

            /// <summary>
            /// This function takes a collection of certificates in a PEM file (in a string format)
            /// and returns an X509Certificate2Collection object containing them.
            /// </summary>
            /// <param name="PEM_string"></param>
            /// <returns>The collection of certificates</returns>
            public static X509Certificate2Collection GetCertCollectionFromPEM(string PEM_string)
            {
                byte[] binary_pfx_data;
                X509Certificate2Collection collection = new X509Certificate2Collection();

                string[] lines;
                PEM_string = PEM_string.Replace("\r\n", "\n");
                lines = PEM_string.Split('\n');

                StringBuilder strCertB = new StringBuilder();
                bool inCert = false;
                foreach (string line in lines)
                {
                    if (!inCert)
                    {
                        if (line.Contains("-----BEGIN CERTIFICATE-----"))
                        {
                            inCert = true;
                            strCertB = new StringBuilder();
                        }
                    }
                    else
                    {
                        if (line.Contains("-----END CERTIFICATE-----"))
                        {
                            inCert = false;
                            binary_pfx_data = System.Convert.FromBase64String(strCertB.ToString());
                            X509Certificate2 cert = new X509Certificate2(binary_pfx_data);
                            collection.Add(cert);
                        }
                        else
                            strCertB.Append(line);
                    }
                }
                return collection;
            }

            /// <summary>
            /// This function takes a certificate signing request in a PEM file 
            /// (in a string format) and returns an X509Certificate2 object.
            /// </summary>
            /// <param name="PEM_string"></param>
            /// <returns></returns>
            public static X509Certificate2 GetCertificateReqFromPEM(string PEM_string)
            {
                PEM_string = PEM_string.Replace("CERTIFICATE REQUEST", "CERTIFICATE");
                X509Certificate2Collection collection = GetCertCollectionFromPEM(PEM_string);
                return collection[0];
            }

            /// <summary>
            /// This function takes a certificate in a PEM file (in a string 
            /// format) and returns an X509Certificate2 object.
            /// </summary>
            /// <param name="PEM_string"></param>
            /// <returns></returns>
            public static X509Certificate2 GetCertificateFromPEM(string PEM_string)
            {
                X509Certificate2Collection collection = GetCertCollectionFromPEM(PEM_string);
                return collection[0];
            }

            /// <summary>
            /// Export the public portion of a certificate to a PEM string.
            /// </summary>
            /// <param name="cert"></param>
            /// <returns></returns>
            public static string ExportCertificateToPEM(X509Certificate cert)
            {
                StringBuilder builder = new StringBuilder();
                
                builder.AppendLine("-----BEGIN CERTIFICATE-----");
                //specify X509ContentType.Cert to get only the public key
                builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
                builder.Append("-----END CERTIFICATE-----");

                return builder.ToString();
            }

            /// <summary>
            /// Export the public portion of a certificate request to a PEM string.
            /// </summary>
            /// <param name="cert"></param>
            /// <returns></returns>
            public static string ExportCertificateRequestToPEM(X509Certificate req)
            {
                StringBuilder builder = new StringBuilder();

                builder.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");
                //specify X509ContentType.Cert to get only the public key
                builder.AppendLine(Convert.ToBase64String(req.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
                builder.Append("-----END CERTIFICATE REQUEST-----");

                return builder.ToString();
            }

            

            /// <summary>
            /// This is a way to get a regular string into a secure string
            /// NOTE that this is bad form. It negates the security gained
            /// by using a secure string.
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static SecureString MakeSecureString(string str)
            {
                SecureString pw = new SecureString();
                foreach (char c in str)
                    pw.AppendChar(c);
                return pw;
            }
            #endregion

            #region Helper functions


            
            /// <summary>
            /// Retrieve a certificate from the indicated store by searching the subject name.
            /// Throw an exception if more (or less) than one valid certificate is found which
            /// has a subject name that contains the search string.
            /// </summary>
            /// <param name="loc"></param>
            /// <param name="substrSubject"></param>
            /// <returns></returns>
            protected X509Certificate2 GetCertificateFromStore(StoreLocation loc, string substrSubject)
            {
                X509Certificate2 cert;
                X509Store store = new X509Store(loc);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindBySubjectName, substrSubject, true);
                store.Close();

                //more than one found?
                if (col.Count > 1)
                    throw new CryptographicException("MultipleCertsFound: " + col.Count.ToString() + " certificates found in "
                        + loc.ToString() + " store which contain \"" + substrSubject + "\" in their subject line");

                //no certs found?
                if (col.Count == 0)
                    throw new CryptographicException("NoCertFound: no certificate found in " + loc.ToString() + " store which contains \"" +
                        substrSubject + "\" in the subject line");

                //we got it
                cert = col[0];

                return cert;
            }

            protected void InstallCert(X509Certificate2 cert, StoreLocation location, StoreName name)
            {
                X509Store store = new X509Store(name, location);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
            }

            

            private void InstallCertificateChain(StoreLocation location, X509Certificate2Collection collection)
            {
                foreach (X509Certificate2 cert in collection)
                {                    
                    if (cert.Subject == cert.Issuer)
                    {                        
                        //self signed, must be a root
                        X509Store store = new X509Store(StoreName.Root, location);
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(cert);
                        store.Close();                        
                    }
                    else
                    {                        
                        bool bCA = false;
                        //check to make sure it can sign certificates (basic contraints)
                        foreach (X509Extension ext in cert.Extensions)
                        {
                            if (ext is X509BasicConstraintsExtension)
                            {
                                X509BasicConstraintsExtension bext = (X509BasicConstraintsExtension)ext;
                                bCA = bext.CertificateAuthority;
                                if (!bCA)
                                    throw new CryptographicException("CertMissingPriviliges: found a certificate in the signers collection that does not have permission to sign another certificate: " +
                                        cert.Subject); //Console.WriteLine("WARNING -- non certificate authority certificate found, ignoring this certificate");
                            }
                        }

                        //must be an intermediate cert
                        if (bCA)
                        {
                            X509Store store = new X509Store(StoreName.CertificateAuthority, location);
                            store.Open(OpenFlags.ReadWrite);
                            store.Add(cert);
                            store.Close();
                        }
                    }
                }
            }
            #endregion
        }


    }
}
