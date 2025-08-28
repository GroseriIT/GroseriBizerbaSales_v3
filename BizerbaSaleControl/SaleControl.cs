using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BizerbaSaleControl
{
    public class SaleControl
    {
        ServerConn serverConn = new ServerConn();

        public void SaleControlRecords()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(serverConn.grsDbMagazaConn()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_EtiketVeKasaSatisKarsilastir", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.ExecuteNonQuery();
                        Logger.Log("GroseriBizerbaSaleControl_",$"Satış kontrol procedure çalıştırıldı - {DateTime.Now}","INFO");
                        Console.WriteLine($"Satış kontrol procedure çalıştırıldı - {DateTime.Now}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GroseriBizerbaSaleControl_",$"Satış kontrol procedure çalıştırma hatası: {ex.Message}","ERROR");
                Console.WriteLine($"Satış kontrol procedure çalıştırma hatası: {ex.Message}");
            }
        }
    }
}
