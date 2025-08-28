using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizerbaSaleControl
{
    static class CheckMerkez
    {
        public static bool CheckMerkezConnection()
        {
            ServerConn serverConn = new ServerConn();

            try
            {
                using (SqlConnection conn = new SqlConnection(serverConn.grsDbConn()))
                {
                    conn.Open();
                    Logger.Log("GroseriBizerbaSaleControl_","Merkez Bağlantısı Başarılı.","INFO");
                    return true; // Bağlantı başarılı
                }
            }
            catch
            {
                Logger.Log("GroseriBizerbaSaleControl_","Merkez Bağlantısı Başarısız.","ERROR");
                return false; // Bağlantı başarısız
            }
        }
    }
}
