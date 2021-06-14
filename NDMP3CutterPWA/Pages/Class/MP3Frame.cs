using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace NDMP3CutterPWA.Pages.Class
{
    public class MP3Frame
    {
        //Vị trí Byte đầu tiên của 1 frame
        public int StartBytePosition = 0;
        //Vị trí Byte cuối cùng của 1 frame
        public int EndBytePosition = 0;
        //Chiều dài chuỗi frame hiện tại
        public int FrameLenght;
        //Chuỗi byte chứa dữ liệu 1 frame bao gồm header
        //private byte[] frameByteArray;
        //MP3 Header
        public MP3FrameHeader Header;
        //Frame per second
        public double FPS;
        //Milisecond per Frame
        public double MilisecondPerFrame;

        /// <summary>
        /// Khởi tạo đối tượng là Frame từ thông tin 4 byte Header đầu vào
        /// </summary>
        /// <param name="HeaderByteArray">Chuỗi 4 byte chứa thông tin Header của Frame cần lấy</param>
        public MP3Frame(byte[] HeaderByteArray, int StartBytePositionInFile)
        {
            //Gán vị trí bắt đầu của chuỗi Frame trong file
            StartBytePosition = StartBytePositionInFile;
            //Khởi tạo Mp3 Header
            Header = new MP3FrameHeader(HeaderByteArray);
            //Gán chiều dài Frame
            FrameLenght = Header.MP3FrameLenght;
            //Gán vị trí kết thúc chuỗi Frame trong file
            EndBytePosition = StartBytePosition + FrameLenght - 1;
            //Tính Frame per second
            FPS = (double)Header.Frequency / 1152;
            //Tính số mili giây trên mỗi Frame
            MilisecondPerFrame = 1000 / FPS;
        }
        /// <summary>
        /// Tổng thời gian của file MP3 tính bằng Mili giây
        /// </summary>
        /// <param name="firstFrame">Frame đầu tiên của file MP3</param>
        /// <param name="endFrame">Frame cuối cùng của file MP3</param>
        /// <returns>Milisecond</returns>
        static public double GetMP3TotalTime(MP3Frame firstFrame, MP3Frame endFrame)
        {
            int totalByte = endFrame.EndBytePosition - firstFrame.StartBytePosition + 1;
            return (double)totalByte / endFrame.FrameLenght * endFrame.MilisecondPerFrame;
        }
        /// <summary>
        /// Tìm Frame quanh vị trí thời gian cho vào (Milisecond)
        /// </summary>
        /// <param name="mStream">Memory Stream</param>
        /// <param name="firstFrame">Frame đầu tiên trong file MP3</param>
        /// <param name="endFrame">Frame cuối cùng trong file MP3</param>
        /// <param name="AtTime">Vị trí thời gian (Milisecond) cần tìm Frame</param>
        /// <returns>Frame gần vị trí cần tìm, Nếu không tìm thấy thì trả về Frame đầu tiên</returns>
        static public async Task<MP3Frame> GetFrameByTimeAsync(MemoryStream mStream, MP3Frame firstFrame, MP3Frame endFrame, int AtTime)
        {
            byte[] fourByte = new byte[4];
            double TotalTime = GetMP3TotalTime(firstFrame, endFrame);
            //Vị trí Byte bắt đầu tìm Frame
            int TempPosition = (int)Math.Round(AtTime / endFrame.MilisecondPerFrame * endFrame.FrameLenght,
                0,
                MidpointRounding.ToNegativeInfinity);
            //Lần lượt tăng giảm 1 đơn vị để tìm Frame nằm quanh vị trí cần tìm, nếu không tìm thấy thì trả về Frame đầu tiên
            for (int i = 0; i < endFrame.FrameLenght; i++)
            {
                // +i
                // Nếu byte đang tìm đến cuối file thì trả về Frame cuối
                if (TempPosition + i >= endFrame.StartBytePosition)
                {
                    return endFrame;
                }
                mStream.Position = TempPosition + i;
                _ = await mStream.ReadAsync(fourByte, 0, 4);
                if (MP3FrameHeader.ByteArrayIsHeader(fourByte))
                {
                    return new MP3Frame(fourByte, TempPosition + i);
                }
                // -i
                // Nếu byte đang tìm đến đầu file thì trả về Frame đầu
                if (TempPosition - i <= firstFrame.EndBytePosition)
                {
                    return firstFrame;
                }
                mStream.Position = TempPosition - i;
                _ = await mStream.ReadAsync(fourByte, 0, 4);
                if (MP3FrameHeader.ByteArrayIsHeader(fourByte))
                {
                    return new MP3Frame(fourByte, TempPosition - i);
                }

            }
            //Nếu không tìm thấy thì trả về Frame đầu tiên
            return firstFrame;
        }
    }
    
}
