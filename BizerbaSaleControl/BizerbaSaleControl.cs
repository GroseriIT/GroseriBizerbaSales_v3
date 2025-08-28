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

namespace BizerbaSaleControl
{
    public partial class BizerbaSaleControl : ServiceBase
    {

        private Timer timer;

        public BizerbaSaleControl()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
            string MagazaIp = "";
            string BizerbaRaporGonderimDk = "";
            string TeraziDurumu = "Aktif";

            foreach (var teraziDurum in config.TeraziDurum)
            {
                TeraziDurumu = teraziDurum.Value;
            }

            if (TeraziDurumu == "Aktif")
            {
                foreach (var magaza in config.Magaza)
                {
                    MagazaIp = magaza.Value;
                }
                foreach (var rprGonderimDk in config.BizerbaRaporGonderimDk)
                {
                    BizerbaRaporGonderimDk = rprGonderimDk.Value;
                }

                try
                {
                    LogYedekle();
                    Logger.Log("GroseriBizerbaSaleControl_","Bizerba Sale Control Servisi Başlatılıyor...","INFO");
                    timer = new Timer(Convert.ToInt32(BizerbaRaporGonderimDk)*60000); // **(1 dk 60000 ms)**
                    timer.Elapsed += new ElapsedEventHandler(Task);
                    timer.Start();
                    Logger.Log("GroseriBizerbaSaleControl_","Bizerba Sale Control Servisi Başlatıldı.","INFO");
                    EventLog.WriteEntry("Bizerba Sale Control Servisi Başlatıldı.");
                }
                catch (Exception ex)
                {
                    Logger.Log("GroseriBizerbaSaleControl_","Servis başlatılırken hata oluştu: " + ex.Message, "ERROR");
                    EventLog.WriteEntry("Servis başlatılırken hata oluştu: " + ex.Message, EventLogEntryType.Error);
                    ServiceStop();
                }
            }
            else
            {
                Logger.Log("GroseriBizerbaSaleControl_",$"Bizerba Pasif olduğu için Servis Çalıştırılmıyor!", "ERROR");
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
                    Logger.Log("GroseriBizerbaSaleControl_","Servisi Durduruldu.", "ERROR");
                    EventLog.WriteEntry("Servisi Durduruldu.");
                }
                else
                {
                    Logger.Log("GroseriBizerbaSaleControl_","Servis zaten durdurulmuş veya Timer hiç başlatılmamış.", "ERROR");
                    EventLog.WriteEntry("Servis zaten durdurulmuş veya Timer hiç başlatılmamış.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GroseriBizerbaSaleControl_", "Servisi durdurulurken hata oluştu: " + ex.Message, "WARNING");
                EventLog.WriteEntry("Servisi durdurulurken hata oluştu: " + ex.Message, EventLogEntryType.Error);
                try
                {
                    Environment.Exit(0); // Servisin kapanmasını %100 garantiye alır!
                }
                catch
                {
                    Logger.Log("GroseriBizerbaSaleControl_", "Servis zaten kapalı", "INFO");
                }
            }
        }

        public void ServiceStop()
        {
            using (var serviceController = new ServiceController("GroseriBizerbaSaleControl"))
            {
                serviceController.Stop();
            }
        }

        public void Task(object sender, ElapsedEventArgs e)
        {
            //Saat 8'den büyükse işlem yap
            if (DateTime.Now.Hour >= 8)
            {
                try
                {
                    var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
                    var saleControl = new SaleControl();
                    var transferMerkez = new TransferMerkez();
                    string MagazaIp = "";

                    if (config.Magaza.Count == 1)
                    {
                        foreach (var magaza in config.Magaza)
                        {
                            MagazaIp = magaza.Value;
                        }
                    }
                    else
                    {
                        Logger.Log("GroseriBizerbaSaleControl_","Config.ini dosyasında Mağaza Ip Adresi 1 defa belirtilmesi gerekmektedir! Servis Durduruluyor.", "ERROR");
                        ServiceStop();
                    }

                    Logger.Log("GroseriBizerbaSaleControl_","Satışlar Kontrol Ediliyor.", "INFO");
                    saleControl.SaleControlRecords();

                    Logger.Log("GroseriBizerbaSaleControl_","Satışlar Merkeze Yazılıyor.", "INFO");
                    transferMerkez.TransferUpdatedRecords();

                    Logger.Log("GroseriBizerbaSaleControl_","Satışlar EPosta Gönderiliyor.", "INFO");
                    MailPrepare.SendMail(MagazaIp);
                }
                catch
                {

                }
            }
        }

        public static void LogYedekle()
        {
            string logDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string backupDirectory = Path.Combine(logDirectory, "Yedek");
            string currentLogFilePath = Path.Combine(logDirectory, "GroseriBizerbaSaleControl_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            try
            {
                // Yedek klasörü yoksa oluştur
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Ana dizindeki tüm "service*.log" dosyalarını al
                string[] logFiles = Directory.GetFiles(logDirectory, "GroseriBizerbaSaleControl_*.log");

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
                        Logger.Log("GroseriBizerbaSaleControl_",$"Log dosyası yedeklendi: {backupFilePath}","INFO");
                        Console.WriteLine($"Log dosyası yedeklendi: {backupFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GroseriBizerbaSaleControl_",$"Servis açılışında log yedekleme hatası: {ex.Message}","ERROR");
                Console.WriteLine($"Servis açılışında log yedekleme hatası: {ex.Message}");
            }
        }
    }
}