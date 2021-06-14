using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NDMP3CutterPWA.Pages.Class
{
    public class ID3tagV2
    {
        //Boolean Kiểm tra chuỗi đầu vào có phải ID3 Header/ Footer hay không
        public bool isID3tag;
        //Hàm kiểm tra chuỗi đầu vào có phải ID3 Header/ Footer hay không
        private bool getIsID3tag()
        {
            if ((iD3tagByteArray[0] == 73) && (iD3tagByteArray[1] == 68) && (iD3tagByteArray[2] == 51))
                return true;
            else
                return false;
        }
        //Kích thước của ID3 tag trong file MP3 tính theo Bit.
        public int ID3Size;
        //Hàm tính giá trị của ID3 tag theo số byte
        private int getID3Size()
        {
            //Nếu chuỗi đầu vào là ID3 thì tiếp tục tính. Nếu false thì trả về giá trị 0
            if (!getIsID3tag())
            {
                return 0;
            }
            //Đếm số byte có nghĩa trong chuỗi Size Bytes
            byte c = 0;
            byte[] bSize = getSizeBytes();
            for (int i =0; i < 4; i++)
            {
                c = (byte)(c + ((bSize[i] == 0) ? 0 : 1));
            }
            //Tạo chuỗi bit của ID3 size
            Boolean[] bitID3Size = new Boolean[32];
            for (int i = 0; i < 32; i++)
                bitID3Size[i] = false;
            //Lấy chuỗi bit của ID2 size, bỏ bit thứ 7 của mỗi byte bSize
            for (int i = 0; i < 28; i++)
            {
                if ((i>=0)&&(i<7))
                {
                    bitID3Size[i] = (bSize[3] & (int)Math.Pow(2, i)) == (int)Math.Pow(2, i) ? true : false;
                }
                else if ((i >= 7) && (i < 14))
                {
                    bitID3Size[i] = (bSize[2] & (int)Math.Pow(2, i-7)) == (int)Math.Pow(2, i-7) ? true : false;
                }
                else if ((i >= 14) && (i < 21))
                {
                    bitID3Size[i] = (bSize[1] & (int)Math.Pow(2, i-14)) == (int)Math.Pow(2, i-14) ? true : false;
                }
                else if ((i >= 21) && (i < 28))
                {
                    bitID3Size[i] = (bSize[0] & (int)Math.Pow(2, i-21)) == (int)Math.Pow(2, i-21) ? true : false;
                }
            }
            //Gán giá trị 0 cho chuỗi bSize
            bSize = new byte[4] { 0, 0, 0, 0 };
            //Lấy chuỗi bit BitID3size gán vào chuỗi byte bSize
            for (int i = 31; i >= 0; i--)
            {
                if ((i >= 0) && (i < 8))
                {
                    bSize[0] <<= 1;
                    bSize[0] += bitID3Size[i] ? 1 : 0;
                }
                else if ((i >= 8) && (i < 16))
                {
                    bSize[1] <<= 1;
                    bSize[1] += bitID3Size[i] ? 1 : 0;
                }
                else if ((i >= 16) && (i < 24))
                {
                    bSize[2] <<= 1;
                    bSize[2] += bitID3Size[i] ? 1 : 0;
                }
                else if ((i >= 24) && (i < 32))
                {
                    bSize[3] <<= 1;
                    bSize[3] += bitID3Size[i] ? 1 : 0;
                }
            }
            //Trả về KQ đổi bytearray bSize sang int, +10 byte của ID3 Header
            return BitConverter.ToInt32(bSize, 0) + 10;
        }
        //Chuỗi bytes của ID3 Header, 10 bytes đầu.
        private byte[] iD3tagByteArray = new byte[10];
        public byte[] ID3tagByteArray
        {
            set
            {
                iD3tagByteArray = value;
                isID3tag = getIsID3tag();
                ID3Size = getID3Size();
            }
            get
            {
                return iD3tagByteArray;
            }
        }
        //Lấy chuỗi byte xác định chiều dài ID3 tag.
        public byte[] getSizeBytes()
        {
            byte[] tempBytes = new byte[4];
            for (int i = 0; i<4; i++)
            {
                tempBytes[i] = ID3tagByteArray[i + 6];
            }
            return tempBytes;
        }
    }
}
