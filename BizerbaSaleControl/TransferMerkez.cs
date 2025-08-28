using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BizerbaSaleControl
{
    public class TransferMerkez
    {
        ServerConn serverConn = new ServerConn();

        public void TransferUpdatedRecords()
        {
            if (!CheckMerkez.CheckMerkezConnection())
            {
                Logger.Log("GroseriBizerbaSaleControl_",$"Merkez bağlantısı yok, bekleniyor...", "ERROR");
                Console.WriteLine("[X] Merkez bağlantısı yok, bekleniyor...");
                return;
            }

            List<(string id, string actionType, string terminalIp, DateTime createDate, string plu, decimal amountKg, decimal price, int status, Int64 tsId, DateTime IslemTarihi,string IslemYapan, Int64 MagazaID)> records =
                new List<(string, string, string, DateTime, string, decimal, decimal, int, Int64,DateTime,string,Int64)>();

            // **1️⃣ - Güncellenen Kayıtları Belleğe Al (Reader Kullan)**
            using (SqlConnection localConn = new SqlConnection(serverConn.grsDbMagazaConn()))
            {
                localConn.Open();
                string fetchUpdatedRecords = @"
                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
                SELECT log.TSatisID, log.AksiyonTipi, t.TeraziIp, t.EtiketTarihi, t.Barkod, t.Miktar, t.Tutar, t.FK_DurumKodu, ISNULL(t.FK_TransactionSale,0) TsId, ISNULL(IslemTarihi,t.EtiketTarihi) IslemTarihi, ISNULL(IslemYapan,'') IslemYapan, s.FK_STORE MagazaID
                FROM TeraziSatis t
                    INNER JOIN TeraziSatis_Log log ON log.TSatisID = t.TSatisID
                    INNER JOIN (Select top 1 FK_STORE From Genius3.GENIUS3.TRANSACTION_HEADER WHERE CREATE_DATE >= CAST(GETDATE() as date)) s on 1 = 1
                Order By log.AksiyonTipi,t.EtiketTarihi ASC";

                using (SqlCommand fetchCmd = new SqlCommand(fetchUpdatedRecords, localConn))
                using (SqlDataReader reader = fetchCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetDateTime(3),
                            reader.GetString(4),
                            reader.GetDecimal(5),
                            reader.GetDecimal(6),
                            reader.GetInt32(7),
                            reader.GetInt64(8),
                            reader.GetDateTime(9),
                            reader.GetString(10),
                            reader.GetInt64(11)
                        ));
                    }
                }
            } // **Reader ve Bağlantı Kapanıyor**

            // **2️ - Reader Kapanınca Yeni Bağlantılar Aç (Güncelleme ve Silme İşlemleri)**
            using (SqlConnection merkezConn = new SqlConnection(serverConn.grsDbConn()))
            using (SqlConnection localConn = new SqlConnection(serverConn.grsDbMagazaConn()))
            {
                merkezConn.Open();
                localConn.Open();
                SqlTransaction merkezTransaction = merkezConn.BeginTransaction();

                try
                {
                    foreach (var record in records)
                    {
                        if (record.actionType == "U")
                        {
                            // **Önce Update Deniyoruz**
                            string updateQuery = @"
                            UPDATE biz.TeraziSatis 
                            SET TeraziIp = @TerminalIp, EtiketTarihi = @CreateDate, Barkod = @Plu, Miktar = @AmountKG, 
                                Fiyat = @Price, FK_DurumKodu = @Status, FK_TransactionSale = @TsId, IslemTarihi = @IslemTarihi, IslemYapan=@IslemYapan,
                                FK_Mekan = (Select MekanID From Mekan Where PosID = @MagazaID)
                            WHERE TSatisID = @ID";
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, merkezConn, merkezTransaction))
                            {
                                updateCmd.Parameters.AddWithValue("@ID", record.id);
                                updateCmd.Parameters.AddWithValue("@TerminalIp", record.terminalIp);
                                updateCmd.Parameters.AddWithValue("@CreateDate", record.createDate);
                                updateCmd.Parameters.AddWithValue("@Plu", record.plu);
                                updateCmd.Parameters.AddWithValue("@AmountKG", record.amountKg);
                                updateCmd.Parameters.AddWithValue("@Price", record.price);
                                updateCmd.Parameters.AddWithValue("@Status", record.status);
                                updateCmd.Parameters.AddWithValue("@TsId", record.tsId);
                                updateCmd.Parameters.AddWithValue("@IslemTarihi", record.IslemTarihi != DateTime.MinValue ? (object)record.IslemTarihi : DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@IslemYapan", !string.IsNullOrEmpty(record.IslemYapan) ? (object)record.IslemYapan : DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@MagazaID", record.MagazaID);

                                int rowsAffected = updateCmd.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    // **Update Başarısız, Insert Yap**
                                    string insertQuery = @"
                                    INSERT INTO biz.TeraziSatis  (TSatisID, TeraziIp, EtiketTarihi, Barkod, Miktar, Fiyat, FK_DurumKodu, FK_Mekan)
                                    VALUES (@ID, @TerminalIp, @CreateDate, @Plu, @AmountKG, @Price, @Status, (Select MekanID From Mekan Where PosID = @MagazaID))";

                                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, merkezConn, merkezTransaction))
                                    {
                                        insertCmd.Parameters.AddWithValue("@ID", record.id);
                                        insertCmd.Parameters.AddWithValue("@TerminalIp", record.terminalIp);
                                        insertCmd.Parameters.AddWithValue("@CreateDate", record.createDate);
                                        insertCmd.Parameters.AddWithValue("@Plu", record.plu);
                                        insertCmd.Parameters.AddWithValue("@AmountKG", record.amountKg);
                                        insertCmd.Parameters.AddWithValue("@Price", record.price);
                                        insertCmd.Parameters.AddWithValue("@Status", record.status);
                                        insertCmd.Parameters.AddWithValue("@MagazaID", record.MagazaID);
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        else if (record.actionType == "I")
                        {
                            // **Yeni Kayıt (Insert)**
                            string insertQuery = @"
                            INSERT INTO biz.TeraziSatis (TSatisID, TeraziIp, EtiketTarihi, Barkod, Miktar, Fiyat, FK_DurumKodu, FK_Mekan)
                            VALUES (@ID, @TerminalIp, @CreateDate, @Plu, @AmountKG, @Price, @Status, (Select MekanID From Mekan Where PosID = @MagazaID))";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, merkezConn, merkezTransaction))
                            {
                                insertCmd.Parameters.AddWithValue("@ID", record.id);
                                insertCmd.Parameters.AddWithValue("@TerminalIp", record.terminalIp);
                                insertCmd.Parameters.AddWithValue("@CreateDate", record.createDate);
                                insertCmd.Parameters.AddWithValue("@Plu", record.plu);
                                insertCmd.Parameters.AddWithValue("@AmountKG", record.amountKg);
                                insertCmd.Parameters.AddWithValue("@Price", record.price);
                                insertCmd.Parameters.AddWithValue("@Status", record.status);
                                insertCmd.Parameters.AddWithValue("@MagazaID", record.MagazaID);
                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        Logger.Log("GroseriBizerbaSaleControl_",$"Merkeze yazıldı: PLU={record.plu}, KG={record.amountKg}, Tarih={record.createDate}","INFO");

                        // **Başarıyla kaydedilen logları sil**
                        string deleteLogs = "DELETE FROM TeraziSatis_Log WHERE TSatisID = @ID";
                        using (SqlCommand deleteCmd = new SqlCommand(deleteLogs, localConn))
                        {
                            deleteCmd.Parameters.AddWithValue("@ID", record.id);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }

                    merkezTransaction.Commit();
                }
                catch (Exception ex)
                {
                    merkezTransaction.Rollback();
                    Logger.Log("GroseriBizerbaSaleControl_",$"Merkeze gönderme hatası: {ex.Message}", "ERROR");
                }
            }
        }
    }
}
