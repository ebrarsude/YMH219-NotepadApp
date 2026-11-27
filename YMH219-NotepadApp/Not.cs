using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Tek bir not. XmlSerializer'ın okuyup yazabilmesi için parametresiz kurucusu ve
    /// okunup yazılabilen public özellikleri vardır.
    /// </summary>
    public class Not
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Baslik { get; set; } = "";

        /// <summary>Düz metin içerik. Arama ve .txt dışa aktarma bu alanı kullanır.</summary>
        public string Icerik { get; set; } = "";

        /// <summary>Kalın, italik, renk gibi biçimleri de taşıyan RTF içerik.</summary>
        public string BicimliIcerik { get; set; } = "";

        [XmlArray("Etiketler")]
        [XmlArrayItem("Etiket")]
        public List<string> Etiketler { get; set; } = new List<string>();

        /// <summary>Sabitlenen notlar listede her zaman en üstte görünür.</summary>
        public bool Sabitlendi { get; set; }

        public DateTime OlusturmaTarihi { get; set; }

        public DateTime DuzenlemeTarihi { get; set; }

        /// <summary>Listede gösterilen başlık; başlık boşsa yer tutucu metin döner.</summary>
        [XmlIgnore]
        public string GorunenBaslik
        {
            get { return Baslik.Trim().Length > 0 ? Baslik.Trim() : "(Başlıksız not)"; }
        }

        /// <summary>İçeriğin ilk dolu satırı; listede başlığın altında gösterilir.</summary>
        [XmlIgnore]
        public string Ozet
        {
            get
            {
                foreach (string satir in Icerik.Split('\n'))
                {
                    string temiz = satir.Trim();
                    if (temiz.Length > 0)
                        return temiz.Length > 90 ? temiz.Substring(0, 90) + "…" : temiz;
                }
                return "";
            }
        }

        /// <summary>Başlığı ve içeriği boş olan not dosyaya kaydedilmez.</summary>
        public bool BosMu()
        {
            return Baslik.Trim().Length == 0 && Icerik.Trim().Length == 0;
        }

        public override string ToString()
        {
            return GorunenBaslik;
        }
    }
}
