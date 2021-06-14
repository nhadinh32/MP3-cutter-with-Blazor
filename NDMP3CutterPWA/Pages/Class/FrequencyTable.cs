using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NDMP3CutterPWA.Pages.Class
{
    public class FrequencyTable
    {
        private static readonly int[,] Table = new int[3, 4]
        {
            //Chuỗi bit 0, 0 đến 1, 1 tương ứng 4 giá trị Frequency theo từng Version
            {44100,48000,32000,0}, //MPEG version 1
            {22050,24000,16000,0}, //MPEG version 2
            {11025,12000,8000,0} //MPEG version 2.5
        };
        static public int GetFrequency(MP3FrameHeader.Version Ver, byte[] BitHeaderArray)
        {
            //Nếu MPEG Version là reserved hoặc Error thì trả về kết quả 0
            if (GetMPEGVersionIndex(Ver) > 2)
            {
                return 0;
            }
            //Nếu MPEG Version thỏa mãn thì tiếp tục tính kết quả
            byte[] FrequencyBitArray = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                FrequencyBitArray[i] = BitHeaderArray[i + 20];
            }
            return Table[GetMPEGVersionIndex(Ver), BitsArrayToInt(FrequencyBitArray)];
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
        /// Lấy giá trị Index của MPEG Version tương ứng với vị trí trong bảng Table
        /// </summary>
        /// <param name="Ver"></param>
        /// <returns>Ver 1 trả về 0; Ver 2 trả về 1; Ver 2.5 trả về 2; còn lại trả về 3-5</returns>
        static private int GetMPEGVersionIndex(MP3FrameHeader.Version Ver)
        {
            return Ver switch
            {
                MP3FrameHeader.Version.MPEG_Version_2_5 => 2,
                MP3FrameHeader.Version.reserved => 3,
                MP3FrameHeader.Version.MPEG_Version_2 => 1,
                MP3FrameHeader.Version.MPEG_Version_1 => 0,
                MP3FrameHeader.Version.Error => 4,
                _ => 5,
            };
        }
    }
}
