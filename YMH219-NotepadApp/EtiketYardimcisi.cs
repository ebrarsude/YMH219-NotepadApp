using System;
using System.Collections.Generic;
using System.Linq;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Kullanıcının yazdığı "okul, sınav; #ödev" gibi metni etiket listesine çevirir ve tersini yapar.
    /// </summary>
    public static class EtiketYardimcisi
    {
        private static readonly char[] Ayiricilar = { ',', ';' };

        /// <summary>
        /// Virgül veya noktalı virgülle ayrılmış etiketleri ayırır. Baştaki # işareti ve boşluklar atılır,
        /// büyük/küçük harf farkıyla tekrarlanan etiketler bir kez alınır.
        /// </summary>
        public static List<string> Ayristir(string metin)
        {
            return metin
                .Split(Ayiricilar, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().TrimStart('#').Trim())
                .Where(e => e.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static string Birlestir(IEnumerable<string> etiketler)
        {
            return string.Join(", ", etiketler);
        }
    }
}
