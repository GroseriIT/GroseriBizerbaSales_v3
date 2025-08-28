using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;

public class TeraziDosyaOku
{
    private string ftpUser = "bizuser";
    private string ftpPassword = "bizerba";
    public string MagazaIp;

    ServerConn serverConn = new ServerConn();
    public void DosyalariOkuVeSil(string teraziIp)
    {
        string ftpPath = $"ftp://{teraziIp}/bizerba/edv/out/";
        
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath);
        request.Method = WebRequestMethods.Ftp.ListDirectory;
        request.Credentials = new NetworkCredential(ftpUser, ftpPassword);

        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        {
            string fileName;
            while ((fileName = reader.ReadLine()) != null)
            {
                Console.WriteLine($"{teraziIp} - {fileName} Dosya Okunuyor.");
                if (fileName.EndsWith(".q00") || fileName.EndsWith(".q01") || fileName.EndsWith(".q02") || fileName.EndsWith(".q03"))
                {
                    string downloadPath = $"ftp://{teraziIp}/bizerba/edv/out/{fileName}";
                    Console.WriteLine($"{teraziIp} - {fileName} Dosya İşleniyor.");

                    // FTP’den dosya aç ve direkt oku
                    FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(downloadPath);
                    downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    downloadRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);

                    using (FtpWebResponse downloadResponse = (FtpWebResponse)downloadRequest.GetResponse())
                    using (Stream responseStream = downloadResponse.GetResponseStream())
                    using (MemoryStream memoryStream = new MemoryStream()) // **Bellekte tutmak için**
                    {
                        responseStream.CopyTo(memoryStream); // **FTP’den gelen dosyayı RAM'e al**
                        memoryStream.Position = 0; // **Akışın başına geri dön**

                        // **Dosya DB'ye işleniyor**
                        using (StreamReader ftpReader = new StreamReader(memoryStream, Encoding.UTF8, true, 1024, true)) // **Akışı kapatma**
                        {
                            DosyaKaydet(ftpReader, fileName, teraziIp);
                        }

                        // **Lokal Yedek Klasörüne Kaydet**
                        string backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Yedek");

                        if (!Directory.Exists(backupDirectory))
                        {
                            Directory.CreateDirectory(backupDirectory); // **Klasör yoksa oluştur**
                        }

                        string localBackupPath = Path.Combine(backupDirectory, fileName);

                        memoryStream.Position = 0; // **Akışın başına geri dön**

                        using (FileStream fileStream = new FileStream(localBackupPath, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.CopyTo(fileStream); // **Bellekteki veriyi dosyaya yaz**
                        }

                        Console.WriteLine($"Yedekleme tamamlandı: {localBackupPath}");
                    }


                    // Dosya FTP'den siliniyor
                    FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(downloadPath);
                    deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                    deleteRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                    deleteRequest.GetResponse().Close();

                    Logger.Log("GroseriBizerbaSaleImport_",$"Dosya işlendi ve silindi: {fileName}");
                    Console.WriteLine($"Dosya işlendi ve silindi: {fileName}");
                }
            }
        }

        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        {
            Logger.Log("GroseriBizerbaSaleImport_",$"Bağlantı Durumu : {response.StatusDescription}");
            Console.WriteLine($"Bağlantı Durumu : {response.StatusDescription}");
        }
    }

    private void DosyaKaydet(StreamReader ftpReader, string fileName, string teraziIp)
    {
        using (SqlConnection conn = new SqlConnection(serverConn.grsDbMagazaConn()))
        {
            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();
            string kntPlu = "";
            string kntAmount = "";
            try
            {
                string line;
                Console.WriteLine($"{teraziIp} - {fileName} Dosyası işleniyor.");
                while ((line = ftpReader.ReadLine()) != null)
                {
                    try
                    {
                        if (line.Contains("STYP2")) // Sadece satış satırlarını işle
                        {
                            string plu, Terminal;
                            decimal barkod, price, amountKg = 0;
                            DateTime createDate;

                            // PLU Kodunu Al (SNR1)
                            int startIndex = line.IndexOf("SNR1") + 4;
                            int endIndex = line.IndexOf("", startIndex);
                            plu = line.Substring(startIndex, endIndex - startIndex);
                            line = line.Replace("SNR1" + plu, "");

                            // Ağırlık Bilgisi (GEW1)
                            try
                            {
                                startIndex = line.IndexOf("GEW1") + 4;
                                endIndex = line.IndexOf("", startIndex);
                                string miktar = line.Substring(startIndex, endIndex - startIndex).Replace("GEW1", "");
                                amountKg = Convert.ToDecimal(miktar) / 1000;
                            }
                            catch
                            {
                                amountKg = 0;
                            }

                            if (amountKg > 0)
                            {
                                try
                                {
                                    // Etiket Fiyatı (BT2)
                                    startIndex = line.IndexOf("BT2") + 3;
                                    endIndex = line.IndexOf("", startIndex);
                                    string etTut = line.Substring(startIndex, endIndex - startIndex).Replace("BT2", "");
                                    price = Convert.ToDecimal(etTut) / 100;
                                }
                                catch
                                {
                                    // Satış Fiyatı (BT1)
                                    startIndex = line.IndexOf("BT1") + 3;
                                    endIndex = line.IndexOf("", startIndex);
                                    string etTut = line.Substring(startIndex, endIndex - startIndex).Replace("BT1", "");
                                    price = (Convert.ToDecimal(etTut) / 100) * amountKg;
                                }

                                // Tarih (ZEIS)
                                startIndex = line.IndexOf("ZEIS") + 4;
                                endIndex = line.IndexOf("", startIndex);
                                string olTarih = line.Substring(startIndex, endIndex - startIndex).Replace("ZEIS", "");
                                long olTarihUnix = Convert.ToInt32(olTarih);
                                createDate = DateTimeOffset.FromUnixTimeSeconds(olTarihUnix).DateTime.ToLocalTime();

                                // Terminal Numarası (WANU)
                                startIndex = line.IndexOf("WANU") + 4;
                                endIndex = line.IndexOf("", startIndex);
                                string terminal = line.Substring(startIndex, endIndex - startIndex).Replace("WANU", "");
                                Terminal = fileName.Substring(0, 2) + terminal;

                                // Barkod oluşturma
                                if (plu.Length == 2)// Barkodu Kontrol Et, Düzenle
                                {
                                    barkod = Convert.ToDecimal("29000" + plu);//PLU 2 haneli ise başına 29000 ekleyecek
                                }
                                else if (plu.Length == 3)
                                {
                                    barkod = Convert.ToDecimal("2900" + plu);//PLU 3 haneli ise başına 2900 ekleyecek
                                }
                                else if (plu.Length == 4)
                                {
                                    barkod = Convert.ToDecimal("290" + plu);//PLU 4 haneli ise başına 290 ekleyecek
                                }
                                else
                                {
                                    barkod = Convert.ToDecimal(plu);
                                }
                                string newID = Guid.NewGuid().ToString();

                                if ((barkod.ToString() != kntPlu.ToString()) || (barkod.ToString() == kntPlu.ToString() && amountKg.ToString() != kntAmount.ToString()))
                                { 
                                    try
                                    {
                                        // **DB’ye Kaydet**
                                        string sql = @"
                                        INSERT INTO TeraziSatis (TSatisID, TeraziIp, EtiketTarihi, Barkod, Miktar, Tutar, FK_DurumKodu)
                                        VALUES (@ID, @TerminalIp, @CreateDate, @Plu, @AmountKG, @Price, @Status)";

                                        using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@ID", newID);
                                            cmd.Parameters.AddWithValue("@TerminalIp", teraziIp);
                                            cmd.Parameters.AddWithValue("@CreateDate", createDate.AddHours(-3));
                                            cmd.Parameters.AddWithValue("@Plu", barkod);
                                            cmd.Parameters.AddWithValue("@AmountKG", amountKg);
                                            cmd.Parameters.AddWithValue("@Price", price);
                                            cmd.Parameters.AddWithValue("@Status", 0);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Log("GroseriBizerbaSaleImport_",$"Etiket insert edilirken hata oluştu. Hata : {ex.Message}");
                                    }
                                } 

                                kntPlu = barkod.ToString();
                                kntAmount = amountKg.ToString();
                            }
                        }
                    }
                    catch
                    {
                        Logger.Log("GroseriBizerbaSaleImport_",$"Etiket yazma hatası ({fileName}): Satir={line}");
                        Console.WriteLine($"Etiket yazma hatası ({fileName}): Satir={line}");
                    }
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Log("GroseriBizerbaSaleImport_",$"MSSQL yazma hatası ({fileName}): {ex.Message}");
                Console.WriteLine($"MSSQL yazma hatası ({fileName}): {ex.Message}");
            }
        }
        MukerrerTemizle(MagazaIp);
    }
    public void MukerrerTemizle(string MagazaIp)
    {
        string localConnectionString = "Server=" + MagazaIp + ";Database=GRSDB01MGZ;User Id=sa;Password=saw;";

        try
        {
            using (SqlConnection conn = new SqlConnection(localConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_MukerrerKayitTemizle", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                    Logger.Log("GroseriBizerbaSaleImport_",$"Mükerrer Kayıtlar Silindi - {DateTime.Now}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("GroseriBizerbaSaleImport_",$"Mükerrer Kayıtlar Silme procedure çalıştırma hatası: {ex.Message}");
        }
    }
}
