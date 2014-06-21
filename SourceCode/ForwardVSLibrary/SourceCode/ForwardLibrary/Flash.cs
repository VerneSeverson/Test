using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForwardLibrary
{
    namespace Flash
    {

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

        public class HexFile
        {
            public enum HexRecordType : byte
            {
                DataRecord = 0,
                EndOfFileRecord = 1,
                ExtendedSegmentAddressRecord = 2,
                StartSegmentAddressRecord = 3,
                ExtendedLinearAddressRecord = 4,
                StartLinearAddressRecord = 5
            };

            /// <summary>
            /// Indices of the fields within a hex line
            /// </summary>
            enum FieldIndices : int
            {
                StartChar = 0,
                DataByteCount = 1,
                Address = 3,
                RecordType = 7,
                Data = 9
            }

            /// <summary>
            /// Lengths of the fixed length fields
            /// </summary>
            enum FieldLengths : int
            {
                StartChar = 1,
                DataByteCount = 2,
                Address = 4,
                RecordType = 2,
                Checksum = 2
            }

            /// <summary>
            /// Determine the hex line's record type. The hex line's checksum is
            /// validated and an exception is throwm if the checksum doesn't match.            
            /// </summary>
            /// <param name="hexLine"></param>
            /// <returns></returns>
            /// <exception cref="HexLineException">Thrown when the hexLine is invalid, i.e. the checksum doesn't match or incorrect format</exception>
            public static HexRecordType GetHexLineType(string hexLine)
            {
                hexLine = hexLine.Trim();   //get rid of white space

                //check start character
                if (!hexLine.StartsWith(":"))
                    throw new HexLineException("The the start character ':' is missing.", hexLine);

                //validate the line length
                DetermineHexLineLength(hexLine);                              

                //validate the checksum
                VerifyHexLineChecksum(hexLine);

                //we know the line checks out, so now grab the record type:
                byte recordType = Convert.ToByte(hexLine.Substring((int) FieldIndices.RecordType, (int) FieldLengths.RecordType), 16);
                if ((recordType > (byte)HexRecordType.StartLinearAddressRecord)
                        || (recordType < (byte)HexRecordType.DataRecord))
                    throw new HexLineException("Invalid record type found: " + recordType, hexLine);
                return (HexRecordType)recordType;
            }

            /// <summary>
            /// Determines the length of this hex line's data field. This length
            /// is the length of the ascii-coded hex.
            /// </summary>
            /// <param name="hexLine"></param>
            /// <returns></returns>
            /// <exception cref="HexLineException">Thrown when the an error is encountered with parsing the hex line.</exception>
            public static int DetermineHexLineDataLength(string hexLine)
            {
                hexLine = hexLine.Trim();   //get rid of white space

                //check start character
                if (!hexLine.StartsWith(":"))
                    throw new HexLineException("The the start character ':' is missing.", hexLine);

                //check for minimum length: 1 start char + 2 length char + 4 address char + 2 data type char + (?) data char + 2 checksum char
                if (hexLine.Length < 11)
                    throw new HexLineException("The line is too short.", hexLine);

                //determine date length:
                string lenStr = hexLine.Substring((int)FieldIndices.DataByteCount, (int)FieldLengths.DataByteCount);
                int dataLen;
                try
                {
                    dataLen = Convert.ToInt32(lenStr, 16) * 2;
                }
                catch (Exception ex)
                {
                    throw new HexLineException("Unable to parse the length field.", hexLine, ex);
                }

                //check actual length: 1 start char + 2 length char + 4 address char + 2 data type char + (dataLen) data char + 2 checksum char
                if (hexLine.Length != (11 + dataLen))
                    throw new HexLineException("The line is the incorrect length.", hexLine);

                return dataLen;
            }


            /// <summary>
            /// Determines the length of this hex line.
            /// </summary>
            /// <param name="hexLine"></param>
            /// <returns></returns>
            /// <exception cref="HexLineException">Thrown when the an error is encountered with parsing the hex line.</exception>
            public static int DetermineHexLineLength(string hexLine)
            {
                return DetermineHexLineDataLength(hexLine) + 11;
            }

            public static void VerifyHexLineChecksum(string hexLine)
            {
                hexLine = hexLine.Trim();   //get rid of white space

                //check start character
                if (!hexLine.StartsWith(":"))
                    throw new HexLineException("The the start character ':' is missing.", hexLine);
                
                int checksumInt = 0;
                string dataToCsum = hexLine.Substring((int) FieldIndices.DataByteCount, hexLine.Length - 3);

                for (int i = 0; i < dataToCsum.Length / 2; i++)
                {
                    int by = Convert.ToInt32(dataToCsum.Substring(i * 2, 2),16);
                    checksumInt = (checksumInt + by) & 0xff;
                }

                checksumInt = (((~checksumInt) & 0xff) + 1) & 0xff;

                int foundChecksum = Convert.ToInt32(hexLine.Substring(hexLine.Length - 2), 16);

                if (checksumInt != foundChecksum)
                    throw new HexLineException("The checksum is invalid.", hexLine);
            }

            static UInt32 GetHexLineAddressFieldVal(string hexLine)
            {
                hexLine = hexLine.Trim();
                UInt32 val;
                try
                {
                    val = Convert.ToUInt32(hexLine.Substring((int)FieldIndices.Address, (int)FieldLengths.Address), 16);
                }
                catch (Exception ex)
                {
                    throw new HexLineException("Unable to parse the address field.", hexLine, ex);
                }
                return val;
            }

            /// <summary>
            /// This returns the address that this indicated by the hex line.
            /// In most cases, this is simply the address field (16 bits). However, 
            /// if:
            ///  -> the record type is Extended Linear Address, 
            ///     it is a 32bit number (lower 16 bits are 0)
            ///  -> the record type is Start Linear Address Record,
            ///     it is a 32bit number
            /// </summary>
            /// <param name="hexLine"></param>
            /// <exception cref="HexLineException">Thrown when the an error is encountered with parsing the hex line.</exception>
            public static UInt32 GetHexLineAddress(string hexLine)
            {
                UInt32 address = 0;
                hexLine = hexLine.Trim();
                switch (GetHexLineType(hexLine))
                {
                    case HexRecordType.ExtendedLinearAddressRecord:
                        if (GetHexLineAddressFieldVal(hexLine) != 0)
                            throw new HexLineException("Address field is expected to be zero but it is not.", hexLine);
                        else if (DetermineHexLineDataLength(hexLine) != 4)
                            throw new HexLineException("The data field length is expected to be two bytes but it is not.", hexLine);
                        else
                        {
                            try
                            {
                                address = Convert.ToUInt32(hexLine.Substring((int)FieldIndices.Data, 4) + "0000", 16);
                            }
                            catch (Exception ex)
                            {
                                throw new HexLineException("Unable to parse the data field.", hexLine, ex);
                            }
                        }
                        break;

                    case HexRecordType.StartLinearAddressRecord:
                        if (GetHexLineAddressFieldVal(hexLine) != 0)
                            throw new HexLineException("Address field is expected to be zero but it is not.", hexLine);
                        else if (DetermineHexLineDataLength(hexLine) != 8)
                            throw new HexLineException("The data field length is expected to be four bytes but it is not.", hexLine);
                        else
                        {
                            try
                            {
                                address = Convert.ToUInt32(hexLine.Substring((int)FieldIndices.Data, 8), 16);
                            }
                            catch (Exception ex)
                            {
                                throw new HexLineException("Unable to parse the data field.", hexLine, ex);
                            }
                        }
                        break;

                    default:
                        address = GetHexLineAddressFieldVal(hexLine);
                        break;
                }

                return address;
            }

            /// <summary>
            /// Get the binary datafield from a data record hex line
            /// </summary>
            /// <param name="hexLine"></param>
            /// <returns></returns>
            /// <exception cref="HexLineException">Thrown when the an error is encountered with parsing the hex line.</exception>
            public static byte[] GetDataHexLineData(string hexLine)
            {
                byte[] data;
                try
                {
                    hexLine = hexLine.Trim();
                    if (GetHexLineType(hexLine) != HexRecordType.DataRecord)
                        throw new HexLineException("Invalid record type. Expected a data record.", hexLine);

                    string strdata = hexLine.Substring((int)FieldIndices.Data, DetermineHexLineDataLength(hexLine));
                    data = new byte[strdata.Length / 2];
                    for (int i = 0; i < strdata.Length / 2; i++)
                        data[i] = Convert.ToByte(strdata.Substring(i * 2, 2), 16);
                }
                catch (HexLineException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new HexLineException("Unable to parse the hex line.", hexLine, ex);
                }
                return data;
            }
        }

        public interface IFlashDevice
        {
            /// <summary>
            /// The number of sectors that the device has
            /// </summary>
            int NumberOfSectors
            {
                get;
            }            

            /// <summary>
            /// Returns the size (in bytes) of a flash sector
            /// </summary>
            /// <param name="sector">The zero-based sector of interest</param>
            /// <returns>The size of the sector</returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            uint SectorSize(uint sector);

            /// <summary>
            /// Returns start address of a flash sector
            /// </summary>
            /// <param name="sector">The zero-based sector of interest</param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            uint SectorAddress(uint sector);

            /// <summary>
            /// Returns which sector an address resides in
            /// </summary>
            /// <param name="address">The address of interest</param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException">Thrown when the address does not reside in any sector.</exception>
            uint InSector(uint address);

            /// <summary>
            /// Returns the binary data for a sector
            /// </summary>
            /// <param name="sector"></param>
            /// <returns></returns>
            byte[] GetSectorData(uint sector);

            /// <summary>
            /// Loads a hex file into the flash object
            /// </summary>
            /// <param name="fileName">the name of the hex file to load</param>
            void LoadHexFile(string fileName);

            /// <summary>
            /// Loads a hex file into the flash object
            /// </summary>
            /// <param name="hexFileLines">the lines of the hex file</param>
            void LoadHexFile(IEnumerable<string> hexFileLines);
        }

        /// <summary>
        /// This abstract class implements common functionality for flash devices
        /// </summary>
        public abstract class StandardFlash : IFlashDevice 
        {
            #region properties
            protected uint[] SSize; /* = {   0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x8000,
                                                            0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x8000, 0x8000, 
                                                            0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x1000,  0x1000, 0x1000, 
                                                            0x1000, 0x1000, 0x1000 };*/
            protected uint[] SAddress; /* = { 0x0, 0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000, 0x8000, 
                                                            0x10000, 0x18000, 0x20000, 0x28000, 0x30000, 0x38000, 0x40000, 0x48000, 
                                                            0x50000, 0x58000, 0x60000, 0x68000, 0x70000, 0x78000, 0x79000, 0x7A000,
                                                            0x7B000, 0x7C000, 0x7D000};*/
            /// <summary>
            /// The number of sectors that the device has
            /// </summary>
            public int NumberOfSectors
            {
                get { return SSize.Length; }
            }

            protected List<byte[]> BinarySectorData;

            #endregion

            /// <summary>
            /// Returns the size (in bytes) of a flash sector
            /// </summary>
            /// <param name="sector">The zero-based sector of interest</param>
            /// <returns>The size of the sector</returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            public uint SectorSize(uint sector)
            {
                if (sector >= NumberOfSectors)
                    throw new ArgumentException("Information was requested for an invalid sector.", "sector");

                return SSize[sector];
            }

            /// <summary>
            /// Returns start address of a flash sector
            /// </summary>
            /// <param name="sector">The zero-based sector of interest</param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            public uint SectorAddress(uint sector)
            {
                if (sector >= NumberOfSectors)
                    throw new ArgumentException("Information was requested for an invalid sector.", "sector");
                return SAddress[sector];
            }

            /// <summary>
            /// Returns which sector an address resides in
            /// </summary>
            /// <param name="address">The address of interest</param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException">Thrown when the address does not reside in any sector.</exception>
            public uint InSector(uint address)
            {
                uint sector = 0;
                if (address >= (SAddress[SAddress.Length-1] + SSize[SSize.Length-1]))
                    throw new ArgumentException("The specified address does not reside in any sector.", "address");
                for (uint i = 0; i < SAddress.Length; i++)
                {
                    if (address < (SAddress[i] + SSize[i]))
                    {
                        sector = i;
                        break;
                    }
                }
                return sector;
            }

            /// <summary>
            /// Returns the binary data for a sector
            /// </summary>
            /// <param name="sector"></param>
            /// <returns></returns>
            public byte[] GetSectorData(UInt32 sector)
            {
                if (sector >= NumberOfSectors)
                    throw new ArgumentException("Information was requested for an invalid sector.", "sector");
                return BinarySectorData[(int)sector];
            }

            /// <summary>
            /// Load hex file into the flash
            /// </summary>
            /// <param name="hexFileLines">The hex file name to load</param>
            public void LoadHexFile(string filename)
            {
                //open file and read it in
                LoadHexFile(File.ReadAllLines(filename));
            }

            /// <summary>
            /// Load hex file into the flash
            /// </summary>
            /// <param name="hexFileLines">The lines of the hex file</param>
            public virtual void LoadHexFile(IEnumerable<string> hexFileLines)
            {
                uint upperAddr = 0, addr = 0, offset;
                byte[] data;
                uint sector = 0;
                foreach (string line in hexFileLines)
                {
                    switch (HexFile.GetHexLineType(line))
                    {
                        case HexFile.HexRecordType.DataRecord:
                            addr = upperAddr + HexFile.GetHexLineAddress(line);
                            data = HexFile.GetDataHexLineData(line);
                            sector = InSector(addr);
                            offset = addr - SectorAddress(sector);
                            if (sector != InSector(addr + (uint)data.Length-1))
                                throw new HexLineException("The hex line contains data which spans two sectors. This is not allowed.", line);
                            data.CopyTo(BinarySectorData[(int)sector], offset);
                            break;

                        case HexFile.HexRecordType.ExtendedLinearAddressRecord:
                            upperAddr = HexFile.GetHexLineAddress(line);
                            break;

                        default:
                            //don't implement any other record types for now
                            break;
                    }
                }                    
            }
            #region constructors
            

            #endregion

            #region Helper functions
            
            #endregion
        }



        /// <summary>
        /// This object represents the memory of a LPC2000 device
        /// </summary>
        public class InternalFlash_LPC2000 : StandardFlash
        {
            #region properties
            
            #endregion

            #region constructors
            public InternalFlash_LPC2000()
            {
                BasicConstruction();
            }
            

            #endregion

            #region Helper functions
            private void BasicConstruction()
            {
                SSize = new uint[] {    0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x1000, 0x8000,
                                        0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x8000, 0x8000, 
                                        0x8000,  0x8000,  0x8000,  0x8000,  0x8000,  0x1000,  0x1000, 0x1000, 
                                        0x1000, 0x1000, 0x1000 };
                SAddress = new uint[] { 0x0, 0x1000, 0x2000, 0x3000, 0x4000, 0x5000, 0x6000, 0x7000, 0x8000, 
                                        0x10000, 0x18000, 0x20000, 0x28000, 0x30000, 0x38000, 0x40000, 0x48000, 
                                        0x50000, 0x58000, 0x60000, 0x68000, 0x70000, 0x78000, 0x79000, 0x7A000,
                                        0x7B000, 0x7C000, 0x7D000};

                //create an empty byte array for each sector
                BinarySectorData = new List<byte[]>(NumberOfSectors);
                for (int i = 0; i<NumberOfSectors; i++)
                    BinarySectorData.Add(new byte[SectorSize((uint) i)]);

            }
            #endregion
        }

    }
}
