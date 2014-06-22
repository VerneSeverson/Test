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

                private IProtocolHandler _ProtocolHandler;
                public IProtocolHandler ProtocolHandler
                {
                    get { return _ProtocolHandler; }
                }

                /// <summary>
                /// Send a command
                /// </summary>
                /// <param name="command">The command to send</param>
                /// <param name="NumResponses">The number of replies required (as defined by protocol handler)</param>
                /// <param name="optionalCloseConn">Set to true if the connection should be closed when this function is done. Default: false</param>
                /// <param name="optionalRetries">Number of retries to get the command sent. Default: 3 (if supported by protocol handler)</param>
                /// <param name="optionalTimeout">Timeout (in seconds) when waiting for an STXETX response. Default: 10 seconds (if supported by protocol handler)</param>
                /// <returns></returns>
                /// <exception cref="CommandHandlers.ResponseException">Thrown when an invalid or unexpected response is received from the server</exception>
                /// <exception cref="CommandHandlers.ResponseErrorCodeException">Thrown when the server responds with an error code</exception>
                /// <exception cref="CommandHandlers.UnresponsiveConnectionException">Thrown when a timeout occurs waiting for the connection to the server to complete an operation</exception>
                public List<string> SendCommand(string command, int NumResponses = 0,
                                        bool optionalCloseConn = false, int optionalRetries = 3,
                                        int optionalTimeout = 10)
                {
                    List<string> resps = new List<string>();
                    string resp;
                 //   ProtocolHandler.ReceiveData(
                    return resps;
                }
            }
        }
    }
}
