using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NDMP3CutterPWA.Pages.Class
{
    public class MP3FrameHeader
    {
        //Chuỗi byte chứa MP3 Header
        public byte[] byteHeaderArray = new byte[4];
        //Chuỗi byte chứa MP3 Header với frame có Padding
        public byte[] byteHeaderArrayWithPadding;
        //Chuỗi byte chứa MP3 Header với frame không có Padding
        public byte[] byteHeaderArrayWithoutPadding;
        //Chuỗi bit chứa MP3 Header, tính từ trái sang phải của chuỗi Bit
        private byte[] bitHeaderArray = new byte[32];
        //Chuỗi chứa MPEG version, 10 = Version 2; 11 = Version 1:
        public Version MPEGVersion;
        //Enum chứa các loại MPEG Version
        public enum Version
        {
            MPEG_Version_2_5,
            reserved,
            MPEG_Version_2,
            MPEG_Version_1,
            Error
        }
        //Chuỗi chứa MPEG layer, 01 = Layer 3; 10 = Layer 2; 11 = Layer 1:
        public Layer MPEGLayer;
        //Enum chứa các loại MPEG Layer
        public enum Layer
        {
            reserved,
            Layer_3,
            Layer_2,
            Layer_1,
            Error
        }
        //Chuỗi có được bảo vệ hay không, Protected by CRC (16bit CRC follows header)
        public bool isProtectionBit = false;
        //Giá trị Bitrate của file MP3
        public int Bitrate = 0;
        //Giá trị Tần số lấy mẫu file, frequency
        public int Frequency = 0;
        //Giá trị chứa Padding Bit
        public byte isPadding = 0;

        //MP3 frame lenght
        public int MP3FrameLenght = 0;
        
        public MP3FrameHeader(byte[] headerBytesArray)
        {
            if (headerBytesArray.Length == 4)
            {
                //Gán chuỗi Bytes cho byteHeaderArray
                byteHeaderArray = headerBytesArray;

                //Chuyển chuỗi Byte thành chuỗi Bit
                bitHeaderArray = ByteToBitArray(byteHeaderArray);

                //Lấy thông tin phiên bản
                MPEGVersion = GetMPEGVersion(bitHeaderArray);
                //Lấy thông tin Layer
                MPEGLayer = GetMPEGLayer(bitHeaderArray);
                //Lấy thông tin CRC Protection
                isProtectionBit = (bitHeaderArray[15] == 0) ? true : false;
                //Lấy thông tin Bitrate
                Bitrate = BitrateTable.GetBitrate(MPEGVersion, MPEGLayer, bitHeaderArray);
                //Lấy thông tin Frequency
                Frequency = FrequencyTable.GetFrequency(MPEGVersion, bitHeaderArray);
                //Lấy thông tin Padding
                isPadding = bitHeaderArray[22];

                //Tính chuỗi ByteHeaderArray nếu có Padding và không có Padding
                if (isPadding == 1)
                {
                    //Lấy chuỗi có Padding
                    byteHeaderArrayWithPadding = byteHeaderArray;
                    //Lấy chuỗi không có Padding
                    byte[] tempBitArray = new byte[32];
                    for (int i = 0; i < 32; i++)
                    {
                        tempBitArray[i] = bitHeaderArray[i];
                    }
                    tempBitArray[22] = 0;
                    byteHeaderArrayWithoutPadding = BitToByteArray(tempBitArray);
                }
                else
                {
                    //Lấy chuỗi không có Padding
                    byteHeaderArrayWithoutPadding = byteHeaderArray;
                    //Lấy chuỗi có Padding
                    byte[] tempBitArray = new byte[32];
                    for (int i = 0; i < 32; i++)
                    {
                        tempBitArray[i] = bitHeaderArray[i];
                    }
                    tempBitArray[22] = 1;
                    byteHeaderArrayWithPadding = BitToByteArray(tempBitArray);
                }

                //Tính kích thước chuỗi MP3 Frame tương ứng với Header hiện hoạt
                MP3FrameLenght = (int)Math.Round((144 * Bitrate * 1000.0 / Frequency), 0, MidpointRounding.ToNegativeInfinity) + isPadding;
            }

        }
        /// <summary>
        /// Tạo chuỗi các Byte từ chuỗi các Bit được đưa vào
        /// </summary>
        /// <param name="BitArray">Chuỗi Bit đầu vào</param>
        /// <returns>Chuỗi Byte từ chuỗi Bit</returns>
        static public byte[] BitToByteArray(byte[] BitArray)
        {
            int ByteCount = (int)Math.Round(BitArray.Length / 8.0, 0, MidpointRounding.ToPositiveInfinity);
            byte[] resultArray = new byte[ByteCount];
            
            
            for (int i = 0; i < ByteCount; i++)
            {
                //Gán giá trị 0 cho tất cả các Byte ban đầu
                resultArray[i] = 0;
                for (int j = 0; j < 8; j++)
                {
                    resultArray[i] <<= 1;
                    resultArray[i] += BitArray[j + 8 * i];
                }
            }

            return resultArray;
        }
        /// <summary>
        /// Chuyển chuỗi ByteArray sang BitArray để nhận diện các thuộc tính của MP3 Header
        /// </summary>
        /// <param name="ByteArray"></param>
        /// <returns>Trả về chuỗi 32 Bit của MP3 Header</returns>
        static public byte[] ByteToBitArray(byte[] ByteArray)
        {
            //Dấu & để so sánh chuỗi bit
            byte[] returnArray = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                if ((i >= 0) && (i < 8))
                {
                    returnArray[i] = (ByteArray[0] & (int)Math.Pow(2, 7 - i)) == (int)Math.Pow(2, 7 - i) ? 1 : 0;
                }
                else if ((i >= 8) && (i < 16))
                {
                    returnArray[i] = (ByteArray[1] & (int)Math.Pow(2, 7 - (i - 8))) == (int)Math.Pow(2, 7 - (i - 8)) ? 1 : 0;
                }
                else if ((i >= 16) && (i < 24))
                {
                    returnArray[i] = (ByteArray[2] & (int)Math.Pow(2, 7 - (i - 16))) == (int)Math.Pow(2, 7 - (i - 16)) ? 1 : 0;
                }
                else if ((i >= 24) && (i < 32))
                {
                    returnArray[i] = (ByteArray[3] & (int)Math.Pow(2, 7 - (i - 24))) == (int)Math.Pow(2, 7 - (i - 24)) ? 1 : 0;
                }
            }

            return returnArray;
        }
        /// <summary>
        /// Lấy thông tin phiên bản của MP3 Frame Header
        /// </summary>
        /// <param name="bitArray"></param>
        /// <returns>MPEG Version</returns>
        static private Version GetMPEGVersion(byte[] bitArray)
        {
            if ((bitArray[11] == 0) &&(bitArray[12] == 0))
            {
                return Version.MPEG_Version_2_5;
            }
            else if ((bitArray[11] == 0) && (bitArray[12] == 1))
            {
                return Version.reserved;
            }
            else if ((bitArray[11] == 1) && (bitArray[12] == 0))
            {
                return Version.MPEG_Version_2;
            }
            else if ((bitArray[11] == 1) && (bitArray[12] == 1))
            {
                return Version.MPEG_Version_1;
            }
            else
            {
                return Version.Error;
            }
        }
        /// <summary>
        /// Lấy thông tin Layer của MPE frame header
        /// </summary>
        /// <param name="bitArray"></param>
        /// <returns>Trả về kết quả Layer của MP3 Frame</returns>
        static private Layer GetMPEGLayer(byte[] bitArray)
        {
            if ((bitArray[13] == 0) && (bitArray[14] == 0))
            {
                return Layer.reserved;
            }
            else if ((bitArray[13] == 0) && (bitArray[14] == 1))
            {
                return Layer.Layer_3;
            }
            else if ((bitArray[13] == 1) && (bitArray[14] == 0))
            {
                return Layer.Layer_2;
            }
            else if ((bitArray[13] == 1) && (bitArray[14] == 1))
            {
                return Layer.Layer_1;
            }
            else
            {
                return Layer.Error;
            }
        }
        /// <summary>
        /// Tính chiều dài 1 Frame trong file MP3 với MP3 Header hiện tại
        /// </summary>
        /// <param name="HeaderByteArray">Chuỗi MP3 Header đầu vào, gồm 4 Byte</param>
        /// <returns>Chiều dài của chuỗi Frame</returns>
        static public int GetFrameLenght(byte[] HeaderByteArray)
        {
            //Nếu chuỗi Header Byte đầu vào có số phần tử khác 4 thì lỗi, trả về 0
            if (HeaderByteArray.Length != 4)
            {
                return 0;
            }
            MP3FrameHeader mP3FrameHeader = new MP3FrameHeader(HeaderByteArray);
            return mP3FrameHeader.MP3FrameLenght;
        }
        /// <summary>
        /// Kiểm tra chuỗi Byte có phải MP3 Header hay không
        /// </summary>
        /// <param name="ByteArray">Chuỗi Byte cần kiểm tra</param>
        /// <returns>Nếu hợp lệ thì trả về True</returns>
        static public bool ByteArrayIsHeader(byte[] ByteArray)
        {
            //Nếu Chuỗi khác 4 phần tử thì trả về False
            if (ByteArray.Length != 4)
                return false;
            MP3FrameHeader mP3FrameHeader = new MP3FrameHeader(ByteArray);

            for (int i = 0; i < 11; i++)
            {
                if (mP3FrameHeader.bitHeaderArray[i] != 1) return false;
            }
            if ((mP3FrameHeader.MPEGVersion == Version.Error) || (mP3FrameHeader.MPEGVersion == Version.reserved))
                return false;
            if ((mP3FrameHeader.MPEGLayer == Layer.Error) || (mP3FrameHeader.MPEGLayer == Layer.reserved))
                return false;
            if (mP3FrameHeader.Bitrate == 0)
                return false;
            if (mP3FrameHeader.Frequency == 0)
                return false;
            //Nếu hợp lệ thì trả về kết quả True
            return true;
        }
    }
}
