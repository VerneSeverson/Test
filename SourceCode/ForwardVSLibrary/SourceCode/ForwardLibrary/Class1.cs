using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    /// <summary>
    /// a namespace to tuck library functions in that don't belong elsewhere
    /// </summary>
    namespace Default
    {
        /// <summary>
        /// Attribute for enums to allow them to have a friendly name
        /// </summary>
        public class FriendlyName : System.Attribute
        {
            private string _value;

            public FriendlyName(string value)
            {
                _value = value;
            }

            public string Value
            {
                get { return _value; }
            }
        }

        /// <summary>
        /// Class to house generic static functions
        /// </summary>
        public class FPS_LibFuncs
        {

            /// <summary>
            /// Get the friendly name attribute of an enum
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public static string GetEnumFriendlyName(Enum value)
            {
                string output = null;
                Type type = value.GetType();
                FieldInfo fi = type.GetField(value.ToString());
                FriendlyName[] attrs =
                    fi.GetCustomAttributes(typeof(FriendlyName),
                                   false) as FriendlyName[];
                if (attrs.Length > 0)
                    output = attrs[0].Value;

                return output;
            }


            public static T ParseEnumFriendlyName<T>(string friendly_name) where T : struct, IConvertible
            {
                foreach (T id_type in Enum.GetValues(typeof(T)))
                {
                    if (GetEnumFriendlyName(((Enum) (object) id_type)) == friendly_name)
                        return id_type;
                }
                
                throw new Exception("No match for the friendly_name");
            }

            /// <summary>
            /// Convert an ascii-coded hex string to a byte array.
            /// This code was taken from this example:
            /// http://stackoverflow.com/questions/321370/convert-hex-string-to-byte-array
            /// </summary>
            /// <param name="hex">the string of ascii-coded hex</param>
            /// <returns>the hex byte array</returns>
            public static byte[] AsciiEncodedHexStringToByteArray(string hex)
            {
                return Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/641361/base32-decoding
        /// </summary>
        public class Base32Encoding
        {
            public static byte[] ToBytes(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    throw new ArgumentNullException("input");
                }

                input = input.TrimEnd('='); //remove padding characters
                int byteCount = input.Length * 5 / 8; //this must be TRUNCATED
                byte[] returnArray = new byte[byteCount];

                byte curByte = 0, bitsRemaining = 8;
                int mask = 0, arrayIndex = 0;

                foreach (char c in input)
                {
                    int cValue = CharToValue(c);

                    if (bitsRemaining > 5)
                    {
                        mask = cValue << (bitsRemaining - 5);
                        curByte = (byte)(curByte | mask);
                        bitsRemaining -= 5;
                    }
                    else
                    {
                        mask = cValue >> (5 - bitsRemaining);
                        curByte = (byte)(curByte | mask);
                        returnArray[arrayIndex++] = curByte;
                        curByte = (byte)(cValue << (3 + bitsRemaining));
                        bitsRemaining += 3;
                    }
                }

                //if we didn't end with a full byte
                if (arrayIndex != byteCount)
                {
                    returnArray[arrayIndex] = curByte;
                }

                return returnArray;
            }

            public static string ToString(byte[] input)
            {
                if (input == null || input.Length == 0)
                {
                    throw new ArgumentNullException("input");
                }

                int charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
                char[] returnArray = new char[charCount];

                byte nextChar = 0, bitsRemaining = 5;
                int arrayIndex = 0;

                foreach (byte b in input)
                {
                    nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                    returnArray[arrayIndex++] = ValueToChar(nextChar);

                    if (bitsRemaining < 4)
                    {
                        nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                        returnArray[arrayIndex++] = ValueToChar(nextChar);
                        bitsRemaining += 5;
                    }

                    bitsRemaining -= 3;
                    nextChar = (byte)((b << bitsRemaining) & 31);
                }

                //if we didn't end with a full char
                if (arrayIndex != charCount)
                {
                    returnArray[arrayIndex++] = ValueToChar(nextChar);
                    while (arrayIndex != charCount) returnArray[arrayIndex++] = '='; //padding
                }

                return new string(returnArray);
            }

            private static int CharToValue(char c)
            {
                int value = (int)c;

                //65-90 == uppercase letters
                if (value < 91 && value > 64)
                {
                    return value - 65;
                }
                //50-55 == numbers 2-7
                if (value < 56 && value > 49)
                {
                    return value - 24;
                }
                //97-122 == lowercase letters
                if (value < 123 && value > 96)
                {
                    return value - 97;
                }

                throw new ArgumentException("Character is not a Base32 character.", "c");
            }

            private static char ValueToChar(byte b)
            {
                if (b < 26)
                {
                    return (char)(b + 65);
                }

                if (b < 32)
                {
                    return (char)(b + 24);
                }

                throw new ArgumentException("Byte is not a value Base32 value.", "b");
            }

        }


        //It is handy to have a void delegate type defined:
        public delegate void VoidDel();
    }
    public class Class1
    {
        //dummy class a
        //
        //
    }
}
