using ForwardLibrary.Communications;
using ForwardLibrary.Communications.CommandHandlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            /// validated and an exception is thrown if the checksum doesn't match.            
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
            /// <exception cref="HexLineException">Thrown when an error is encountered with parsing the hex line.</exception>
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

            /// <summary>
            /// Verifies the checksum of this hex line.
            /// </summary>
            /// <param name="hexLine"></param>
            /// <exception cref="HexLineException">Thrown when the checksum is invalid or an error is encountered with parsing the hex line.</exception>
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
            /// This returns the address that is indicated by the hex line.
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
            /// <exception cref="HexLineException">Thrown when the an error is encountered with parsing the hex line or if the hex line is not a data record.</exception>
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
            /// Indicates whether or not a sector has data
            /// </summary>
            /// <param name="sector">The sector of interest</param>
            /// <returns>True if the sector is empty</returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            bool SectorEmpty(uint sector);

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
            protected bool[] SWrittenTo; 
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
            /// Indicates whether or not a sector has data
            /// </summary>
            /// <param name="sector">The sector of interest</param>
            /// <returns>True if the sector is empty</returns>
            /// <exception cref="System.ArgumentException">Thrown when an invalid sector is specified.</exception>
            public bool SectorEmpty(uint sector)
            {
                if (sector >= NumberOfSectors)
                    throw new ArgumentException("Information was requested for an invalid sector.", "sector");
                return !SWrittenTo[sector];
            }

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
                            SWrittenTo[(int)sector] = true;
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
            
            /// <summary>
            /// Read the 32 bit int at the specified address.
            /// </summary>
            /// <param name="address">the address to read. Note that this must be divisible by 4 (word aligned)</param>
            /// <returns></returns>
            protected uint GetDataAtAddress(uint address)
            {
                //0. make sure that address is word aligned:
                if (address % 4 != 0)
                    throw new ArgumentException("The address is not word aligned.", "startAddr");

                //1. determine sector of start address
                uint sector = InSector(address);
                if (!SWrittenTo[sector])
                    throw new InvalidOperationException("The sector of the address has not loaded with data.");

                //2. determine the offset of the address within the sector
                uint offset = address - SectorAddress(sector);

                //3. convert the bytes to a uint
                uint result = BitConverter.ToUInt32(BinarySectorData[(int) sector], (int) offset);

                return result;
            }

            /// <summary>
            /// Write a 32 bit uint at the specified address.
            /// </summary>
            /// <param name="address">the address to write to. Note that this must be divisible by 4 (word aligned)</param>
            /// <param name="data">the data to write</param>
            /// <returns></returns>
            protected void WriteDataAtAddress(uint address, uint data)
            {
                //0. make sure that address is word aligned:
                if (address % 4 != 0)
                    throw new ArgumentException("The address is not word aligned.", "startAddr");

                //1. determine sector of start address
                uint sector = InSector(address);
                /*if (!SWrittenTo[sector])
                    throw new InvalidOperationException("The sector of the address has not loaded with data.");*/

                //2. determine the offset of the address within the sector
                uint offset = address - SectorAddress(sector);

                //3. get the bytes of the int
                byte [] databytes = BitConverter.GetBytes(data);

                //4. copy it in
                databytes.CopyTo(BinarySectorData[(int) sector], offset);                

                //5. flag the sector as having been written to
                SWrittenTo[(int) sector] = true;
            }
            #endregion
        }


        namespace LPC2400
        {
            /// <summary>
            /// This object represents the memory of a LPC2400 device
            /// </summary>
            public class InternalFlash_LPC2400 : StandardFlash
            {
                #region properties
                public uint Checksum1Start_Address = 0x20;
                public uint Checksum1End_Address = 0x24;
                public uint Checksum1_Address = 0x28;

                public uint Checksum2Start_Address = 0x2C;
                public uint Checksum2End_Address = 0x30;
                public uint Checksum2_Address = 0x34;
                #endregion

                #region constructors
                public InternalFlash_LPC2400()
                {
                    BasicConstruction();
                }
            

                #endregion

                /// <summary>
                /// Call this function to add the ISR checksum at offset address 0x14 (LPC2000 needs this)
                /// </summary>
                public void InsertISR_Checksum()
                {
                    uint sector; 
                    //1. find the first sector occupied
                    for (sector = 0; sector < NumberOfSectors; sector++)
                    {
                        if (SWrittenTo[sector])
                            break;
                    }

                    uint checksum = 0;
                    for (uint i = 0; i < 8; i++)
                        if (i * 4 != 0x14)
                            checksum += GetDataAtAddress((uint) SectorAddress(sector) + i*4);

                    checksum = (~checksum) + 1;                
                    WriteDataAtAddress(SectorAddress(sector) + 0x14, checksum);
                }

                /// <summary>
                /// call this to add the checksums at the offsets specified
                /// </summary>
                /// <exception cref="System.InvalidOperationException">Thrown when there is no valid data to checksum.</exception>
                public void InsertChecksums()
                {
                    uint sector, firstSector;    
                    uint valCheck1Start, valCheck1End, valCheck1;
                    uint valCheck2Start, valCheck2End, valCheck2;  
                    //1. find the first sector occupied
                        for (sector= 0; sector < NumberOfSectors; sector++)
                        {
                            if (SWrittenTo[sector])
                                break;
                        }
                        if (sector >= NumberOfSectors)
                            throw new InvalidOperationException("No sectors to checksum.");

                    //2. calculate the first checksum                                    
                        //this is the first sector, so we want to start checksumming after Checksum2_Address
                        firstSector = sector;
                        valCheck1Start = SectorAddress(firstSector) + Checksum2End_Address + 4;
                        PrepareChecksum(valCheck1Start, out valCheck1End, out valCheck1);

                    //3. find the next sector occupied
                        if (valCheck1End + 4 < (SectorAddress((uint)NumberOfSectors - 1) + SectorSize((uint)NumberOfSectors - 1)))
                        {
                            for (sector = InSector(valCheck1End+4); sector < NumberOfSectors; sector++)
                            {
                                if (SWrittenTo[sector])
                                    break;
                            }
                        }

                    //4. calculate the second checksum
                        if (sector >= NumberOfSectors)
                        {
                            //no more data to checksum, so let's just checksum the first checksum
                            valCheck2Start = SectorAddress(firstSector) + Checksum1_Address;
                            valCheck2End = SectorAddress(firstSector) + Checksum1_Address;
                            valCheck2 = (~valCheck1) + 1;
                        }
                        else
                        {
                            //there is more data to checksum
                            valCheck2Start = SectorAddress(sector);
                            PrepareChecksum(valCheck2Start, out valCheck2End, out valCheck2);
                        }

                    //5. Okay -- write our checksums out
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum1Start_Address, valCheck1Start);
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum1End_Address, valCheck1End);
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum1_Address, valCheck1);
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum2Start_Address, valCheck2Start);
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum2End_Address, valCheck2End);
                        WriteDataAtAddress(SectorAddress(firstSector) + Checksum2_Address, valCheck2);
                }

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
                    SWrittenTo = new bool[SAddress.Length]; //initializes every item to false
                 
                    //create an empty byte array for each sector
                    BinarySectorData = new List<byte[]>(NumberOfSectors);
                    for (int i = 0; i<NumberOfSectors; i++)
                        BinarySectorData.Add(new byte[SectorSize((uint) i)]);

                }

                /// <summary>
                /// Calculate the checksum for a continuous chunk of flash starting with address startAddr
                /// </summary>
                /// <param name="startAddr">The address to start calculating at. This must be divisible by 4 (word aligned)</param>
                /// <param name="endAddr">The last address that was included in the checksum </param>
                /// <param name="checksum">the checksum value</param>
                /// <exception cref="System.ArgumentException">Thrown when the start address is not valid (not word aligned or does not reside in a sector).</exception>
                /// <exception cref="System.InvalidOperationException">Thrown when the start sector has no data.</exception>
                private void PrepareChecksum(uint startAddr, out uint endAddr, out uint checksum)
                {
                    //0. make sure that startAddr is word aligned:
                    if (startAddr % 4 != 0)
                        throw new ArgumentException("The start address is not word aligned.", "startAddr");

                    //1. determine sector of start address
                    uint sector = InSector(startAddr);
                    if (!SWrittenTo[sector])
                        throw new InvalidOperationException("The sector of the start address has not loaded with data.");

                    //2. calculate the checksum
                    checksum = 0;
                    uint address = startAddr;
                    uint EndOfFlash = SectorAddress((uint) NumberOfSectors-1) + SectorSize((uint) NumberOfSectors-1) - 1;
                    //keep going until we either get to the end of the flash or to an empty sector.
                    while (SWrittenTo[sector] && address < EndOfFlash)
                    {
                        checksum += GetDataAtAddress(address);
                        address += 4;
                        if (address < EndOfFlash)
                            sector = InSector(address);                    
                    }
                    endAddr = address - 4;

                    //3. now do 2's complement of it
                    checksum = (~checksum) + 1;

                }

            
                #endregion
            }


            /// <summary>
            /// This handler implments the ISP protocol for LPC-based microcontrollers
            /// per datasheet of LPC2468
            /// </summary>
            public class LPC_ISP_Handler : IProtocolHandler
            {
                #region properties

                DateTime _LastSent;

                /// <summary>
                /// The last time the user used the protocol handler to successfully send data.
                /// 
                /// Note that periodic ping messages do not cause this to be updated.
                /// </summary>
                public DateTime LastSent
                {
                    get { return _LastSent; }
                }

                public ClientContext CommContext { get; set; }

                private static Random randObj = new Random(20);
                private LinkedList<EventNotify> EventCallbacks = new LinkedList<EventNotify>();
                private Object EventCallbacksSync = new Object();
                private bool bDisconnectEventHandled = false;
                StringBuilder DisconnectDatLock = new StringBuilder(randObj.Next().ToString());
                private AutoResetEvent NewMsgEvt = new AutoResetEvent(false);
                StringBuilder ReceiveDatLock = new StringBuilder(randObj.Next().ToString());
                StringBuilder ReceiveAPI_Lock = new StringBuilder(randObj.Next().ToString());
                MemoryStream ReceivedData = new MemoryStream();
                private ConcurrentQueue<ReceivedMsgLog> RxMsgs = new ConcurrentQueue<ReceivedMsgLog>();

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

                /// <summary>
                /// Force a clean up of the resources (not safe to use after this is called)
                /// </summary>
                public void Dispose()
                {
                    if (CommContext.bConnected)
                        try { CommContext.Close(); }
                        catch { }
                }

                /// <summary>
                /// This function enables/disables periodically sending STX ETX messages 
                /// to the remote device to ensure that the connection stays alive and that
                /// the peer is present.
                /// </summary>
                /// <param name="enable">Set to true to periodically send STX ETX messages to the peer</param>
                /// <param name="optionalMaxIdleTime">Maximum connection idle time before an STX ETX should be sent</param>
                public void PeriodicPing(bool enable, TimeSpan MaxIdleTime)
                {
                    throw new NotImplementedException();
                }


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

                #region send functions
                /// <summary>
                /// Send command (protocol-dependent characters are added here). 
                /// Blocks until protocol determines the message was received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if protocol supports it</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if the data was sent, otherwise false</returns>
                public bool SendCommand(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    bool bFoundAck = false;
                    //bool bGotNB_Safe = false;
                    try
                    {
                        _LastSent = DateTime.Now;
                        byte[] data_with_crlf = new byte[data.Length];
                        data.CopyTo(data_with_crlf, 0);
                        data_with_crlf[data.Length] = 0x0D;
                        data_with_crlf[data.Length + 1] = 0x0A;
                        string dat = System.Text.Encoding.Default.GetString(data);
                        CommContext.LogMsg(TraceEventType.Verbose, "ISP SENT: " + dat + "<CR><LF>");
                        CommContext.Write(data_with_crlf);
                    }
                    catch (Exception ex)
                    {
                        CommContext.LogMsg(TraceEventType.Error, "Caught an unexpected exception when sending the command: <STX>" + System.Text.Encoding.Default.GetString(data.ToArray()) + "<ETX>. optionalRetries: " + optionalRetries.ToString() + ". Exception: " + ex.ToString());
                    }
                    finally
                    {
                        /*if (bGotNB_Safe)
                            SendingContext.NB_Safe.Set();*/
                    }
                    return bFoundAck;
                }

                /// <summary>
                /// Send command (protocol-dependent characters are added here). 
                /// Blocks until protocol determines the message was received or connection fails.
                /// </summary>
                /// <param name="data">the data to send (can be null)</param>
                /// <param name="optionalRetries">Number of retries if protocol supports it</param>
                /// <param name="optionalRetryTime">Amount of time (in ms) between retries</param>
                /// <returns>True if an ACK was recieved, otherwise false</returns>
                public bool SendCommand(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES, int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    return SendCommand(System.Text.Encoding.ASCII.GetBytes(data), optionalRetries, optionalRetryTime);
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
                public IAsyncResult BeginSendCommand(byte[] data,
                                                    int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS,
                                                    AsyncCallback callback = null,
                                                    Object state = null)
                {
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                }

                /// <summary>
                /// Call to end asynchronous sending
                /// </summary>
                /// <param name="ar"></param>
                /// <returns></returns>
                public bool EndSendCommand(IAsyncResult ar)
                {
                    throw new NotImplementedException();
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
                public void SendCommandNB(byte[] data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                    int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    throw new NotImplementedException();
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
                public void SendCommandNB(string data, int optionalRetries = ProtocolDefaults.DEF_NUM_RETRIES,
                                                int optionalRetryTime = ProtocolDefaults.DEF_RETRY_TIMEOUT_MS)
                {
                    throw new NotImplementedException();
                }
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
                            /*try { SendingContext.Dispose(); }
                            catch { }*/
                        }
                    }
                }

                byte LastRxByte;
                protected virtual void ReceivedDataEvent(ClientReceivedDataEvent Revent)
                {
                    bool bEnd = false;
                    lock (ReceiveDatLock)
                    {
                        foreach (byte theByte in Revent.RcvDat)
                        {
                            if (bEnd)
                                break;
                            if ((theByte == 0x0A) && (LastRxByte == 0x0D))    //if we got a CRLF, we have a response packet
                            {
                                CommContext.LogMsg(TraceEventType.Verbose, "ISP RCVD: <STX>" + System.Text.Encoding.Default.GetString(ReceivedData.ToArray()) + "<ETX>");

                                //process the parsed command
                                ProcessResponse(ReceivedData);

                                ReceivedData = new MemoryStream();
                                LastRxByte = 0;
                            }
                            else
                            {
                                //if the CR was no followed by a line feed, add it back to the received data
                                if (LastRxByte == 0x0D)
                                    ReceivedData.WriteByte(LastRxByte);

                                //prevent CR from appearing in the received data (if followed by a LF)
                                if (theByte != 0x0D)
                                    ReceivedData.WriteByte(theByte);
                                LastRxByte = theByte;
                            }
                        }
                    }
                }

                /// <summary>
                /// Process the response received. Store in the receive log for future 
                /// retrieval.
                /// </summary>
                /// <param name="theCommand">The command received (STX ETX removed)</param>
                /// <returns></returns>
                protected bool ProcessResponse(MemoryStream theResponse)
                {
                    String rsp = System.Text.Encoding.Default.GetString(theResponse.ToArray());
                    //moved up one level to get this message before ACK: CommContext.LogMsg(TraceEventType.Verbose, "STXETX RCVD: <STX>" + cmd + "<ETX>") ;                                        
                    RxMsgs.Enqueue(new ReceivedMsgLog(rsp));
                    NewMsgEvt.Set();

                    PublishEvent(new ClientWroteDataEvent(theResponse.ToArray(), CommContext));
                    return true;
                }
                #endregion

            }

            /// <summary>
            /// This handler implements the ISP commands for LPC-based microcontrollers
            /// per datasheet of LPC2468
            /// </summary>
            public class LPC_ISP_CommandHandler : ICommandHandler
            {
                public enum ReturnCodes : int
                {
                    CMD_SUCCESS = 0,
                    INVALID_COMMAND = 1,
                    SRC_ADDR_ERROR = 2,
                    DST_ADDR_ERROR = 3,
                    SRC_ADDR_NOT_MAPPED = 4,
                    DST_ADDR_NOT_MAPPED = 5,
                    COUNT_ERROR = 6,
                    INVALID_SECTOR = 7,
                    SECTOR_NOT_BLANK = 8,
                    SECTOR_NOT_PREPARED_FOR_WRITE_OPERATION = 9,
                    COMPARE_ERROR = 10,
                    BUSY = 11,
                    PARAM_ERROR = 12,
                    ADDR_ERROR = 13,
                    ADDR_NOT_MAPPED = 14,
                    CMD_LOCKED = 15,
                    INVALID_CODE = 16,
                    INVALID_BAUD_RATE = 17,
                    INVALID_STOP_BIT = 18,
                    CODE_READ_PROTECTION_ENABLED = 19
                }

                public int LogID
                {
                    get;
                    set;
                }

                public TraceSource ts
                {
                    get;
                    set;
                }

                /// <summary>
                /// The default timeout for a response in seconds
                /// </summary>
                public int DefaultTimeout = 20;                

                private IProtocolHandler _ProtocolHandler;
                public IProtocolHandler ProtocolHandler
                {
                    get { return _ProtocolHandler; }
                }

                public void Dispose()
                {
                    try
                    {
                        _ProtocolHandler.Dispose();
                        _ProtocolHandler = null;
                    }
                    catch
                    {
                    }
                }

                /// <summary>
                /// Send a command
                /// </summary>
                /// <param name="command">The command to send</param>
                /// <param name="NumResponses">The number of replies required (as defined by protocol handler)</param>
                /// <param name="optionalCloseConn">Set to true if the connection should be closed when this function is done. Default: false</param>
                /// <param name="optionalRetries">Number of retries to get the command sent. Default: 3 (if supported by protocol handler)</param>
                /// <param name="optionalTimeout">Timeout (in seconds) when waiting for a response. Default: 10 seconds (if supported by protocol handler)</param>
                /// <returns></returns>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>
                public List<string> SendCommand(string command, int NumResponses = 0,
                                        bool optionalCloseConn = false, int optionalRetries = 3,
                                        int optionalTimeout = 10)
                {

                    List<string> Responses = new List<string>();

                    try
                    {
                        while (optionalRetries-- > 0)
                        {
                            if (_ProtocolHandler.CommContext.bConnected == false)
                                throw new UnresponsiveConnectionException("Connection has disconnected.", command);

                            //first clear out all data:
                            string dummy;
                            while (ProtocolHandler.ReceiveData(out dummy, 0)) ;

                            if (_ProtocolHandler.SendCommand(command))
                            {
                                string reply = null;
                                bool result = true;
                                int giveUp = NumResponses + 3;
                                while (result && Responses.Count < NumResponses && giveUp-- > 0)
                                {
                                    result = _ProtocolHandler.ReceiveData(out reply, optionalTimeout * 1000);
                                    if (reply != null)
                                    {
                                        //Check for and throw out the echo
                                        if (reply.Trim() == command.Trim()) //if we got the echo clear out the received data
                                            Responses = new List<string>();
                                        else
                                        {
                                            Responses.Add(reply);
                                        //Check to make sure that we received a valid status code
                                            if (Responses.Count == 1)   //this is the status code
                                            {
                                                int status_code = int.Parse(reply);
                                                if (status_code > (int)ReturnCodes.CODE_READ_PROTECTION_ENABLED)
                                                    throw new ResponseException("Unexpected status code found: " + status_code, command, reply);
                                                ReturnCodes rc = (ReturnCodes) status_code;
                                                if (rc > ReturnCodes.CMD_SUCCESS)
                                                    throw new ResponseErrorCodeException("Error status code found: " + rc.ToString(), command, reply); 
                                            }
                                        }
                                    }
                                }
                                //see if we found all the responses we were looking for
                                if (Responses.Count < NumResponses)
                                {
                                    throw new ResponseException(
                                        "Did not receive the desired number of responses: found "
                                        + Responses.Count.ToString() + " of " + NumResponses.ToString(),
                                        command, Responses);

                                }
                                break;
                            }                                                                                                                                                 
                            else if (optionalRetries < 1)
                            {
                                throw new UnresponsiveConnectionException(
                                    "Failed to send the data: connection is unresponsive.", command);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when trying to send or receive a command.", command, Responses, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }

                    finally
                    {
                        if (optionalCloseConn == true)
                            try { Dispose(); }
                            catch { }

                    }

                    return Responses;

                }

                /// <summary>
                /// Sends a command and returns the status code and fields
                /// </summary>
                /// <param name="command">the command to send</param>
                /// <param name="NumResponses">the expected number of responses</param>
                /// <param name="Timeout">How long (in seconds) to wait for the expected number of responses</param>
                /// <param name="responses">The responses received. The first response is always the status code</param>
                /// <returns>the status code</returns>
                public ReturnCodes SendCommand(string command, int NumResponses, int Timeout, out List<string> responses)
                {
                    try
                    {
                        responses = SendCommand(command, NumResponses, false, 0, Timeout);
                        try
                        {
                            return (ReturnCodes)int.Parse(responses[0].Trim());
                        }
                        catch (Exception ex)
                        {
                            throw new ResponseException("Unable to parse the status code.", command, responses, ex);
                        }
                    }
                    catch (ResponseErrorCodeException ex)
                    {
                        //there will only be one field and it will be the status code
                        try
                        {
                            responses = ex.ResponsesReceived;
                            return (ReturnCodes)int.Parse(ex.ResponsesReceived[0].Trim());
                        }
                        catch (Exception ex1)
                        {
                            throw new ResponseException("Unable to parse the status code.", command, ex.ResponsesReceived, ex1);
                        }
                    }
                }

                /// <summary>
                /// UUEncode and send the data. Follow the data with a checksum.
                /// If the device replies with OK, then the function is successful. 
                /// If the device replies with RESEND, the function resends the data 
                /// NumRetries times. If the device still replies with RESEND then
                /// ResponseErrorCodeException is thrown.
                /// </summary>
                /// <param name="data">The data to send</param>
                /// <param name="NumRetries">The number of times to retry</param>
                /// <param name="Timeout">The timeout to wait for OK/RESEND in seconds</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>
                protected void UUEncodeSendData(byte[] data, int NumRetries, int Timeout)
                {
                    string command = "";
                    string response = "";
                    try
                    {
                        //1. calculate checksum
                        uint checksum = 0;
                        foreach (byte by in data)
                            checksum += by;
                        while (NumRetries > 0)
                        {
                            //2. break the data up into 45 byte segments, UU encode, and send
                            int i = 0;
                            while (i < data.Length)
                            {
                                if (_ProtocolHandler.CommContext.bConnected == false)
                                    throw new UnresponsiveConnectionException("Connection has disconnected.", "");

                                //a. copy a line
                                int Length = 45;
                                if (Length > (data.Length - i))
                                    Length = data.Length - i;
                                byte[] dataToSend = new byte[Length];
                                Array.Copy(data, i, dataToSend, 0, Length);
                                i += Length;

                                //b. uu encode the line
                                command = Convert.ToBase64String(dataToSend);

                                //c. send the uu encoded line
                                ProtocolHandler.SendCommand(command);
                            }

                            //3. send the checksum
                            if (_ProtocolHandler.CommContext.bConnected == false)
                                throw new UnresponsiveConnectionException("Connection has disconnected.", "");

                            //a. clear out any data first
                            while (ProtocolHandler.ReceiveData(out response, 0)) ;

                            command = checksum.ToString();
                            ProtocolHandler.SendCommand(command);

                            //b. now wait for OK or RESEND
                            ProtocolHandler.ReceiveData(out response, Timeout * 1000);
                            if (response == "OK")
                            {
                                //success!
                                break;
                            }
                            else if (response == "RESEND")
                            {
                                //failure... send the bytes again
                                NumRetries--;
                            }
                            else
                            {
                                //unexpected response
                                throw new ResponseException("Unexpected response to data checksum.", checksum.ToString(), response);
                            }
                        }
                        if (NumRetries <= 0)                        
                            throw new ResponseErrorCodeException("Unable to get an OK response to sending the data checksum.", command, response);
                        
                    }
                    catch (Exception ex)
                    {
                        if (!IsStandardException(ex))
                            ex = new ResponseException("Error occurred when trying to send data.", command, response, ex);

                        LogMsg(TraceEventType.Warning, ex.ToString());
                        throw ex;
                    }
                }
            
                /// <summary>
                /// Accumulate and decode incoming UU data which is the result of a
                /// read operation. The checksum is verified:
                /// Reply with OK if the checksum matches(the function is successful)
                /// OR if the numnber of retries has been exceeded in which case
                /// ResponseErrorCodeException is thrown.
                /// Reply with RESEND, if the data must be resent.                  
                /// 
                /// </summary>
                /// <param name="length">The amount of data expected</param>
                /// <param name="NumRetries">The number of times to retry getting the data correctly</param>
                /// <param name="Timeout">The timeout to wait for the next packet of data in seconds</param>
                /// <param name="data">The decoded, received data</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>
                protected void AccumulateAndDecodeUUData(int length, int NumRetries, int Timeout, out byte[] data)
                {                    
                    string incomingdata;                                        
                    MemoryStream AllData = new MemoryStream();                    

                    while (AllData.Length < length)
                    {
                        int retriesLeft = NumRetries;
                        bool validData = false;
                        while (!validData)
                        {
                            MemoryStream ReceivedDataPacket = new MemoryStream();
                            int packetLines = 0;  //number of lines received for the current packet

                            //1. accumulate the data
                            while ((AllData.Length < length) && (packetLines < 20))
                            {
                                if (_ProtocolHandler.CommContext.bConnected == false)
                                    throw new UnresponsiveConnectionException("Connection has disconnected.", "");

                                //a. retrieve a line
                                ProtocolHandler.ReceiveData(out incomingdata, Timeout * 1000);

                                //b. UU decode the line
                                byte[] rawdata;
                                rawdata = Convert.FromBase64String(incomingdata.Trim());

                                //c. add it to our received data length
                                ReceivedDataPacket.Write(rawdata, 0, rawdata.Length);
                                packetLines++;
                            }

                            //2. retrieve the checksum
                            ProtocolHandler.ReceiveData(out incomingdata, Timeout * 1000);
                            uint ChecksumRcvd = Convert.ToUInt32(incomingdata.Trim());

                            //3. calculate the checksum and compare, save the data if valid
                            uint checksum = 0;
                            byte[] packetdata = ReceivedDataPacket.ToArray();
                            foreach (byte by in packetdata)
                                checksum += by;

                            if (checksum == ChecksumRcvd)
                            {
                                ProtocolHandler.SendCommand("OK");
                                AllData.Write(packetdata, 0, packetdata.Length);
                                validData = true;
                            }
                            else
                            {
                                ProtocolHandler.SendCommand("RESEND");
                                validData = false;
                                retriesLeft--;
                                if (retriesLeft <= 0)
                                    throw new ResponseErrorCodeException("Unable to successfully match the data checksum.", incomingdata, checksum.ToString());
                            }
                        }
                    }

                    data = AllData.ToArray();
                }


                /// <summary>
                /// This command is used to unlock Flash Write, Erase, and Go commands
                /// </summary>
                /// <param name="unlockCode"></param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                public void Unlock(uint unlockCode)
                {
                    string command = "U " + unlockCode;
                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to unlock the write, erase, and go commands: " + retCode, command, resps);
                }

                /// <summary>
                /// This command is used to change the baud rate. The new baud rate is effective
                /// after the command handler sends the CMD_SUCCESS return code.
                /// </summary>
                /// <param name="BaudRate">The baudrate of the serial port</param>
                /// <param name="StopBit">The number of stop bits (only 1 or 2 are acceptable)</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown when a StopBit other than 1 or 2 is specified</exception>
                public void SetBaudRate(int BaudRate, System.IO.Ports.StopBits StopBit)
                {
                    string command = "B " + BaudRate;
                    if (StopBit == System.IO.Ports.StopBits.One)
                        command = command + " 1";
                    else if (StopBit == System.IO.Ports.StopBits.Two)
                        command = command + " 2";
                    else 
                        throw new ArgumentException("Invalid number of stop bits. Only 1 and 2 are acceptable.", "StopBit");

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to set the baud rate: " + retCode, command, resps);
                }

                /// <summary>
                /// The default setting for echo command is ON. When ON the ISP command handler
                /// sends the received serial data back to the host.
                /// </summary>
                /// <param name="EchoOn">Whether or not echo should be on</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                public void Echo(bool EchoOn)
                {
                    string command = "A ";
                    if (EchoOn)
                        command = command + " 1";
                    else
                        command = command + " 0";                    

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to set the baud rate: " + retCode, command, resps);
                }

                /// <summary>
                /// This command is used to download data to RAM. Data will be UU-encoded
                /// before sending by the function. This command is blocked when code read 
                /// protection is enabled.
                /// </summary>
                /// <param name="address">RAM address where data bytes are to be written. This address should be a word boundary.</param>
                /// <param name="data">The binary data to send. The data can have a maximum length of 900 bytes and must be an integer number of words (data length must be divisible by 4).</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown when address is invalid or the data is an invalid length</exception>
                public void WriteToRAM(uint address, byte[] data)
                {
                    WriteRamCMD(address, data.Length);

                    //break data up into 45 byte segments and send:
                    UUEncodeSendData(data, 3, DefaultTimeout);
                }                

                /// <summary>
                /// This command is used to read data from RAM or Flash memory. This command is
                /// blocked when code read protection is enabled.
                /// </summary>
                /// <param name="address">Address from where data bytes are to be read. This address
                /// should be a word boundary.</param>
                /// <param name="length">Number of bytes to be read. Count should be a multiple of 4.</param>
                /// <param name="data">The data that was read.</param>
                public void ReadMemory(uint address, int length, out byte[] data)
                {
                    ReadRamCMD(address, length);

                    AccumulateAndDecodeUUData(length, 3, DefaultTimeout, out data);
                }

                /// <summary>
                /// This command must be executed before executing CopyRAMtoFlash or
                /// EraseSectors command. Successful execution of the CopyRAMtoFlash or
                /// EraseSectors command causes relevant sectors to be protected again. The
                /// boot block can not be prepared by this command. To prepare a single sector use
                /// the same "Start" and "End" sector numbers.
                /// </summary>
                /// <param name="startSector">The start sector number</param>
                /// <param name="StopBit">The end sector number</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown when the end sector number is not greater than or equal to the start sector number</exception>
                public void PrepareSectors(uint startSector, uint endSector)
                {                    
                    if (endSector < startSector)                         
                        throw new ArgumentException("End sector must be greater than or equal to the start sector.", "endSector");
                    
                    string command = "P " + startSector.ToString() + " " + endSector.ToString();

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to prepare sectors: " + retCode, command, resps);
                }


                /// <summary>
                /// This command is used to program the Flash memory. The PrepareSectors command 
                /// should precede this command. The affected sectors are automatically protected 
                /// again once the copy command is successfully executed. The boot block cannot 
                /// be written by this command. This command is blocked when code read protection 
                /// is enabled.
                /// </summary>
                /// <param name="flashAddress">Destination Flash address where data bytes are to be
                /// written. The destination address should be a 256 byte boundary.</param>
                /// <param name="ramAddress">Source RAM address from where data bytes are to be read. 
                /// Must be on a word boundary.</param>
                /// <param name="numberOfBytes">Number of bytes to be written. Should be 256 | 512 |
                /// 1024 | 4096.</param>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown one of the parameters breaks the described rules.</exception>
                public void CopyRAMtoFlash(uint flashAddress, uint ramAddress, int numberOfBytes)
                {
                    if (flashAddress % 256 != 0)
                        throw new ArgumentException("The flash address must be on a 256 byte boundary.", "flashAddress");
                    else if (ramAddress % 4 != 0)
                        throw new ArgumentException("The RAM address must be on a word boundary.", "ramAddress");
                    else if ( (numberOfBytes != 256) && (numberOfBytes != 512) && (numberOfBytes != 1024) && (numberOfBytes != 4096) )
                        throw new ArgumentException("The number of bytes must be either 256, 512, 1024, or 4096.", "numberOfBytes");

                    string command = "C " + flashAddress.ToString() + " " + ramAddress.ToString() + " " + numberOfBytes.ToString();

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to copy RAM to flash: " + retCode, command, resps);                    
                }

                /// <summary>
                /// This command is used to execute a program residing in RAM or Flash memory. It
                /// may not be possible to return to the ISP command handler once this command is
                /// successfully executed. This command is blocked when code read protection is
                /// enabled.
                /// </summary>
                /// <param name="address">Flash or RAM address from which the code execution is to 
                /// be started. This address should be on a word boundary.</param>
                /// <param name="mode">T (Execute program in Thumb Mode) | A (Execute program in ARM mode).</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown one of the parameters breaks the described rules.</exception>
                public void Go(uint address, char mode)
                {
                    if (address % 4 != 0)
                        throw new ArgumentException("The address must be on a word boundary.", "address");
                    else if ( (mode != 'T') && (mode != 'A') )
                        throw new ArgumentException("The mode must either be T for thumb mode or A for ARM mode.", "mode");

                    string command = "G " + address.ToString() + " " + mode + " ";

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to execute: " + retCode, command, resps);                    
                }

                /// <summary>
                /// This command is used to erase one or more sector(s) of on-chip Flash memory.
                /// The boot block can not be erased using this command. This command only allows
                /// erasure of all user sectors when the code read protection is enabled.
                /// </summary>
                /// <param name="startSector">The start sector.</param>
                /// <param name="endSector">The end sector. Should be greater than or equal to start sector number.</param>                
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown one of the parameters breaks the described rules.</exception>
                public void EraseSectors(uint startSector, uint endSector)
                {
                    if (endSector < startSector)
                        throw new ArgumentException("End sector must be greater than or equal to the start sector.", "endSector");

                    string command = "E " + startSector.ToString() + " " + endSector.ToString();

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to erase sectors: " + retCode, command, resps);
                }

                /// <summary>
                /// This command is used to blank check one or more sectors of on-chip Flash
                /// memory.
                /// Blank check on sector 0 always fails as first 64 bytes are re-mapped to Flash
                /// boot block.
                /// </summary>
                /// <param name="startSector">The start sector.</param>
                /// <param name="endSector">The end sector. Should be greater than or equal to start sector number.</param>     
                /// <param name="Blank">True if the sectors are blank, false if they are not.</param>
                /// <param name="offset">The offset of the first non blank word location (ignore if Blank was true).</param>     
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown one of the parameters breaks the described rules.</exception>
                public void BlankCheckSectors(uint startSector, uint endSector, out bool Blank, out uint offset)
                {
                    if (endSector < startSector)
                        throw new ArgumentException("End sector must be greater than or equal to the start sector.", "endSector");

                    string command = "I " + startSector.ToString() + " " + endSector.ToString();

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);

                    offset = 0;
                    Blank = true;

                    //interpret the response
                    if (retCode == ReturnCodes.SECTOR_NOT_BLANK)
                    {
                        Blank = false;

                        //The offset information is in the next response
                        string reply;
                        bool result = _ProtocolHandler.ReceiveData(out reply, DefaultTimeout * 1000);
                        if (reply == null)
                            throw new UnresponsiveConnectionException(
                                    "Failed to receive non-blank offset data: connection is unresponsive.", command);
                        else
                        {
                            resps.Add(reply);
                            try
                            {
                                offset = Convert.ToUInt32(reply);
                            }
                            catch (Exception ex)
                            {
                                throw new ResponseException("Unable to interpret the value received for the offset of first non-blank data.", command, resps, ex);
                            }
                        }
                    }
                    else if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to blank check sectors: " + retCode, command, resps);
                    
                }

                /// <summary>
                /// This command is used to read the part identification number.
                /// </summary>
                /// <param name="partID">The part identification number, as a string.</param>                 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                                
                public void ReadPartID(out string partID)
                {
                    string command = "J";

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 2, DefaultTimeout, out resps);

                    partID = "";

                    //interpret the response
                    if (retCode == ReturnCodes.CMD_SUCCESS)
                        partID = resps[1];
                    else
                        throw new ResponseErrorCodeException("Received an error code when trying to read the part ID: " + retCode, command, resps);
                }

                /// <summary>
                /// This command is used to read the boot code version number.
                /// </summary>
                /// <param name="version">The boot code version number, as a string.</param>                 
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>
                public void ReadBootCodeVersion(out string version)
                {
                    string command = "K";

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 2, DefaultTimeout, out resps);

                    version = "";

                    //interpret the response
                    if (retCode == ReturnCodes.CMD_SUCCESS)
                        version = resps[1];
                    else
                        throw new ResponseErrorCodeException("Received an error code when trying to read the boot code version: " + retCode, command, resps);
                }

                /// <summary>
                /// This command is used to compare the memory contents at two locations.
                /// Compare result may not be correct when source or destination address
                /// contains any of the first 64 bytes starting from address zero. First 64 bytes
                /// are re-mapped to Flash boot sector
                /// </summary>
                /// <param name="address1">Starting Flash or RAM address of data bytes to be compared.
                /// This address should be a word boundary.</param>
                /// <param name="address2">Starting Flash or RAM address of data bytes to be compared.
                /// This address should be a word boundary. </param>     
                /// <param name="length">Number of bytes to be compared; should be a multiple of 4.</param>
                /// <param name="Equal">True if the memory is equal, false if it is not.</param>
                /// <param name="offset">The offset of the first mismatch (ignore if Equal was true).</param>     
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the device</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the device responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the device to complete an operation</exception>                
                /// <exception cref="System.ArgumentException">Thrown one of the parameters breaks the described rules.</exception>
                public void Compare(uint address1, uint address2, uint length, out bool Equal, out uint offset)
                {
                    if (address1 % 4 != 0)
                        throw new ArgumentException("The addresses must be word aligned.", "address1");
                    if (address2 % 4 != 0)
                        throw new ArgumentException("The addresses must be word aligned.", "address2");
                    if (length % 4 != 0)
                        throw new ArgumentException("The number of bytes to compare must be a multiple of 4.", "length");

                    string command = "M " + address1.ToString() + " " + address2.ToString() + " " + length.ToString();

                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);

                    offset = 0;
                    Equal = true;

                    //interpret the response
                    if (retCode == ReturnCodes.COMPARE_ERROR)
                    {
                        Equal = false;

                        //The offset information is in the next response
                        string reply;
                        bool result = _ProtocolHandler.ReceiveData(out reply, DefaultTimeout * 1000);
                        if (reply == null)
                            throw new UnresponsiveConnectionException(
                                    "Failed to receive offset of first non-equal data: connection is unresponsive.", command);
                        else
                        {
                            resps.Add(reply);
                            try
                            {
                                offset = Convert.ToUInt32(reply);
                            }
                            catch (Exception ex)
                            {
                                throw new ResponseException("Unable to interpret the value received for the offset of the first non-equal data.", command, resps, ex);
                            }
                        }
                    }
                    else if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to compare data: " + retCode, command, resps);
                    
                }


                #region Helper functions

                protected void ReadRamCMD(uint address, int length)
                {
                    string command = "R ";
                    if (length > 900)
                        throw new ArgumentException("The data length exceeds 900 bytes.", "data");
                    if (length % 4 != 0)
                        throw new ArgumentException("The data length is invalid.", "data");
                    if (address % 4 != 0)
                        throw new ArgumentException("The address is invalid.", "address");

                    command = command + address + " " + length;
                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to issue a read RAM command: " + retCode, command, resps);
                }

                protected void WriteRamCMD(uint address, int length)
                {
                    string command = "W ";
                    if (length > 900)
                        throw new ArgumentException("The data length exceeds 900 bytes.", "data");
                    if (length % 4 != 0)
                        throw new ArgumentException("The data length is invalid.", "data");
                    if (address % 4 != 0)
                        throw new ArgumentException("The address is invalid.", "address");

                    command = command + address + " " + length;
                    List<string> resps;
                    ReturnCodes retCode = SendCommand(command, 1, DefaultTimeout, out resps);
                    if (retCode != ReturnCodes.CMD_SUCCESS)
                        throw new ResponseErrorCodeException("Received an error code when trying to issue a write RAM command: " + retCode, command, resps);
                }


                /// <summary>
                /// returns true if the exception is one of the default exceptions expected
                /// when sending message
                /// </summary>
                /// <param name="ex"></param>
                /// <returns></returns>
                protected static bool IsStandardException(Exception ex)
                {
                    return ((ex is ResponseException) || (ex is ResponseErrorCodeException) || (ex is UnresponsiveConnectionException));
                }

                protected void LogMsg(TraceEventType type, string msg)
                {
                    ts.TraceEvent(type, LogID, msg);
                }
                #endregion
            }
        }
    }
}
