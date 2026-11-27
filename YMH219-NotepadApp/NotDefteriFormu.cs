using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace YMH219_NotepadApp
{
    /// <summary>
    /// Ana pencere. Soldaki listeden seçilen not sağdaki düzenleyicide açılır. Düzenleyicideki
    /// değişiklikler yazmaya ara verilince (kısa bir gecikmeyle) nota işlenir; "Otomatik kaydet" açıksa
    /// her işlemden sonra bütün notlar XML dosyasına yazılır.
    /// </summary>
    public partial class NotDefteriFormu : Form
    {
        private const string TarihBicimi = "dd.MM.yyyy HH:mm";

        private readonly NotYoneticisi yonetici = new NotYoneticisi();
        private readonly XmlNotDeposu depo = new XmlNotDeposu(XmlNotDeposu.VarsayilanDosyaYolu());

        /// <summary>Etiket filtresi kutusundaki her satırın (ilk satır hariç) karşılık geldiği etiket.</summary>
        private readonly List<string> etiketFiltreAdlari = new List<string>();

        /// <summary>Düzenleyicide açık olan not; hiçbir not açık değilse null.</summary>
        private Not? acikNot;

        /// <summary>Düzenleyicideki değişiklik henüz nota işlenmedi.</summary>
        private bool duzenleyiciKirli;

        /// <summary>Alanları kod doldururken TextChanged gibi olaylar iş yapmasın.</summary>
        private bool arayuzGuncelleniyor;

        /// <summary>Son dosyaya yazma denemesi başarısız oldu.</summary>
        private bool sonKayitBasarisiz;

        public NotDefteriFormu()
        {
            InitializeComponent();
        }

        private bool OtomatikKayitAcik
        {
            get { return mnuOtomatikKaydet.Checked; }
        }

        // ------------------------------------------------------------------
        //  Açılış ve kapanış
        // ------------------------------------------------------------------

        private void NotDefteriFormu_Load(object sender, EventArgs e)
        {
            // Önceki oturumdaki notlar kendiliğinden yüklenir.
            DosyadanYukle(true);

            EtiketFiltresiniYenile();
            ListeyiYenile();
            NotuAc(notListesi.Items.Count > 0 ? notListesi.Items[0] as Not : null);
            KayitDurumunuGoster();
        }

        private void NotDefteriFormu_FormClosing(object sender, FormClosingEventArgs e)
        {
            DuzenleyiciyiIsle();

            if (!yonetici.KaydedilmemisDegisiklikVar)
                return;

            DialogResult cevap = MessageBox.Show(this,
                "Notlardaki değişiklikler dosyaya kaydedilmedi.\nÇıkmadan önce kaydedilsin mi?",
                "Not Defteri", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (cevap == DialogResult.Cancel || (cevap == DialogResult.Yes && !DosyayaYaz(false)))
                e.Cancel = true;
        }

        // ------------------------------------------------------------------
        //  Dosya: yükleme ve kaydetme
        // ------------------------------------------------------------------

        /// <summary>Notları XML dosyasından okur. Başarılıysa true döner; hata durumunu kullanıcıya bildirir.</summary>
        private bool DosyadanYukle(bool acilista)
        {
            try
            {
                List<Not> okunan = depo.Yukle();
                yonetici.TumunuDegistir(okunan);
                sonKayitBasarisiz = false;
                DurumYaz(okunan.Count + " not yüklendi.");
                return true;
            }
            catch (FileNotFoundException)
            {
                if (acilista)
                {
                    // İlk çalıştırmada dosyanın olmaması normaldir.
                    DurumYaz("Henüz not dosyası yok; ilk not kaydedildiğinde oluşturulacak.");
                }
                else
                {
                    MessageBox.Show(this,
                        "Not dosyası bulunamadı:\n" + depo.Konum +
                        "\n\nEkrandaki notlar değiştirilmedi. \"Kaydet\" ile dosya yeniden oluşturulabilir.",
                        "Dosya bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DurumYaz("Yükleme yapılamadı: not dosyası bulunamadı.");
                }
            }
            catch (NotDosyasiBozukException)
            {
                string ek;
                try
                {
                    ek = "Bozuk dosya silinmedi, şu adla saklandı:\n" + depo.BozukDosyayiKenaraAl();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    OtomatikKaydiAyarla(false);
                    ek = "Bozuk dosya yeniden adlandırılamadı (" + ex.Message + ").\n" +
                         "Üzerine yazılmaması için otomatik kayıt kapatıldı.";
                }

                MessageBox.Show(this,
                    "Not dosyası okunamadı; içeriği bozuk.\n\n" + ek + "\n\n" +
                    (acilista ? "Uygulama boş bir not listesiyle açılıyor." : "Ekrandaki notlar değiştirilmedi."),
                    "Not dosyası bozuk", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DurumYaz("Not dosyası bozuk olduğu için yüklenemedi.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Dosya var ama okunamıyor: üzerine yazılmasın diye otomatik kayıt kapatılır.
                OtomatikKaydiAyarla(false);
                MessageBox.Show(this,
                    "Not dosyası okunamadı.\n\nAyrıntı: " + ex.Message +
                    "\n\nDosyanın üzerine yazılmaması için otomatik kayıt kapatıldı.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DurumYaz("Not dosyası okunamadı.");
            }

            // Ekrandaki notlar dosyada yoksa kaydedilmeleri gerekir.
            if (!acilista && yonetici.KaydedilecekNotlar().Count > 0)
                yonetici.DegisiklikVarOlarakIsaretle();

            return false;
        }

        /// <summary>Bütün notları XML dosyasına yazar. Yazılamazsa nedenini bildirir ve false döner.</summary>
        private bool DosyayaYaz(bool otomatik)
        {
            try
            {
                depo.Kaydet(yonetici.KaydedilecekNotlar());
                yonetici.KaydedildiOlarakIsaretle();
                sonKayitBasarisiz = false;
                KayitDurumunuGoster();
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                sonKayitBasarisiz = true;
                string ek = "";
                if (otomatik)
                {
                    // Her tuş vuruşunda aynı hata penceresi çıkmasın.
                    OtomatikKaydiAyarla(false);
                    ek = "\n\nOtomatik kayıt kapatıldı. Sorun giderilince \"Kaydet\" ile yeniden deneyin.";
                }

                MessageBox.Show(this, "Notlar dosyaya kaydedilemedi.\n\nAyrıntı: " + ex.Message + ek,
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DurumYaz("Kaydetme başarısız.");
                KayitDurumunuGoster();
                return false;
            }
        }

        private void OtomatikKaydet()
        {
            if (OtomatikKayitAcik && yonetici.KaydedilmemisDegisiklikVar)
                DosyayaYaz(true);
            else
                KayitDurumunuGoster();
        }

        private void Kaydet_Click(object sender, EventArgs e)
        {
            DuzenleyiciyiIsle();
            if (DosyayaYaz(false))
                DurumYaz(yonetici.KaydedilecekNotlar().Count + " not kaydedildi.");
        }

        private void Yukle_Click(object sender, EventArgs e)
        {
            DuzenleyiciyiIsle();

            if (yonetici.KaydedilmemisDegisiklikVar)
            {
                DialogResult cevap = MessageBox.Show(this,
                    "Kaydedilmemiş değişiklikler var; dosyadan yüklenirse bu değişiklikler kaybolur.\n\nYine de yüklensin mi?",
                    "Dosyadan yükle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (cevap != DialogResult.Yes)
                    return;
            }

            if (DosyadanYukle(false))
            {
                acikNot = null;
                FiltreleriTemizle();
                EtiketFiltresiniYenile();
                ListeyiYenile();
                NotuAc(notListesi.Items.Count > 0 ? notListesi.Items[0] as Not : null);
            }

            KayitDurumunuGoster();
        }

        private void OtomatikKaydet_Click(object sender, EventArgs e)
        {
            // Menüdeki ve araç çubuğundaki seçenek birlikte değişir.
            bool acik = sender == tsbOtomatik ? tsbOtomatik.Checked : mnuOtomatikKaydet.Checked;
            OtomatikKaydiAyarla(acik);

            if (acik)
            {
                DurumYaz("Otomatik kayıt açık: her değişiklikten sonra notlar dosyaya yazılır.");
                OtomatikKaydet();
            }
            else
            {
                DurumYaz("Otomatik kayıt kapalı. Kaydetmek için Ctrl+S kullanın.");
            }
        }

        private void OtomatikKaydiAyarla(bool acik)
        {
            mnuOtomatikKaydet.Checked = acik;
            tsbOtomatik.Checked = acik;
            KayitDurumunuGoster();
        }

        private void mnuKayitKlasoru_Click(object sender, EventArgs e)
        {
            string? klasor = Path.GetDirectoryName(depo.Konum);
            if (string.IsNullOrEmpty(klasor) || !Directory.Exists(klasor))
            {
                MessageBox.Show(this, "Kayıt klasörü henüz oluşmadı. İlk kayıttan sonra şurada olacak:\n" + depo.Konum,
                                "Kayıt klasörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(klasor) { UseShellExecute = true });
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(this, "Klasör açılamadı.\n\nAyrıntı: " + ex.Message, "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void mnuCikis_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ------------------------------------------------------------------
        //  Not komutları: yeni, sil, sabitle, dışa aktar
        // ------------------------------------------------------------------

        private void YeniNot_Click(object sender, EventArgs e)
        {
            DuzenleyiciyiIsle();
            Not? birakilan = acikNot;

            // Yeni not filtreler yüzünden listede gizli kalmasın.
            FiltreleriTemizle();

            Not yeni = yonetici.YeniNot();
            ListeyiYenile();
            NotuAc(yeni);
            BosNotuAt(birakilan);

            txtBaslik.Focus();
            DurumYaz("Yeni not oluşturuldu. Başlık ve içerik yazmaya başlayın.");
        }

        private void Sil_Click(object sender, EventArgs e)
        {
            if (acikNot == null)
                return;

            if (!acikNot.BosMu())
            {
                DialogResult cevap = MessageBox.Show(this,
                    "\"" + acikNot.GorunenBaslik + "\" notu silinsin mi?\nBu işlem geri alınamaz.",
                    "Notu sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (cevap != DialogResult.Yes)
                    return;
            }

            string baslik = acikNot.GorunenBaslik;
            int sira = Math.Max(0, notListesi.Items.IndexOf(acikNot));

            islemeZamanlayicisi.Stop();
            duzenleyiciKirli = false;
            yonetici.Sil(acikNot);
            acikNot = null;

            EtiketFiltresiniYenile();
            ListeyiYenile();

            // Silinen notun yerindeki not açılır.
            Not? sonraki = notListesi.Items.Count == 0 ? null : notListesi.Items[Math.Min(sira, notListesi.Items.Count - 1)] as Not;
            NotuAc(sonraki);

            DurumYaz("\"" + baslik + "\" notu silindi.");
            OtomatikKaydet();
        }

        private void Sabitle_Click(object sender, EventArgs e)
        {
            if (acikNot == null)
                return;

            DuzenleyiciyiIsle();
            yonetici.SabitlemeyiDegistir(acikNot);
            ListeyiYenile();
            KomutlariGuncelle();

            DurumYaz("\"" + acikNot.GorunenBaslik + "\" " + (acikNot.Sabitlendi ? "sabitlendi; listede en üstte duracak." : "notunun sabitlemesi kaldırıldı."));
            OtomatikKaydet();
        }

        private void DisaAktar_Click(object sender, EventArgs e)
        {
            if (acikNot == null)
                return;

            DuzenleyiciyiIsle();
            Not not = acikNot;

            using (SaveFileDialog pencere = new SaveFileDialog())
            {
                pencere.Title = "Notu düz metin olarak dışa aktar";
                pencere.Filter = "Metin dosyası (*.txt)|*.txt";
                pencere.DefaultExt = "txt";
                pencere.FileName = MetinDisaAktarici.DosyaAdiOner(not);

                if (pencere.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    MetinDisaAktarici.DisaAktar(not, pencere.FileName);
                    DurumYaz("Not dışa aktarıldı: " + pencere.FileName);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "Not dışa aktarılamadı.\n\nAyrıntı: " + ex.Message, "Hata",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>Başka bir nota geçilirken, bırakılan not tamamen boşsa listeden çıkarılır.</summary>
        private void BosNotuAt(Not? not)
        {
            if (not == null || not == acikNot || !not.BosMu() || !yonetici.Notlar.Contains(not))
                return;

            yonetici.Sil(not);
            ListeyiYenile();
        }

        // ------------------------------------------------------------------
        //  Liste, arama ve etiket filtresi
        // ------------------------------------------------------------------

        private void notListesi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (arayuzGuncelleniyor)
                return;

            Not? secilen = notListesi.SelectedItem as Not;
            if (secilen == null || secilen == acikNot)
                return;

            DuzenleyiciyiIsle();
            Not? birakilan = acikNot;
            NotuAc(secilen);
            BosNotuAt(birakilan);
        }

        private void notListesi_MouseDown(object sender, MouseEventArgs e)
        {
            // Sağ tıklanan not seçilsin ki bağlam menüsü o not için açılsın.
            if (e.Button == MouseButtons.Right)
            {
                int sira = notListesi.IndexFromPoint(e.Location);
                if (sira >= 0)
                    notListesi.SelectedIndex = sira;
            }
        }

        private void cmsNot_Opening(object sender, CancelEventArgs e)
        {
            if (acikNot == null)
                e.Cancel = true;
        }

        private void tstAra_TextChanged(object sender, EventArgs e)
        {
            if (!arayuzGuncelleniyor)
                ListeyiYenile();
        }

        private void tscEtiket_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!arayuzGuncelleniyor)
                ListeyiYenile();
        }

        private void ListeyiYenile()
        {
            List<Not> gorunen = yonetici.Ara(tstAra.Text, SeciliEtiket());

            arayuzGuncelleniyor = true;
            notListesi.BeginUpdate();
            notListesi.Items.Clear();
            foreach (Not not in gorunen)
                notListesi.Items.Add(not);
            notListesi.EndUpdate();
            notListesi.SelectedIndex = acikNot == null ? -1 : notListesi.Items.IndexOf(acikNot);
            arayuzGuncelleniyor = false;

            int toplam = yonetici.Notlar.Count;
            lblNotSayisi.Text = gorunen.Count == toplam ? toplam + " not" : gorunen.Count + " / " + toplam + " not gösteriliyor";
        }

        private void EtiketFiltresiniYenile()
        {
            string? onceki = SeciliEtiket();

            arayuzGuncelleniyor = true;
            tscEtiket.Items.Clear();
            etiketFiltreAdlari.Clear();
            tscEtiket.Items.Add("Tüm etiketler");
            foreach (KeyValuePair<string, int> etiket in yonetici.EtiketSayilari())
            {
                etiketFiltreAdlari.Add(etiket.Key);
                tscEtiket.Items.Add("#" + etiket.Key + " (" + etiket.Value + ")");
            }

            int sira = onceki == null
                ? -1
                : etiketFiltreAdlari.FindIndex(e => string.Equals(e, onceki, StringComparison.CurrentCultureIgnoreCase));
            tscEtiket.SelectedIndex = sira >= 0 ? sira + 1 : 0;
            arayuzGuncelleniyor = false;
        }

        private string? SeciliEtiket()
        {
            int sira = tscEtiket.SelectedIndex;
            return sira <= 0 || sira - 1 >= etiketFiltreAdlari.Count ? null : etiketFiltreAdlari[sira - 1];
        }

        private void FiltreleriTemizle()
        {
            arayuzGuncelleniyor = true;
            tstAra.Text = "";
            if (tscEtiket.Items.Count > 0)
                tscEtiket.SelectedIndex = 0;
            arayuzGuncelleniyor = false;
        }

        // ------------------------------------------------------------------
        //  Düzenleyici
        // ------------------------------------------------------------------

        /// <summary>Notu düzenleyicide açar; null verilirse "not seçilmedi" ekranı gösterilir.</summary>
        private void NotuAc(Not? not)
        {
            islemeZamanlayicisi.Stop();
            duzenleyiciKirli = false;
            acikNot = not;

            arayuzGuncelleniyor = true;
            if (not == null)
            {
                pnlDuzenleyici.Visible = false;
                pnlBosDurum.Visible = true;
            }
            else
            {
                pnlBosDurum.Visible = false;
                pnlDuzenleyici.Visible = true;
                txtBaslik.Text = not.Baslik;
                txtEtiketler.Text = EtiketYardimcisi.Birlestir(not.Etiketler);
                IcerigiYukle(not);
            }

            notListesi.SelectedIndex = not == null ? -1 : notListesi.Items.IndexOf(not);
            arayuzGuncelleniyor = false;

            AltBilgiyiGuncelle();
            KomutlariGuncelle();
            KayitDurumunuGoster();
        }

        /// <summary>Biçimli içerik varsa onu, yoksa ya da bozuksa düz metni gösterir.</summary>
        private void IcerigiYukle(Not not)
        {
            if (not.BicimliIcerik.Length > 0)
            {
                try
                {
                    rtbIcerik.Rtf = not.BicimliIcerik;
                    return;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
                {
                    // Dosya elle değiştirilmiş olabilir; düz metne dönülür.
                }
            }

            rtbIcerik.Clear();
            rtbIcerik.SelectionFont = rtbIcerik.Font;
            rtbIcerik.SelectionColor = rtbIcerik.ForeColor;
            rtbIcerik.Text = not.Icerik;
        }

        private void Duzenleyici_TextChanged(object sender, EventArgs e)
        {
            DuzenleyiciDegisti();
            if (sender == rtbIcerik)
                AltBilgiyiGuncelle();
        }

        private void txtEtiketler_Leave(object sender, EventArgs e)
        {
            DuzenleyiciyiIsle();
        }

        /// <summary>Kullanıcı bir şey değiştirdi; yazmaya ara verince değişiklik nota işlenir.</summary>
        private void DuzenleyiciDegisti()
        {
            if (arayuzGuncelleniyor || acikNot == null)
                return;

            duzenleyiciKirli = true;
            islemeZamanlayicisi.Stop();
            islemeZamanlayicisi.Start();
            KayitDurumunuGoster();
        }

        private void islemeZamanlayicisi_Tick(object sender, EventArgs e)
        {
            DuzenleyiciyiIsle();
        }

        /// <summary>Düzenleyicideki başlığı, etiketleri ve içeriği açık nota yazar.</summary>
        private void DuzenleyiciyiIsle()
        {
            islemeZamanlayicisi.Stop();
            if (!duzenleyiciKirli || acikNot == null)
                return;

            duzenleyiciKirli = false;
            bool degisti = yonetici.Guncelle(acikNot, txtBaslik.Text.Trim(), rtbIcerik.Text, rtbIcerik.Rtf ?? "",
                                             EtiketYardimcisi.Ayristir(txtEtiketler.Text));
            if (!degisti)
            {
                KayitDurumunuGoster();
                return;
            }

            EtiketFiltresiniYenile();
            ListeyiYenile();
            AltBilgiyiGuncelle();
            OtomatikKaydet();
        }

        private void AltBilgiyiGuncelle()
        {
            if (acikNot == null)
            {
                lblAltBilgi.Text = "";
                return;
            }

            string metin = rtbIcerik.Text;
            int kelime = metin.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            lblAltBilgi.Text = "Oluşturma: " + acikNot.OlusturmaTarihi.ToString(TarihBicimi)
                               + "     Son düzenleme: " + acikNot.DuzenlemeTarihi.ToString(TarihBicimi)
                               + "     " + metin.Length + " karakter, " + kelime + " kelime";
        }

        private void KomutlariGuncelle()
        {
            bool notAcik = acikNot != null;
            string sabitleMetni = acikNot != null && acikNot.Sabitlendi ? "Sabitlemeyi Kaldır" : "Sabitle";

            foreach (ToolStripItem komut in new ToolStripItem[] { tsbSil, mnuSil, cmsSil, tsbDisaAktar, mnuDisaAktar, cmsDisaAktar, tsbSabitle, mnuSabitle, cmsSabitle })
                komut.Enabled = notAcik;

            tsbSabitle.Text = "★ " + sabitleMetni;
            mnuSabitle.Text = sabitleMetni;
            cmsSabitle.Text = sabitleMetni;

            mnuBicim.Enabled = notAcik;
            tsBicim.Enabled = notAcik;
        }

        // ------------------------------------------------------------------
        //  Biçimlendirme
        // ------------------------------------------------------------------

        private void Kalin_Click(object sender, EventArgs e)
        {
            BicimiDegistir(FontStyle.Bold);
        }

        private void Italik_Click(object sender, EventArgs e)
        {
            BicimiDegistir(FontStyle.Italic);
        }

        private void AltiCizili_Click(object sender, EventArgs e)
        {
            BicimiDegistir(FontStyle.Underline);
        }

        private void UstuCizili_Click(object sender, EventArgs e)
        {
            BicimiDegistir(FontStyle.Strikeout);
        }

        /// <summary>Seçili metinde biçim varsa kaldırır, yoksa ekler (XOR).</summary>
        private void BicimiDegistir(FontStyle bicim)
        {
            if (acikNot == null)
                return;

            Font mevcut = rtbIcerik.SelectionFont ?? rtbIcerik.Font;
            rtbIcerik.SelectionFont = new Font(mevcut, mevcut.Style ^ bicim);
            BicimDegisti();
        }

        private void YaziRengi_Click(object sender, EventArgs e)
        {
            if (acikNot == null)
                return;

            using (ColorDialog renkPenceresi = new ColorDialog())
            {
                renkPenceresi.Color = rtbIcerik.SelectionColor;
                if (renkPenceresi.ShowDialog(this) != DialogResult.OK)
                    return;

                rtbIcerik.SelectionColor = renkPenceresi.Color;
            }

            BicimDegisti();
        }

        private void MaddeIsareti_Click(object sender, EventArgs e)
        {
            if (acikNot == null)
                return;

            rtbIcerik.SelectionBullet = !rtbIcerik.SelectionBullet;
            BicimDegisti();
        }

        /// <summary>Biçim değişikliği TextChanged olayını tetiklemez; değişiklik burada bildirilir.</summary>
        private void BicimDegisti()
        {
            DuzenleyiciDegisti();
            rtbIcerik.Focus();
        }

        // ------------------------------------------------------------------
        //  Kullanıcıya geri bildirim
        // ------------------------------------------------------------------

        private void KayitDurumunuGoster()
        {
            if (sonKayitBasarisiz)
                lblKayitDurumu.Text = "Kaydedilemedi";
            else if (yonetici.KaydedilmemisDegisiklikVar || duzenleyiciKirli)
                lblKayitDurumu.Text = OtomatikKayitAcik ? "Kaydediliyor…" : "Kaydedilmemiş değişiklik var";
            else
                lblKayitDurumu.Text = "Tüm notlar kaydedildi";
        }

        private void DurumYaz(string mesaj)
        {
            lblDurum.Text = mesaj;
        }

        private void mnuHakkinda_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "Not Defteri\nYMH 219 Nesne Tabanlı Programlama - Ek Uygulama Ödevi\n\n" +
                "Notlar XML biçiminde şu dosyada saklanır:\n" + depo.Konum,
                "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
