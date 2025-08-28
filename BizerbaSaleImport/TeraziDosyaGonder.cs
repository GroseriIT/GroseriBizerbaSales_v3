using System;
using System.IO;
using System.Net;

public class TeraziDosyaGonder
{
    private string ftpUser = "bizuser";
    private string ftpPassword = "bizerba";

    public void DosyaGonder(string teraziIp, string localFilePath, string remoteFileName)
    {
        try
        {
            string ftpPath = $"ftp://{teraziIp}/bizerba/edv/in/{remoteFileName}";

            if (!File.Exists(localFilePath))
            {
                Logger.Log("GroseriBizerbaSaleImport_",$"Dosya bulunamadı: {localFilePath}");
                Console.WriteLine($"Dosya bulunamadı: {localFilePath}");
                return;
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
            request.UseBinary = true;
            request.KeepAlive = false;
            request.Timeout = 5000; // 5 saniye zaman aşımı

            byte[] fileContents = File.ReadAllBytes(localFilePath);
            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Logger.Log("GroseriBizerbaSaleImport_",$"Dosya gönderildi: {remoteFileName} - {response.StatusDescription}");
                Console.WriteLine($"Dosya gönderildi: {remoteFileName} - {response.StatusDescription}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log("GroseriBizerbaSaleImport_",$"FTP Dosya gönderme hatası ({teraziIp}): {ex.Message}");
            Console.WriteLine($"FTP Dosya gönderme hatası ({teraziIp}): {ex.Message}");
        }
    }
}
