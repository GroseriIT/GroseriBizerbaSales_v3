using System;
using System.Data.SqlClient;

public class DbTabloOlustur
{
    public static void DbTabloKontrolEt()
    {
        ServerConn serverConn = new ServerConn();

        using (SqlConnection conn = new SqlConnection(serverConn.grsDbMagazaConn()))
        {
            string a = serverConn.grsDbMagazaConn();

            conn.Open();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // TeraziSatis Tablosu Kontrol ve Oluşturma
                cmd.CommandText = @"
                    -- **TeraziSatis tablosu yoksa oluştur**
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TeraziSatis')
                    BEGIN
                        CREATE TABLE TeraziSatis (
                            TSatisID NVARCHAR(50) NOT NULL PRIMARY KEY,
                            TeraziIp NVARCHAR(50) NOT NULL,
                            EtiketTarihi DATETIME NOT NULL DEFAULT GETDATE(),
                            Barkod NVARCHAR(20) NOT NULL,
                            Miktar DECIMAL(10,3) NOT NULL,
                            Tutar DECIMAL(10,2) NOT NULL,
                            FK_DurumKodu INT NOT NULL DEFAULT 0,
                            FK_TransactionSale BIGINT NULL,
                            IslemTarihi DATETIME NULL,
                            IslemYapan NVARCHAR(50) NULL
                        );
                    END;
    
                    -- **İndex ekleme (Eğer yoksa oluştur)**
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IDX_TeraziSatis_EtiketTarihi')
                    BEGIN
                        CREATE NONCLUSTERED INDEX IDX_TeraziSatis_EtiketTarihi
                        ON TeraziSatis (EtiketTarihi, Barkod, Miktar, FK_TransactionSale);
                    END;

                    -- **Triger ekleme (Eğer yoksa oluştur)**
                    IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_TeraziSatis_Log')
                    BEGIN
                        EXEC('
                            CREATE TRIGGER trg_TeraziSatis_Log
                            ON TeraziSatis
                            AFTER INSERT, UPDATE
                            AS
                            BEGIN
                                SET NOCOUNT ON;

                                -- **INSERT İşlemi İçin Log Kaydı**
                                INSERT INTO TeraziSatis_Log (TSatisID, AksiyonTipi, AksiyonTarihi)
                                SELECT TSatisID, ''I'', GETDATE()
                                FROM inserted
                                WHERE NOT EXISTS (SELECT 1 FROM deleted WHERE deleted.TSatisID = inserted.TSatisID);

                                -- **UPDATE İşlemi İçin Log Kaydı**
                                INSERT INTO TeraziSatis_Log (TSatisID, AksiyonTipi, AksiyonTarihi)
                                SELECT TSatisID, ''U'', GETDATE()
                                FROM inserted
                                WHERE EXISTS (SELECT 1 FROM deleted WHERE deleted.TSatisID = inserted.TSatisID);
                            END;
                        ');
                    END;

                    -- **İndex çalıştır**
                    ALTER INDEX IDX_TeraziSatis_EtiketTarihi ON TeraziSatis REBUILD

                    -- **MekanHataTespit tablosu yoksa oluştur**
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MekanHataTespit')
                    BEGIN
                        CREATE TABLE MekanHataTespit(
                                [TespitID] [nvarchar](50) NOT NULL,
                                [TespitTarihi] [datetime] NOT NULL,
                                [TespitKonu] [nvarchar](50) NOT NULL,
                                [TespitAciklama] [nvarchar](250) NOT NULL
                        ) ON [PRIMARY]

                        ALTER TABLE [dbo].[MekanHataTespit] ADD  CONSTRAINT [DF_MekanHataTespit_TespitID]  DEFAULT (newid()) FOR [TespitID]

                        ALTER TABLE [dbo].[MekanHataTespit] ADD  CONSTRAINT [DF_MekanHataTespit_TespitTarihi]  DEFAULT (getdate()) FOR [TespitTarihi]
                    END;

                    -- **TeraziSatis_Log  tablosu yoksa oluştur**
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TeraziSatis_Log')
                    BEGIN
                        CREATE TABLE TeraziSatis_Log (
                            LogID BIGINT IDENTITY(1,1) PRIMARY KEY,
                            TSatisID NVARCHAR(50) NOT NULL,
                            AksiyonTipi CHAR(1) NOT NULL,
                            AksiyonTarihi DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END;

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_EtiketVeKasaSatisKarsilastir')
                    DROP PROCEDURE dbo.sp_EtiketVeKasaSatisKarsilastir

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_EtiketVeKasaSatisKarsilastir')
                    BEGIN
                        EXEC('
                            CREATE PROCEDURE sp_EtiketVeKasaSatisKarsilastir
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED 

                                --Bekleyen satışları al
                                SELECT *,RANK() over(partition by ''1'' order By t.Barkod,t.EtiketTarihi asc) Ranks
                                INTO #Sales
                                FROM TeraziSatis t
                                WHERE t.FK_DurumKodu in (0,8) and CAST(EtiketTarihi as date) = CAST(GETDATE() as date)

                                --Satışları kontrol et!
                                DECLARE @Sayac int = 1;
                                WHILE @Sayac <= (Select COUNT(*) From #Sales)
                                BEGIN

                                    UPDATE t
                                    SET t.FK_DurumKodu = 1, t.FK_TransactionSale = s.TsId, t.IslemTarihi = s.CREATE_DATE, t.IslemYapan = s.NAME
                                    From TeraziSatis t
		                            inner join (
                                                    Select s.TSatisID,s.Barkod,s.Miktar,ts.ID TsId,u.NAME ,th.CREATE_DATE
			                            From Genius3.GENIUS3.TRANSACTION_HEADER th
				                            inner join Genius3.GENIUS3.TRANSACTION_SALE ts on 
					                            ts.FK_TRANSACTION_HEADER = th.ID 
					                            and th.STATUS = 0 
					                            and th.PTYPE != 2 
                                                                    and CAST(th.TRANS_DATE as date) = CAST(GETDATE() as date) and NOT EXISTS (SELECT 1 FROM TeraziSatis WHERE FK_TransactionSale = ts.ID)
                                                            inner join #Sales s on
                                                                    s.Ranks = @Sayac
                                                                    and s.Barkod = ts.BARCODE
                                                                    and CAST(s.Miktar as decimal(18,3)) = CAST(ts.AMOUNT as decimal(18,3))
					                            and ts.FK_UNIT = 2
					                            and th.TRANS_DATE >= DATEADD(MINUTE,-5,s.EtiketTarihi)
					                        inner join Genius3.GENIUS3.USERS u on u.ID = th.FK_USER
                                            ) s on s.TSatisID = t.TSatisID and s.Barkod = t.Barkod and s.Miktar = t.Miktar
	                            SET @Sayac += 1;
	
                                END;

                                DROP TABLE #Sales
                            END;
                        ');
                    END;

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_MukerrerKayitTemizle')
                    DROP PROCEDURE dbo.sp_MukerrerKayitTemizle

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_MukerrerKayitTemizle')
                    BEGIN
                        EXEC('
                            CREATE PROC sp_MukerrerKayitTemizle
                            AS
                            BEGIN
                            
                            Select TSatisID,RANK() OVER (PARTITION BY TeraziIP,Barkod,Miktar ORDER BY TeraziIP,Barkod,Miktar,EtiketTarihi ASC) AS Ranks INTO #DeleteSale
                            From TeraziSatis ts
                            Where EtiketTarihi >= DATEADD(MINUTE,-10,GETDATE())

                            DELETE TeraziSatis_Log Where TSatisID in (Select TSatisID From #DeleteSale Where Ranks > 1)
                            DELETE TeraziSatis Where TSatisID in (Select TSatisID From #DeleteSale Where Ranks > 1)

                            DROP TABLE #DeleteSale

                            END;
                        ');
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_EtiketDurumRapor')
                    BEGIN
                        EXEC('
                            CREATE VIEW vw_EtiketDurumRapor AS
                            SELECT
                                t.TeraziIp,
                                t.Barkod,
                                sc.DESCRIPTION AS UrunAdi,
                                t.Miktar,
                                t.Tutar,
                                t.EtiketTarihi
                            FROM TeraziSatis t
                                INNER JOIN Genius3.GENIUS3.STOCK_BARCODE sb ON sb.BARCODE = t.Barkod
                                INNER JOIN Genius3.GENIUS3.STOCK_CARD sc ON sc.ID = sb.FK_STOCK_CARD
                            WHERE t.FK_DurumKodu = 0
                            AND t.EtiketTarihi < DATEADD(MINUTE, -10, GETDATE())
                            AND GETDATE() < DATEADD(MINUTE, 60, t.EtiketTarihi)
                        ');
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'V' AND name = 'vw_GunlukEtiketDurumRapor')
                    BEGIN
                        EXEC('
                            CREATE VIEW vw_GunlukEtiketDurumRapor AS
                            SELECT
                                t.TeraziIp,
                                t.Barkod,
                                sc.DESCRIPTION AS UrunAdi,
                                t.Miktar,
                                t.Tutar,
                                t.EtiketTarihi
                            FROM TeraziSatis t
                                INNER JOIN Genius3.GENIUS3.STOCK_BARCODE sb ON sb.BARCODE = t.Barkod
                                INNER JOIN Genius3.GENIUS3.STOCK_CARD sc ON sc.ID = sb.FK_STOCK_CARD
                            WHERE t.FK_DurumKodu = 0
                            AND CAST(t.EtiketTarihi as date) = CAST(GETDATE() as date)
                        ');
                    END;
                    ";
                cmd.ExecuteNonQuery();
            }
        }

        Logger.Log("GroseriBizerbaSaleImport_","Servis başlatıldı, veritabanı ve tablolar hazır.");
        Console.WriteLine("Servis başlatıldı, veritabanı ve tablolar hazır.");
    }
}
