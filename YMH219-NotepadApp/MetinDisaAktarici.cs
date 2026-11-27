using System;
using System.IO;
using System.Linq;
using System.Text;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Bir notu düz metin (.txt) dosyasına yazar. Biçimlendirme (kalın, renk vb.) dosyaya taşınmaz.
    /// </summary>
    public static class MetinDisaAktarici
    {
        public static void DisaAktar(Not not, string dosyaYolu)
        {
            using (StreamWriter yazici = new StreamWriter(dosyaYolu, false, new UTF8Encoding(true)))
            {
                yazici.WriteLine(not.GorunenBaslik);
                yazici.WriteLine(new string('-', Math.Max(20, not.GorunenBaslik.Length)));

                if (not.Etiketler.Count > 0)
                    yazici.WriteLine("Etiketler    : " + string.Join(" ", not.Etiketler.Select(e => "#" + e)));

                yazici.WriteLine("Oluşturuldu  : " + not.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm"));
                yazici.WriteLine("Düzenlendi   : " + not.DuzenlemeTarihi.ToString("dd.MM.yyyy HH:mm"));
                yazici.WriteLine();

                // RichTextBox satırları yalnızca '\n' ile ayırır; dosyada Windows satır sonu kullanılır.
                string icerik = not.Icerik.Replace("\r\n", "\n").TrimEnd('\n', ' ');
                foreach (string satir in icerik.Split('\n'))
                    yazici.WriteLine(satir);
            }
        }

        /// <summary>Not başlığından dosya adı önerir; dosya adında kullanılamayan karakterler '-' olur.</summary>
        public static string DosyaAdiOner(Not not)
        {
            char[] gecersiz = Path.GetInvalidFileNameChars();
            string ad = new string(not.Baslik.Trim().Select(c => gecersiz.Contains(c) ? '-' : c).ToArray()).Trim();
            return (ad.Length == 0 ? "not" : ad) + ".txt";
        }
    }
}
