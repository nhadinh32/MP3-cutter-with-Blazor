using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NDMP3CutterPWA.Pages.Class
{
    public class BitrateTable
    {
        private static readonly int[,] Table = new int[5, 16]
        {
            //Chuỗi bit 0,0,0,0 đến 1,1,1,1 tương ứng 16 giá trị Bitrate theo từng Version và Level
            {0,32,64,96,128,160,192,224,256,288,320,352,384,416,448,0}, //Version 1, Level 1
            {0,32,48,56,64,80,96,112,128,160,192,224,256,320,384,0}, //Version 1, Level 2
            {0,32,40,48,56,64,80,96,112,128,160,192,224,256,320,0}, //Version 1, Level 3
            {0,32,48,56,64,80,96,112,128,144,160,176,192,224,256,0}, // Version 2, Level 1
            {0,8,16,24,32,40,48,56,64,80,96,112,128,144,160,0} // Version 2, Level 2 & Level 3
        };
        /// <summary>
        /// Bitrate tương ứng với số phiên bản Version và lớp Level
        /// </summary>
        public enum VersionAndLevel
        {
            V1L1,
            V1L2,
            V1L3,
            V2L1,
            V2L2L3,
            Error
        }
        /// <summary>
        /// Chuyển chuỗi bitsArray sang giá trị Int, tính từ "Phải sang trái (phần tử cuối đến phần tử đầu)"
        /// </summary>
        /// <param name="bitsArray">Chuỗi bitsArray cần chuyển sang Int32</param>
        /// <returns>Giá trị Int32 của chuỗi bitsArray</returns>
        static public int BitsArrayToInt(byte[] bitsArray)
        {
            int stop = (bitsArray.Length == 32) ? 1 : 0;
            int resultInt = 0;
            //Tính từ phải sang trái (từ phần tử cuối đến phần tử đầu)
            for (int i = bitsArray.Length - 1; i >= stop; i--)
            {
                resultInt += bitsArray[i] * (int)Math.Pow(2, bitsArray.Length - 1 - i);
            }
            return resultInt;
        }
        /// <summary>
        /// Lấy giá trị Bitrate của MP3 Header
        /// </summary>
        /// <param name="Ver"></param>
        /// <param name="Lay"></param>
        /// <param name="BitHeaderArray"></param>
        /// <returns>Trả về giá trị Bitrate của chuỗi MP3 Header</returns>
        static public int GetBitrate(MP3FrameHeader.Version Ver, MP3FrameHeader.Layer Lay, byte[] BitHeaderArray)
        {
            VersionAndLevel VaL = GetVersionAndLevel(Ver, Lay);
            //Nếu VaL (VersionAndLevel) có giá trị là Error (5) thì trả về kết quả 0
            if ((int)VaL == 5)
            {
                return 0;
            }
            byte[] BitrateBitsArray = new byte[4];
            //Lấy chuỗi Bit từ 17 đến 20
            for (int i = 0; i < 4; i++)
            {
                BitrateBitsArray[i] = BitHeaderArray[i + 16];
            }
            return Table[(int)VaL, BitsArrayToInt(BitrateBitsArray)];
        }
        /// <summary>
        /// Xác định enum VersionAndLevel từ enum Version và enum Level
        /// </summary>
        /// <param name="Ver"></param>
        /// <param name="Lay"></param>
        /// <returns>Trả về giá trị num VersionAndLevel</returns>
        static public VersionAndLevel GetVersionAndLevel(MP3FrameHeader.Version Ver, MP3FrameHeader.Layer Lay)
        {
            if ((Ver==MP3FrameHeader.Version.MPEG_Version_1) && (Lay==MP3FrameHeader.Layer.Layer_1))
            {
                return VersionAndLevel.V1L1;
            }
            else if ((Ver == MP3FrameHeader.Version.MPEG_Version_1 )&& (Lay == MP3FrameHeader.Layer.Layer_2))
            {
                return VersionAndLevel.V1L2;
            }
            else if ((Ver == MP3FrameHeader.Version.MPEG_Version_1 )&& (Lay == MP3FrameHeader.Layer.Layer_3))
            {
                return VersionAndLevel.V1L3;
            }
            else if (((Ver == MP3FrameHeader.Version.MPEG_Version_2) || (Ver == MP3FrameHeader.Version.MPEG_Version_2_5))
                && (Lay == MP3FrameHeader.Layer.Layer_1))
            {
                return VersionAndLevel.V2L1;
            }
            else if (((Ver == MP3FrameHeader.Version.MPEG_Version_2) || (Ver == MP3FrameHeader.Version.MPEG_Version_2_5))
                && ((Lay == MP3FrameHeader.Layer.Layer_2)||(Lay==MP3FrameHeader.Layer.Layer_3)))
            {
                return VersionAndLevel.V2L2L3;
            }
            else
            {
                return VersionAndLevel.Error;
            }

        }
    }
}
