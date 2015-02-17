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

        public class UUencoding
        {
            /// <summary>
            /// Creates a UU-encoded line from binary data. A line
            /// can contain a maximum of 45 binary bytes.
            /// </summary>
            /// <param name="line_data">The binary line data. Cannot exceed 45 bytes</param>
            /// <returns>The UU-encoded line</returns>
            public static string UU_EncodeLine(byte[] line_data)
            {
                StringBuilder outputstr = new StringBuilder();
                if (line_data.Length > 45)
                    throw new ArgumentException("Each line can be a maximum of 45 bytes.", "line_data");

                //first byte is the length of the actual data
                outputstr.Append(System.Text.Encoding.Default.GetString(new byte[1] { encode_byte((byte)line_data.Length) }));

                //0 pad to get the length divisible by 3
                while (line_data.Length % 3 != 0)
                {
                    byte[] temp = new byte[line_data.Length+1];
                    Array.Copy(line_data, temp, line_data.Length);
                    temp[temp.Length-1] = 0;
                    line_data = temp;
                }
                for (int i = 0; i < line_data.Length; i+=3)
                {
                    byte[] outbuf = new byte[4];
                    outbuf[0] = encode_byte((byte) ( (line_data[i] & 0xFC) >> 2));
                    outbuf[1] = encode_byte((byte) (((line_data [i] & 0x03) << 4) +
                                                    ((line_data [i+1] & 0xF0) >> 4)));
                    outbuf[2] = encode_byte((byte) (((line_data [i+1] & 0x0F) << 2) +
                                                    ((line_data [i+2] & 0xC0) >> 6)));
                    outbuf[3] = encode_byte((byte) (line_data [i+2] & 0x3F));
                    outputstr.Append(System.Text.Encoding.Default.GetString(outbuf));                                        
                }
                return outputstr.ToString();
            }


            /// <summary>
            /// Decode a UU encoded line into binary data.
            /// The line should not contain the new line character (i.e.: \r 
            /// should not be present)
            /// </summary>
            /// <param name="uuLine">The UU encoded line. Length should be 1 + a multiple of 4. The max allowable length is 61.</param>
            /// <returns>The raw binary data.</returns>
            public static byte[] UU_DecodeLine(string uuLine)
            {
                byte [] return_bytes = null;
                if (uuLine.Length > 61)
                    throw new ArgumentException("Each UU line can be a maximum of 61 uu characters.", "uuLine");
                if ( (uuLine.Length - 1) % 4 != 0)
                    throw new ArgumentException("The UU line length - 1 must be a multiple of 4.", "uuLine");

                byte[] uu_line = System.Text.Encoding.ASCII.GetBytes(uuLine);
                byte linelen_remain = decode_byte(uu_line[0]);
                for (int i = 1; i < uu_line.Length; i += 4)
                {
                    byte[] outbyte = new byte[3];
                    outbyte[0] = decode_byte (uu_line [0]);
                    outbyte[1] = decode_byte (uu_line [1]);
                    outbyte[0] <<= 2;
                    outbyte[0] |= (byte) ((outbyte [1] >> 4) & 0x03);
                    outbyte[1] <<= 4;
                    outbyte[2] = decode_byte(outbyte[2]);
                    outbyte[1] |= (byte) ((outbyte [2] >> 2) & 0x0F);
                    outbyte[2] <<= 6;
                    outbyte[2] |= (byte)(decode_byte(outbyte[3]) & 0x3F);

                    if (linelen_remain < 3)
                    {
                        byte[] temp_bytes = new byte[linelen_remain];
                        Array.Copy(outbyte, temp_bytes, linelen_remain);
                        outbyte = temp_bytes;
                    }

                    if (return_bytes == null)
                        return_bytes = outbyte;
                    else
                    {
                        byte[] temp_bytes = new byte[return_bytes.Length + outbyte.Length];
                        Array.Copy(return_bytes, temp_bytes, return_bytes.Length);
                        Array.Copy(outbyte, 0, temp_bytes, return_bytes.Length, outbyte.Length);
                        return_bytes = temp_bytes;
                    }

                    linelen_remain -= (byte) outbyte.Length;
                }

                return return_bytes;
            }

            #region private helper functions
            private static byte encode_byte(byte b)
            {
                if (b == 0)
                    return 0x60;
                else
                    return (byte)(b + 0x20);
            }
            private static byte decode_byte(byte b)
            {
                if (b == 0x60)
                    return 0x0;
                else
                    return (byte)(b - 0x20);
            }
            #endregion

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
