using System;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Not dosyası var ama içeriği beklenen XML biçiminde değil (yarım kalmış, elle bozulmuş vb.).
    /// </summary>
    public class NotDosyasiBozukException : Exception
    {
        public NotDosyasiBozukException(string mesaj, Exception icHata)
            : base(mesaj, icHata)
        {
        }
    }
}
