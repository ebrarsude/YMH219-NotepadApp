using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// XML dosyasının kök elemanı. Dosyaya not listesiyle birlikte kayıt zamanı ve biçim sürümü de yazılır.
    /// </summary>
    [XmlRoot("NotDefteri")]
    public class NotArsivi
    {
        [XmlAttribute("surum")]
        public int Surum { get; set; } = 1;

        public DateTime KayitZamani { get; set; }

        [XmlArray("Notlar")]
        [XmlArrayItem("Not")]
        public List<Not> Notlar { get; set; } = new List<Not>();
    }
}
