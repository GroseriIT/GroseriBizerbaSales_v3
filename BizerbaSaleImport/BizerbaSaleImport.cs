using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BizerbaSaleImport
{
    public partial class BizerbaSaleImport : ServiceBase
    {
        private Timer timer;
        public BizerbaSaleImport()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log("GroseriBizerbaSaleImport_","Önceki loglar yedekleniyor.", "INFO");
            LogYedekle();

            var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
            string MagazaIp = "";
            string etikatKontrolDk = "";

            string TeraziDurumu = "Aktif";

            if (config.TeraziDurum.Count == 1)
            {
                foreach (var teraziDurum in config.TeraziDurum)
                {
                    TeraziDurumu = teraziDurum.Value;
                }
            }
            else
            {
                Logger.Log("GroseriBizerbaSaleImport_",$"Config.ini dosyasında Terazi durumu 1 defa belirtilmesi gerekmektedir! Servis Durduruluyor.", "ERROR");
                ServiceStop();
            }

            if (TeraziDurumu == "Aktif")
            {
                foreach (var magaza in config.Magaza)
                {
                    MagazaIp = magaza.Value;
                }
                foreach (var etikatKontrol in config.EtiketKontrolDk)
                {
                    etikatKontrolDk = etikatKontrol.Value;
                }

                foreach (var magaza in config.Magaza)
                {
                    MagazaIp = magaza.Value;

                    Logger.Log("GroseriBizerbaSaleImport_","Tablo ve Procedure Kontrol Ediliyor!");
                    DbTabloOlustur.DbTabloKontrolEt();
                    Logger.Log("GroseriBizerbaSaleImport_","30 Gün önceki kayıtlar siliniyor.");
                    DbYemizle();

                    try
                    {
                        Logger.Log("GroseriBizerbaSaleImport_","Bizerba Sale Import Servisi Başlatılıyor...");
                        timer = new Timer(Convert.ToInt32(etikatKontrolDk)*60000); // **(1 dk 60000 ms)**
                        timer.Elapsed += new ElapsedEventHandler(DosyaTransfer);
                        timer.Start();
                        Logger.Log("GroseriBizerbaSaleImport_","Bizerba Sale Import Servisi Başlatıldı.");
                        EventLog.WriteEntry("Bizerba Sale Import Servisi Başladı.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("GroseriBizerbaSaleImport_","Servis başlatılırken hata oluştu: " + ex.Message);
                        EventLog.WriteEntry("Servis başlatılırken hata oluştu: " + ex.Message, EventLogEntryType.Error);
                        ServiceStop();
                    }
                }
            }
            else
            {
                Logger.Log("GroseriBizerbaSaleImport_",$"Bizerba Pasif olduğu için Servis Çalıştırılmıyor!");
                EventLog.WriteEntry($"Bizerba Pasif olduğu için Servis Çalıştırılmıyor!");
                ServiceStop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    Logger.Log("GroseriBizerbaSaleImport_","Servisi Durduruldu.");
                    EventLog.WriteEntry("Servisi Durduruldu.");
                }
                else
                {
                    Logger.Log("GroseriBizerbaSaleImport_","Servis zaten durdurulmuş veya Timer hiç başlatılmamış.");
                    EventLog.WriteEntry("Servis zaten durdurulmuş veya Timer hiç başlatılmamış.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GroseriBizerbaSaleImport_", "Servisi durdurulurken hata oluştu: " + ex.Message, "WARNING");
                EventLog.WriteEntry("Servisi durdurulurken hata oluştu: " + ex.Message, EventLogEntryType.Error);
                try
                {
                    Environment.Exit(0); // Servisin kapanmasını %100 garantiye alır!
                }
                catch
                {
                    Logger.Log("GroseriBizerbaSaleImport_", "Servis zaten kapalı", "INFO");
                }
            }
        }

        public void ServiceStop()
        {
            using (var serviceController = new ServiceController("GroseriBizerbaSaleImport"))
            {
                serviceController.Stop();
            }
        }

        public void DosyaTransfer(object sender, ElapsedEventArgs e)
        {
            //Saat 8'den büyükse işlem yap
            if (DateTime.Now.Hour >= 8)
            {
                try
                {
                    var ftpSender = new TeraziDosyaGonder();
                    var ftpReader = new TeraziDosyaOku();
                    var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));

                    if (config.Magaza.Count == 1)
                    {
                        foreach (var magaza in config.Magaza)
                        {
                            string MagazaIp = magaza.Value;
                            ftpReader.MagazaIp = MagazaIp;
                        }
                    }
                    else
                    {
                        Logger.Log("GroseriBizerbaSaleImport_","Config.ini dosyasında Mağaza Ip Adresi 1 defa belirtilmesi gerekmektedir! Servis Durduruluyor.","ERROR");
                        ServiceStop();
                    }

                    foreach (var terazi in config.Teraziler)
                    {
                        string teraziAdi = terazi.Key;
                        string teraziIp = terazi.Value;

                        Random rastgele = new Random();
                        int sayi = rastgele.Next(99999, 1000000);

                        int ascii = rastgele.Next(65, 91);
                        char karakter = Convert.ToChar(ascii);

                        int ascii1 = rastgele.Next(65, 91);
                        char karakter1 = Convert.ToChar(ascii1);

                        string sablonDosya = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon.s00"); // Teraziye gönderilecek dosyanın yolu
                        string yeniDosyaAdi = $"{karakter}{sayi}{karakter1}.s00"; // Yeni adlandırma

                        Logger.Log("GroseriBizerbaSaleImport_",$"{teraziIp} - Teraziye Gönderiliyor: {sablonDosya} --> {teraziIp} | {yeniDosyaAdi}");
                        Console.WriteLine($"Teraziye Gönderiliyor: {sablonDosya} --> {teraziIp} | {yeniDosyaAdi}");
                        ftpSender.DosyaGonder(teraziIp, sablonDosya, yeniDosyaAdi);

                        Logger.Log("GroseriBizerbaSaleImport_",$"{teraziIp} Terazi Dosyaları Okunuyor");
                        Console.WriteLine($"{teraziIp} Terazi Dosyaları Okunuyor");
                        ftpReader.DosyalariOkuVeSil(teraziIp);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("GroseriBizerbaSaleImport_",$"Terazi Dosyaları Okunurken Hata Oluştu. Hata: " + ex.Message);
                    EventLog.WriteEntry("Hata: " + ex.Message);
                }
            }
        }

        public static void LogYedekle()
        {
            string logDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string backupDirectory = Path.Combine(logDirectory, "Yedek");
            string currentLogFilePath = Path.Combine(logDirectory, "GroseriBizerbaSaleImport_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            try
            {
                // Yedek klasörü yoksa oluştur
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Ana dizindeki tüm "service*.log" dosyalarını al
                string[] logFiles = Directory.GetFiles(logDirectory, "GroseriBizerbaSaleImport_*.log");

                foreach (string logFile in logFiles)
                {
                    string fileName = Path.GetFileName(logFile);

                    // Eğer bugünün log dosyası ise, yedekleme yapma
                    if (fileName == Path.GetFileName(currentLogFilePath))
                        continue;

                    // Hedef yedek dizini
                    string backupFilePath = Path.Combine(backupDirectory, fileName);

                    // Eğer aynı isimde dosya zaten yedekte varsa taşımaya gerek yok
                    if (!File.Exists(backupFilePath))
                    {
                        File.Move(logFile, backupFilePath);
                        Logger.Log("GroseriBizerbaSaleImport_",$"Log dosyası yedeklendi: {backupFilePath}");
                        Console.WriteLine($"Log dosyası yedeklendi: {backupFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GroseriBizerbaSaleImport_",$"Servis açılışında log yedekleme hatası: {ex.Message}");
                Console.WriteLine($"Servis açılışında log yedekleme hatası: {ex.Message}");
            }
        }

        private void DbYemizle()
        {
            var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));

            foreach (var magaza in config.Magaza)
            {
                string MagazaIp = magaza.Value;
                string connectionString = "Server=" + MagazaIp + ";Database=GRSDB01MGZ;User Id=sa;Password=saw;";

                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM TeraziSatis WHERE EtiketTarihi < DATEADD(DAY, -30, GETDATE())";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            int deletedRows = cmd.ExecuteNonQuery();
                            Console.WriteLine($"30 günden eski {deletedRows} kayıt silindi.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eski etiketleri silerken hata oluştu: {ex.Message}");
                }
            }
        }
    }
}
