using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDMP3CutterPWA.Pages.Class;

namespace NDMP3CutterPWA.Pages
{
    public partial class Index
    {
        //chuỗi mã tạo file định dạng bytesBase64
        private string bytesBase64 = string.Empty;
        //Ẩn hiện nút Download
        private bool aDownloadHide = true;

        //memoryStream
        private MemoryStream mStream;
        //ID3tag Header
        private ID3tagV2 iD3Header = new ID3tagV2();
        //ID3tag Footer
        private ID3tagV2 iD3Footer = new ID3tagV2();
        //First Frame Header bytes array
        private byte[] bFirstFrameHeader;
        //First MP3 Frame
        private MP3Frame firstFrame;
        //End MP3 Frame
        private MP3Frame endFrame;
        
        //Tổng thời gian của Mp3 file (Milisecond)
        private double TotalTime = 0;
        //CutStartTime - Thời gian bắt đầu đoạn cắt (Milisecond)
        private double CutStartTime;
        //Thời gian kết thúc đoạn cắt (Milisecond)
        private double CutEndTime;
        //Tin thông báo có thể cắt MP3 hay không
        private string CutMessage = string.Empty;
        //Tên file đã cắt
        private string CutFileName = string.Empty;


        private async Task ClickCutAsync()
        {
            //Tổng thời gian = 0 thì báo file không hợp lệ
            if (TotalTime == 0)
            {
                CutMessage = "File không hợp lệ";
            }
            //Thời gian bắt đầu phải lớn hơn hoặc bằng 0
            if (CutStartTime < 0)
            {
                CutMessage = "Thời gian bắt đầu phải lớn hơn hoặc bằng 0";
                return;
            }
            //Thời gian kết thúc phải nhỏ hơn tổng thời gian của file
            if (CutEndTime > TotalTime)
            {
                CutMessage = "Thời gian kết thúc phải nhỏ hơn tổng thời gian của file";
                return;
            }
            //Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc
            if (CutEndTime <= CutStartTime)
            {
                CutMessage = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu";
                return;
            }
            //Frame bắt đầu cắt
            MP3Frame CutStartFrame = await MP3Frame.GetFrameByTimeAsync(mStream, firstFrame, endFrame, (int)CutStartTime);
            //Frame kết thúc cắt
            MP3Frame CutEndFrame = await MP3Frame.GetFrameByTimeAsync(mStream, firstFrame, endFrame, (int)CutEndTime);
            CutMessage = "Start frame at: " + CutStartFrame.StartBytePosition.ToString()
                + " - End frame at: " + CutEndFrame.StartBytePosition.ToString()
                + " (Byte Position)";

            //Chuỗi Byte đọc từ Stream gốc vào Stream đã cắt
            byte[] ByteReadToWrite = new byte[iD3Header.ID3Size + iD3Footer.ID3Size
                + CutEndFrame.EndBytePosition - CutStartFrame.StartBytePosition + 1];

            //Đọc và ghi ID3 tags
            mStream.Position = 0;
            _ = await mStream.ReadAsync(ByteReadToWrite, 0, ByteReadToWrite.Length);

            //Đọc ghi MP3 Frame
            mStream.Position = CutStartFrame.StartBytePosition;
            _ = await mStream.ReadAsync(ByteReadToWrite,
                iD3Header.ID3Size + iD3Footer.ID3Size,
                CutEndFrame.EndBytePosition - CutStartFrame.StartBytePosition + 1);

            //Chuyển sang chuỗi Base64
            bytesBase64 = await ByteArrayToBase64(ByteReadToWrite);
            

            //Hiện nút Download
            aDownloadHide = false;

        }
        private Task<string> ByteArrayToBase64(byte[] ByteArray)
        {
            return Task.FromResult(Convert.ToBase64String(ByteArray));
        }

        private Dictionary<IBrowserFile, string> loadedFiles =
            new Dictionary<IBrowserFile, string>();
        private long maxFileSize = 1024 * 1024 * 50;
        private int maxAllowedFiles = 1;
        
        private bool isLoading;
        string exceptionMessage;

        async Task LoadFiles(InputFileChangeEventArgs e)
        {
            isLoading = true;
            loadedFiles.Clear();
            exceptionMessage = string.Empty;

            try
            {
                foreach (var file in e.GetMultipleFiles(maxAllowedFiles))
                {
                    using var stream = file.OpenReadStream(maxFileSize);
                    
                    //Khởi tạo mới mStream
                    mStream = new MemoryStream();

                    //Copy file stream > memory stream
                    await stream.CopyToAsync(mStream);
                    mStream.Position = 0;

                    //Tạo mới Bytes để đọc được
                    byte[] bytesRead = new byte[mStream.Length];

                    //Đọc dữ liệu file dạng byte từ memoryStream
                    _ = await mStream.ReadAsync(bytesRead, 0, (int)mStream.Length);

                    //Đọc dữ liệu ID3 tag Header từ memoryStream
                    mStream.Position = 0;
                    byte[] byteTemp = new byte[10];
                    _ = await mStream.ReadAsync(byteTemp, 0, 10);
                    iD3Header.ID3tagByteArray = byteTemp;

                    //Đọc dữ liệu ID3 tag Footer từ memoryStream
                    mStream.Position = iD3Header.ID3Size;
                    byteTemp = new byte[10];
                    _ = await mStream.ReadAsync(byteTemp, 0, 10);
                    iD3Footer.ID3tagByteArray = byteTemp;


                    //Đọc dữ liệu First frame của MP3 file
                    mStream.Position = iD3Header.ID3Size + iD3Footer.ID3Size;
                    bFirstFrameHeader = new byte[4];
                    _ = await mStream.ReadAsync(bFirstFrameHeader, 0, 4);
                    //Khởi tạo Frame Header đầu tiên
                    if (MP3FrameHeader.ByteArrayIsHeader(bFirstFrameHeader))
                    {
                        firstFrame = new MP3Frame(bFirstFrameHeader, iD3Header.ID3Size + iD3Footer.ID3Size);
                    }
                    
                    //Đọc dữ liệu End frame của MP3 file
                    int tempPosition = (int)mStream.Length - firstFrame.FrameLenght + 1;
                    byte[] FourByteArray = new byte[4];
                    bool isContinue = true;
                    while (isContinue)
                    {
                        mStream.Position = tempPosition;
                        _ = await mStream.ReadAsync(FourByteArray, 0, 4);
                        if (MP3FrameHeader.ByteArrayIsHeader(FourByteArray))
                        {
                            endFrame = new MP3Frame(FourByteArray, tempPosition);
                            isContinue = false;
                        }
                        if (tempPosition == firstFrame.StartBytePosition)
                        {
                            isContinue = false;
                        }
                        tempPosition -= 1;
                    }

                    //Tính tổng thời gian của Mp3 file (milisecond)
                    TotalTime = MP3Frame.GetMP3TotalTime(firstFrame, endFrame);

                    //Tạo tên file đã cắt
                    CutFileName = "Cut_" + file.Name;


                    //Bỏ dòng này
                    //loadedFiles.Add(file, await reader.ReadToEndAsync());
                    //Thay bằng dòng dưới
                    byteTemp = new byte[mStream.Length < 1024 * 10 ? mStream.Length : 1024 * 10];
                    for (int i = 0; i < byteTemp.Length; i++)
                    {
                        byteTemp[i] = bytesRead[i];
                    }
                    loadedFiles.Add(file, BitConverter.ToString(byteTemp));
                }
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            isLoading = false;
        }
        
    }
}
