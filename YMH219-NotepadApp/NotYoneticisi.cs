using System;
using System.Collections.Generic;
using System.Linq;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Notları bir List&lt;Not&gt; koleksiyonunda tutar: oluşturma, güncelleme, silme, sabitleme, arama
    /// ve etiket listesi. Dosyayla ilgilenmez; notlarda değişiklik olup olmadığını izler.
    /// </summary>
    public class NotYoneticisi
    {
        private readonly List<Not> notlar = new List<Not>();

        public IReadOnlyList<Not> Notlar
        {
            get { return notlar; }
        }

        /// <summary>Son kayıttan ya da yüklemeden sonra notlarda değişiklik oldu mu?</summary>
        public bool KaydedilmemisDegisiklikVar { get; private set; }

        /// <summary>
        /// Boş bir not oluşturup listeye ekler; oluşturma ve düzenleme tarihi şimdiki zamandır.
        /// Boş notlar dosyaya yazılmadığı için bu işlem tek başına "kaydedilmemiş değişiklik" sayılmaz.
        /// </summary>
        public Not YeniNot()
        {
            DateTime simdi = DateTime.Now;
            Not not = new Not
            {
                OlusturmaTarihi = simdi,
                DuzenlemeTarihi = simdi
            };

            notlar.Add(not);
            return not;
        }

        /// <summary>
        /// Notun alanlarını değiştirir ve düzenleme tarihini günceller. Hiçbir alan değişmediyse
        /// notu olduğu gibi bırakır ve false döner.
        /// </summary>
        public bool Guncelle(Not not, string baslik, string icerik, string bicimliIcerik, IEnumerable<string> etiketler)
        {
            if (!notlar.Contains(not))
                throw new InvalidOperationException("Güncellenecek not listede yok.");

            List<string> yeniEtiketler = etiketler.ToList();
            bool degisiklikYok = not.Baslik == baslik
                                 && not.Icerik == icerik
                                 && not.BicimliIcerik == bicimliIcerik
                                 && not.Etiketler.SequenceEqual(yeniEtiketler);
            if (degisiklikYok)
                return false;

            not.Baslik = baslik;
            not.Icerik = icerik;
            not.BicimliIcerik = bicimliIcerik;
            not.Etiketler = yeniEtiketler;
            not.DuzenlemeTarihi = DateTime.Now;
            KaydedilmemisDegisiklikVar = true;
            return true;
        }

        public bool Sil(Not not)
        {
            bool silindi = notlar.Remove(not);

            // Boş not zaten dosyada yoktu; silinmesi dosyayı değiştirmez.
            if (silindi && !not.BosMu())
                KaydedilmemisDegisiklikVar = true;

            return silindi;
        }

        public void SabitlemeyiDegistir(Not not)
        {
            if (!notlar.Contains(not))
                throw new InvalidOperationException("Not listede yok.");

            not.Sabitlendi = !not.Sabitlendi;
            KaydedilmemisDegisiklikVar = true;
        }

        /// <summary>
        /// Başlığında, içeriğinde veya etiketlerinde aranan metin geçen notlar. Etiket verilirse
        /// yalnızca o etiketi taşıyanlar döner. Sabitlenenler en üstte, sonra en son düzenlenen önce gelir.
        /// </summary>
        public List<Not> Ara(string aranan, string? etiket)
        {
            string metin = aranan.Trim();
            IEnumerable<Not> sonuc = notlar;

            if (!string.IsNullOrEmpty(etiket))
                sonuc = sonuc.Where(n => n.Etiketler.Any(e => string.Equals(e, etiket, StringComparison.CurrentCultureIgnoreCase)));

            if (metin.Length > 0)
                sonuc = sonuc.Where(n => Iceriyor(n.Baslik, metin)
                                         || Iceriyor(n.Icerik, metin)
                                         || n.Etiketler.Any(e => Iceriyor(e, metin)));

            return sonuc
                .OrderByDescending(n => n.Sabitlendi)
                .ThenByDescending(n => n.DuzenlemeTarihi)
                .ToList();
        }

        /// <summary>Kullanılan etiketler ve her birini taşıyan not sayısı, ada göre sıralı.</summary>
        public List<KeyValuePair<string, int>> EtiketSayilari()
        {
            return notlar
                .SelectMany(n => n.Etiketler)
                .GroupBy(e => e, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new KeyValuePair<string, int>(g.First(), g.Count()))
                .OrderBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Dosyadan okunan notlarla listenin tamamını değiştirir.</summary>
        public void TumunuDegistir(IEnumerable<Not> yeniNotlar)
        {
            List<Not> kopya = yeniNotlar.ToList();
            notlar.Clear();
            notlar.AddRange(kopya);
            KaydedilmemisDegisiklikVar = false;
        }

        public void KaydedildiOlarakIsaretle()
        {
            KaydedilmemisDegisiklikVar = false;
        }

        /// <summary>Bellekteki notlar dosyadakilerle aynı değil (ör. dosya silinmiş ya da bozuk).</summary>
        public void DegisiklikVarOlarakIsaretle()
        {
            KaydedilmemisDegisiklikVar = true;
        }

        /// <summary>Dosyaya yazılacak notlar: başlığı ve içeriği boş olanlar hariç.</summary>
        public List<Not> KaydedilecekNotlar()
        {
            return notlar.Where(n => !n.BosMu()).ToList();
        }

        private static bool Iceriyor(string metin, string aranan)
        {
            return metin.IndexOf(aranan, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
