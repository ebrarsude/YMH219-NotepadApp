using System.Collections.Generic;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Notların kalıcı olarak saklandığı yer. Uygulamanın geri kalanı yalnızca bu arayüzü bilir;
    /// böylece XML yerine başka bir saklama biçimine geçmek için yalnızca yeni bir sınıf yazmak yeterlidir.
    /// </summary>
    public interface INotDeposu
    {
        /// <summary>Notların saklandığı dosyanın yolu.</summary>
        string Konum { get; }

        /// <summary>
        /// Notları okur. Dosya yoksa FileNotFoundException, içerik okunamıyorsa
        /// NotDosyasiBozukException fırlatır.
        /// </summary>
        List<Not> Yukle();

        /// <summary>Notları yazar. Yazılamazsa IOException veya UnauthorizedAccessException fırlatır.</summary>
        void Kaydet(IEnumerable<Not> notlar);
    }
}
