using System;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using GroseriServices;
using System.Xml.Linq;
using System.Collections;
using System.Globalization;

namespace BizerbaSaleControl
{
    public static class MailPrepare
    {
        public static void SendMail(string MagazaIp)
        {
            string localConnectionString = "Server="+ MagazaIp + ";Database=GRSDB01MGZ;User Id=sa;Password=saw;";
            
            StringBuilder mailBody = new StringBuilder();
            mailBody.AppendLine(@"
            <html>
            <head>
                <style>
                    body {
                        font-family: Calibri, sans-serif;
                        font-size: 11px;
                        color: #333;
                    }
                    h3 {
                        font-size: 13px;
                        font-weight: bold;
                    }
                    table {
                        border-collapse: collapse;
                        width: 100%;
                        font-size: 11px;
                    }
                    th, td {
                        border: 1px solid #ddd;
                        padding: 8px;
                        text-align: left;
                    }
                    th {
                        background-color: #f2f2f2;
                        font-weight: bold;
                    }
                    tr:nth-child(even) {
                        background-color: #f9f9f9;
                    }
                </style>
            </head>
            <body>");

            int recordCount = 0;
            // Şu anki zamanı al
            DateTime now = DateTime.Now;
            // Günün bitiş zamanı (23:59:59)
            DateTime endOfDay = now.Date.AddDays(1).AddSeconds(-1);
            // Kalan süreyi hesapla
            TimeSpan remainingTime = endOfDay - now;
            // Kalan süreyi dakika olarak al
            int remainingMinutes = (int)remainingTime.TotalMinutes;
            string islemYapilacakView = "GRSDB01MGZ.dbo.vw_EtiketDurumRapor";
            //Eğer gün bitmek üzere ve son rapor ise
            if (remainingMinutes <= 30)
            {
                islemYapilacakView = "vw_GunlukEtiketDurumRapor";
                mailBody.AppendLine($"<h3>{DateTime.Now.Date.ToString("dd.MM.yyyy")} tarihinde terazilerden alınmış ve satışı tamamlanmamış, satış bekleniyor durumda olan etiketler aşağıda belirtildiği gibidir.</h3>");
            }
            else
            {
                mailBody.AppendLine("<h3>Etiketleme üzerinden 10 dakikayı aşmış ve satışı tamamlanmamış, satış bekleniyor durumda olan etiketler aşağıda belirtildiği gibidir.</h3>");
            }
            mailBody.AppendLine("<table>");
            mailBody.AppendLine("<tr><th>Terminal IP</th><th>PLU</th><th>Ürün Adı</th><th>Miktar (KG)</th><th>Fiyat</th><th>Oluşturulma Tarihi</th></tr>");

            using (SqlConnection conn = new SqlConnection(localConnectionString))
            {
                conn.Open();

                string query = $@"SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED Select * From {islemYapilacakView} Order By EtiketTarihi ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal rawAmount = reader.GetDecimal(3);
                        decimal rawPrice = reader.GetDecimal(4);

                        string teraziIp = reader.GetString(0);
                        string plu = reader.GetString(1);
                        string stockName = reader.GetString(2);
                        string amountKg = rawAmount.ToString("N2", new CultureInfo("tr-TR"));
                        string price = rawPrice.ToString("N2", new CultureInfo("tr-TR"));
                        DateTime createDate = reader.GetDateTime(5);

                        mailBody.AppendLine($"<tr><td>{teraziIp}</td><td>{plu}</td><td>{stockName}</td><td>{amountKg}</td><td>{price}</td><td>{createDate}</td></tr>");
                        recordCount++;
                    }
                }
            }
            mailBody.AppendLine("</table>");
            mailBody.AppendLine("</body></html>");
            Logger.Log("GroseriBizerbaSaleControl_", $"{recordCount} kayıt var.", "INFO");

            if (recordCount > 0)
            {
                SendEmail(mailBody.ToString());
                Logger.Log("GroseriBizerbaSaleControl_",$"{recordCount} kayıt için mail gönderildi.", "INFO");
                Console.WriteLine($"[✓] {recordCount} kayıt için mail gönderildi.");
            }
            else
            {
                Logger.Log("GroseriBizerbaSaleControl_","Gönderilecek etiket bulunamadı.", "ERROR");
                Console.WriteLine("[✓] Gönderilecek etiket bulunamadı.");
            }
        }

        private static void SendEmail(string body)
        {
            Logger.Log("GroseriBizerbaSaleControl_", $"Send Mail'e geldik", "INFO");

            string subject = $"[Terazi Etiket Kontrol] - {DateTime.Now:dd.MM.yyyy HH:mm}";
            string mailTo = ",";
            string mailCc = "";
            var config = new AyarOku(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));

            foreach (var eposta in config.EPosta)
            {
                mailTo += $",{eposta.Value}";
            }

            MailSender mailSender = new MailSender();
            try
            {
                mailSender.SendEmail(subject, body.ToString(), mailTo.Replace(",,", "").Replace(" ", ""), mailCc);
            }
            catch
            {
                Logger.Log("GroseriBizerbaSaleControl_", $"Mail gönderimi yapılırken hata oluştu.", "ERROR");
            }
        }
    }
}
