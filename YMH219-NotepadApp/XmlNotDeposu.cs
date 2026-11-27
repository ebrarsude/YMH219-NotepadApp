using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Notları XmlSerializer ile bir XML dosyasına serileştirir ve dosyadan geri okur.
    /// </summary>
    public class XmlNotDeposu : INotDeposu
    {
        private static readonly XmlSerializer Serilestirici = new XmlSerializer(typeof(NotArsivi));

        public XmlNotDeposu(string dosyaYolu)
        {
            Konum = dosyaYolu;
        }

        public string Konum { get; }

        /// <summary>Kullanıcının AppData klasöründe YMH219-NotepadApp\notlar.xml</summary>
        public static string VarsayilanDosyaYolu()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "YMH219-NotepadApp", "notlar.xml");
        }

        public void Kaydet(IEnumerable<Not> notlar)
        {
            string? klasor = Path.GetDirectoryName(Konum);
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            NotArsivi arsiv = new NotArsivi
            {
                KayitZamani = DateTime.Now,
                Notlar = notlar.ToList()
            };

            // CheckCharacters = false: nota yapıştırılmış görünmez kontrol karakterleri de
            // kayıt sırasında hata vermeden saklanabilsin.
            XmlWriterSettings ayarlar = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false),
                CheckCharacters = false
            };

            using (StreamWriter akis = new StreamWriter(Konum, false, new UTF8Encoding(false)))
            using (XmlWriter yazici = XmlWriter.Create(akis, ayarlar))
            {
                Serilestirici.Serialize(yazici, arsiv);
            }
        }

        public List<Not> Yukle()
        {
            if (!File.Exists(Konum))
                throw new FileNotFoundException("Not dosyası bulunamadı.", Konum);

            string xml;
            using (StreamReader okuyucu = new StreamReader(Konum, Encoding.UTF8))
            {
                xml = okuyucu.ReadToEnd();
            }

            // Boş dosya "hiç not yok" demektir.
            if (xml.Trim().Length == 0)
                return new List<Not>();

            NotArsivi? arsiv;
            try
            {
                using (StringReader metin = new StringReader(xml))
                using (XmlReader xmlOkuyucu = XmlReader.Create(metin, new XmlReaderSettings { CheckCharacters = false }))
                {
                    arsiv = Serilestirici.Deserialize(xmlOkuyucu) as NotArsivi;
                }
            }
            catch (InvalidOperationException ex)
            {
                // XmlSerializer biçim hatalarını InvalidOperationException içinde bildirir.
                throw new NotDosyasiBozukException("Not dosyası okunamadı; içerik geçerli bir not defteri XML'i değil.", ex);
            }
            catch (XmlException ex)
            {
                throw new NotDosyasiBozukException("Not dosyası okunamadı; XML biçimi bozuk.", ex);
            }

            if (arsiv == null)
                return new List<Not>();

            List<Not> notlar = arsiv.Notlar.Where(n => n != null).ToList();
            foreach (Not not in notlar)
                EksikleriTamamla(not);

            return notlar;
        }

        /// <summary>
        /// Okunamayan dosyayı aynı klasörde "notlar.bozuk-tarih.xml" adıyla saklar ve yeni yolunu döndürür.
        /// Böylece bir sonraki kayıt bozuk dosyanın üzerine yazmaz.
        /// </summary>
        public string BozukDosyayiKenaraAl()
        {
            string klasor = Path.GetDirectoryName(Konum) ?? "";
            string yeniAd = Path.GetFileNameWithoutExtension(Konum) + ".bozuk-"
                            + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml";
            string yeniYol = Path.Combine(klasor, yeniAd);
            File.Move(Konum, yeniYol);
            return yeniYol;
        }

        /// <summary>Elle düzenlenmiş dosyalarda eksik kalan alanlar null olmasın.</summary>
        private static void EksikleriTamamla(Not not)
        {
            if (not.Id == Guid.Empty)
                not.Id = Guid.NewGuid();

            not.Baslik = not.Baslik ?? "";
            not.Icerik = not.Icerik ?? "";
            not.BicimliIcerik = not.BicimliIcerik ?? "";
            not.Etiketler = (not.Etiketler ?? new List<string>()).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

            if (not.OlusturmaTarihi == default(DateTime))
                not.OlusturmaTarihi = DateTime.Now;
            if (not.DuzenlemeTarihi == default(DateTime))
                not.DuzenlemeTarihi = not.OlusturmaTarihi;
        }
    }
}
