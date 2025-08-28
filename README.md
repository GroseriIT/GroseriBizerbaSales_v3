# BizerbaSales – Teknik Readme
**Revizyon:** 2025-08-28 07:56

Bu proje, **Bizerba terazilerinden** (ve/veya kasalarından) gelen **satış/veri** akışını alıp kurumsal POS/ERP veritabanlarına aktarmayı, tutarlılık kontrolü yapmayı ve hataları loglamayı amaçlayan bir **Windows tabanlı** (servis/konsol) .NET uygulamasıdır.

---
## 1) Çözüm / Proje Yapısı
```
BizerbaSales_v3/
BizerbaSales_v3/BizerbaSaleControl/
BizerbaSales_v3/BizerbaSaleControl/App.config
BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.Designer.cs
BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs
BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.csproj
BizerbaSales_v3/BizerbaSaleControl/CheckMerkez.cs
BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs
BizerbaSales_v3/BizerbaSaleControl/Program.cs
BizerbaSales_v3/BizerbaSaleControl/ProjectInstaller.cs
BizerbaSales_v3/BizerbaSaleControl/Properties/
BizerbaSales_v3/BizerbaSaleControl/Properties/AssemblyInfo.cs
BizerbaSales_v3/BizerbaSaleControl/Properties/Resources.Designer.cs
BizerbaSales_v3/BizerbaSaleControl/Properties/Resources.resx
BizerbaSales_v3/BizerbaSaleControl/SaleControl.cs
BizerbaSales_v3/BizerbaSaleControl/TransferMerkez.cs
BizerbaSales_v3/BizerbaSaleControl/config.ini
BizerbaSales_v3/BizerbaSaleImport/
BizerbaSales_v3/BizerbaSaleImport/App.config
BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.Designer.cs
BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs
BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.csproj
BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.csproj.user
BizerbaSales_v3/BizerbaSaleImport/DbTabloOlustur.cs
BizerbaSales_v3/BizerbaSaleImport/Program.cs
BizerbaSales_v3/BizerbaSaleImport/Properties/
BizerbaSales_v3/BizerbaSaleImport/Properties/AssemblyInfo.cs
BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaGonder.cs
BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaOku.cs
BizerbaSales_v3/BizerbaSaleImport/config.ini
BizerbaSales_v3/BizerbaSaleImport/sablon.s00
BizerbaSales_v3/BizerbaSales_v3.sln
BizerbaSales_v3/README.md
```

- Çözüm dosyaları: BizerbaSales_v3/BizerbaSales_v3.sln
- Projeler: 2 adet csproj
- Konfig dosyaları: BizerbaSales_v3/.vs/BizerbaSales_v3/v17/DocumentLayout.backup.json, BizerbaSales_v3/.vs/BizerbaSales_v3/v17/DocumentLayout.json, BizerbaSales_v3/BizerbaSaleControl/App.config, BizerbaSales_v3/BizerbaSaleControl/bin/Release/GroseriBizerbaSaleControl.exe.config, BizerbaSales_v3/BizerbaSaleControl/config.ini, BizerbaSales_v3/BizerbaSaleImport/App.config, BizerbaSales_v3/BizerbaSaleImport/bin/Release/GroseriBizerbaSaleImport.exe.config, BizerbaSales_v3/BizerbaSaleImport/config.ini
- Varsa mevcut README: BizerbaSales_v3/README.md

---
## 2) Uygulama Türü ve Çalıştırma Akışı
- **Windows Service** bileşeni içeriyor (ServiceBase türevleri tespit edildi):
  - `BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/Program.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/Program.cs`
- Giriş noktaları (Program.cs):
  - `BizerbaSales_v3/BizerbaSaleControl/Program.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/Program.cs`

**Zamanlama:** Kodda Timer kullanımları tespit edildi.
**Dosya İzleme:** `FileSystemWatcher` kullanılmıyor gibi görünüyor.

---
## 3) Önemli Bileşenler ve Dosyalar (Heuristik Tespit)
- **ServerConn** →
  - `BizerbaSales_v3/BizerbaSaleControl/CheckMerkez.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/SaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/TransferMerkez.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/DbTabloOlustur.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaOku.cs`
- **MailSender** →
  - `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`
- **Logger** →
  - `BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/CheckMerkez.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/SaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/TransferMerkez.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/DbTabloOlustur.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaGonder.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaOku.cs`
- **AyarOku** →
  - `BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
- **Bizerba** →
  - `BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleControl/Properties/AssemblyInfo.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaGonder.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaOku.cs`
- **Sale** →
  - `BizerbaSales_v3/BizerbaSaleControl/BizerbaSaleControl.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
- **Import** →
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
- **Terazi** →
  - `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`
  - `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
- **Xml** →
  - `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`

**SQL kullanan dosyalar:**
- `BizerbaSales_v3/BizerbaSaleControl/CheckMerkez.cs`
- `BizerbaSales_v3/BizerbaSaleControl/MailPrepare.cs`
- `BizerbaSales_v3/BizerbaSaleControl/SaleControl.cs`
- `BizerbaSales_v3/BizerbaSaleControl/TransferMerkez.cs`
- `BizerbaSales_v3/BizerbaSaleImport/BizerbaSaleImport.cs`
- `BizerbaSales_v3/BizerbaSaleImport/DbTabloOlustur.cs`
- `BizerbaSales_v3/BizerbaSaleImport/TeraziDosyaOku.cs`

**Tespit edilen tablo/ifade örnekleri (FROM ...):**
- `Genius3.GENIUS3.TRANSACTION_HEADER`
- `INFORMATION_SCHEMA.TABLES`
- `Mekan`
- `TeraziSatis`
- `TeraziSatis_Log`
- `deleted`
- `inserted`
- `sys.indexes`
- `sys.objects`
- `sys.triggers`

**Kodda görünen path örnekleri:**
- `
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
            <body>`

**İşlenen uzantılar:** (belirsiz)

---
## 4) Konfigürasyon ve Sözleşmeler
- `config.ini` / `app.config` / `*.json` benzeri dosyalarla yapılandırma yapılır (listede görülen konfigler yukarıda).
- **DB erişimleri** için tipik olarak `ServerConn` benzeri bir yardımcı sınıf kullanılır; POS (kasa/mağaza/merkez) ve ERP bağlantıları ayrıdır.
- **Bizerba** cihaz dizinleri, işlenen dosya uzantıları (`.csv`, `.xml` vb.) ve hedef klasörler (örn. `Inbox`, `Processed`, `Error`) yapılandırma ile belirlenmelidir.
- **Loglama** için dosya tabanlı bir `Logger` beklenir; hatalı kayıtlar hem loga hem de **Hata / Unprocessed** klasörlerine taşınır.

---
## 5) Olası İş Akışları (Bizerba Senaryoları)
1) **Dosya İzleme/Toplama**: Bizerba cihazlarının paylaştığı klasörlerden gelen dosyalar izlenir veya periyodik olarak taranır.
2) **Doğrulama/Parsing**: Dosya uzantısına göre ayrıştırılır (CSV/XML). Zorunlu alanlar (ürünID, miktar, tutar, tarih/saat, kasa/terazi ID...).
3) **Dönüşüm/Haritalama**: Ürün/kategori/PLU eşlemesi; kampanya/indirim kuralları.
4) **Kalıcılık**: Mağaza/POS/ERP veritabanına yazılır (transaction, idempotent işlemler, duplicate engelleme).
5) **Temizlik/Arşiv**: Başarılı dosyalar `Processed`, hatalılar `Error` veya `Unprocessed`.
6) **Bildirim**: Kritik hatalarda e‑posta veya dashboard entegrasyonu (opsiyonel).

---
## 6) Derleme ve Kurulum
- **.NET Framework / .NET (projedeki hedef sürüm)** ile Visual Studio 2019/2022 önerilir.
- Servisse: `InstallUtil.exe` ya da `sc create` ile kurulum, `sc failure` ile kurtarma politikası.
- Çalışma hesabına klasör ve paylaşımlarda **Modify** izni verin (özellikle Bizerba ağ paylaşımları).

---
## 7) Loglama, Hata Yönetimi, Gözlemlenebilirlik
- Dosya temelli loglar ve günlük rotasyon (gerekirse `Yedek/Processed/Error` dizinleri).
- Tekilleştirme/İdempotent yazma; aynı dosyanın iki kez işlenmesi engellenmeli.
- SQL tarafı için `READ UNCOMMITTED`/`SNAPSHOT` gibi izolasyon, `TRY...CATCH` blokları, geri alma stratejileri.

---
## 8) Operasyon Kontrol Listesi
- [ ] Bizerba paylaşımlarına (SMB) erişim testi (okuma/yazma)
- [ ] DB bağlantıları (POS/ERP/Merkez/Mağaza)
- [ ] `Processed/Error` klasörleri var ve yazılabilir
- [ ] Zamanlayıcı/FileSystemWatcher davranışı test edildi
- [ ] Log dizinleri ve disk kotası kontrol edildi

---
## 9) İyileştirme Önerileri
- Config şeması ve doğrulama (eksik anahtarları erken yakala).
- Dosya işleme hattı için **queue** (örn. MSMQ/RabbitMQ) ve **retry/backoff**.
- Yapısal log ve merkezi toplayıcı (ELK/Graylog).
- Health‑check endpoint (servis için) veya EventLog’a düzenli heartbeat.