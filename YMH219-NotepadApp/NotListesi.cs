using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Notları kart görünümünde listeleyen ListBox: başlık, tarih, içeriğin ilk satırı ve etiketler.
    /// Sabitlenen notların solunda sarı bir şerit vardır.
    /// </summary>
    public class NotListesi : ListBox
    {
        private const int KenarBoslugu = 12;

        private readonly Font baslikFontu = new Font("Segoe UI", 10f, FontStyle.Bold);
        private readonly Font kucukFont = new Font("Segoe UI", 8.25f);

        private static readonly Color SeciliArkaPlan = Color.FromArgb(227, 238, 253);
        private static readonly Color SeciliSerit = Color.FromArgb(30, 136, 229);
        private static readonly Color SabitSerit = Color.FromArgb(255, 193, 7);
        private static readonly Color GriYazi = Color.FromArgb(117, 117, 117);
        private static readonly Color AyiriciRenk = Color.FromArgb(232, 234, 237);
        private static readonly Color EtiketArkaPlan = Color.FromArgb(232, 240, 254);
        private static readonly Color EtiketYazi = Color.FromArgb(25, 103, 210);

        public NotListesi()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            IntegralHeight = false;

            // Satır yüksekliği yazı tipi boyutundan hesaplanır; ekran ölçeklemesinde de kartlar sığar.
            ItemHeight = Math.Min(255, baslikFontu.Height + kucukFont.Height * 2 + 26);
        }

        /// <summary>Bilgileri değişen bir notun kartını yeniden çizer.</summary>
        public void NotuYenidenCiz(Not not)
        {
            int sira = Items.IndexOf(not);
            if (sira >= 0)
                Invalidate(GetItemRectangle(sira));
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle kart = e.Bounds;
            bool secili = (e.State & DrawItemState.Selected) != 0;

            using (SolidBrush arkaPlan = new SolidBrush(secili ? SeciliArkaPlan : BackColor))
                g.FillRectangle(arkaPlan, kart);

            if (!(Items[e.Index] is Not not))
                return;

            // Sol şerit: seçili not mavi, sabitlenen not sarı
            if (secili || not.Sabitlendi)
            {
                using (SolidBrush serit = new SolidBrush(secili ? SeciliSerit : SabitSerit))
                    g.FillRectangle(serit, kart.X, kart.Y, 4, kart.Height);
            }

            int x = kart.X + KenarBoslugu;
            int genislik = kart.Width - 2 * KenarBoslugu;
            int y = kart.Y + 7;

            // Tarih sağda, başlık solda
            string tarih = TarihMetni(not.DuzenlemeTarihi);
            Size tarihBoyutu = TextRenderer.MeasureText(g, tarih, kucukFont);
            TextRenderer.DrawText(g, tarih, kucukFont, new Point(kart.Right - KenarBoslugu - tarihBoyutu.Width, y + 2), GriYazi);

            string baslik = (not.Sabitlendi ? "★ " : "") + not.GorunenBaslik;
            Rectangle baslikAlani = new Rectangle(x, y, Math.Max(10, genislik - tarihBoyutu.Width - 8), baslikFontu.Height);
            TextRenderer.DrawText(g, baslik, baslikFontu, baslikAlani, ForeColor,
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            y += baslikFontu.Height + 2;

            // İçeriğin ilk satırı
            string ozet = not.Ozet.Length > 0 ? not.Ozet : "(içerik yok)";
            TextRenderer.DrawText(g, ozet, kucukFont, new Rectangle(x, y, genislik, kucukFont.Height), GriYazi,
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            y += kucukFont.Height + 4;

            // Etiketler küçük kutucuklar hâlinde
            int etiketX = x;
            foreach (string etiket in not.Etiketler)
            {
                string metin = "#" + etiket;
                Size boyut = TextRenderer.MeasureText(g, metin, kucukFont);
                if (etiketX + boyut.Width + 6 > kart.Right - KenarBoslugu)
                    break;

                Rectangle kutu = new Rectangle(etiketX, y, boyut.Width + 6, kucukFont.Height + 2);
                using (SolidBrush etiketFircasi = new SolidBrush(EtiketArkaPlan))
                    g.FillRectangle(etiketFircasi, kutu);
                TextRenderer.DrawText(g, metin, kucukFont, kutu, EtiketYazi,
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                etiketX += kutu.Width + 5;
            }

            using (Pen ayirici = new Pen(AyiriciRenk))
                g.DrawLine(ayirici, kart.X, kart.Bottom - 1, kart.Right, kart.Bottom - 1);
        }

        /// <summary>Bugün düzenlenen notlar için yalnızca saat, diğerleri için gün ve ay gösterilir.</summary>
        private static string TarihMetni(DateTime tarih)
        {
            return tarih.Date == DateTime.Today ? tarih.ToString("HH:mm") : tarih.ToString("dd.MM.yyyy");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                baslikFontu.Dispose();
                kucukFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
