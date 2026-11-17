using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Layar instruksi "MISI KAMU" lengkap dengan 4 foto dan tombol MULAI.
/// Semua UI dibangun lewat kode. Tempel script ini ke GameObject Canvas.
/// Sudut membulat dibuat otomatis, jadi tidak perlu sprite tambahan.
/// </summary>
public class InstruksiUI : MonoBehaviour
{
    [Header("Gambar (drag Sprite dari Project)")]
    public Sprite backgroundSprite;                 // Assets/UI_DESIGN/bg
    public Sprite[] fotoBug = new Sprite[4];        // foto 1 - foto 4
    public Sprite panelSprite;                      // opsional: sprite sudut bulat buatanmu (9-sliced)

    [Header("Font TextMeshPro (opsional)")]
    public TMP_FontAsset fontJudul;                 // contoh: Lilita One
    public TMP_FontAsset fontIsi;                   // contoh: PT Sans Bold / Ubuntu Bold

    [Header("Teks")]
    public string judul = "MISI KAMU";
    public string subjudul = "Kamu adalah seorang Bug Hunter";
    [TextArea(3, 6)]
    public string deskripsi =
        "Ada 4 bug misterius yang bersembunyi di sekitar area ini. " +
        "Temukan mereka satu per satu, hadapi tantangannya, dan " +
        "selesaikan 2 pertanyaan untuk mengalahkan setiap bug!";
    public string judulPanel = "CARI AKU!!!";
    public string teksTombol = "MULAI";

    [Header("Transparansi (1 = solid, makin kecil makin tembus)")]
    [Range(0f, 1f)] public float opasitasBackground = 0.88f;
    [Range(0f, 1f)] public float opasitasPanel = 0.96f;

    [Header("Warna")]
    public Color warnaBackground = new Color32(24, 78, 180, 255);   // dipakai kalau bg kosong
    public Color warnaJudul = new Color32(10, 60, 130, 255);
    public Color warnaOutlineJudul = new Color32(220, 235, 255, 255);
    public Color warnaPanel = new Color32(8, 58, 125, 255);
    public Color warnaBorder = new Color32(235, 245, 255, 255);
    public Color warnaKartu = new Color32(46, 112, 144, 255);
    public Color warnaTeks = new Color32(235, 245, 255, 255);
    public Color warnaTombol = new Color32(255, 176, 32, 255);
    public Color warnaTeksTombol = new Color32(10, 60, 130, 255);

    [Header("Saat tombol MULAI ditekan")]
    public bool tutupSetelahMulai = true;
    public GameObject[] aktifkanSaatMulai;          // contoh: objek AR / HUD game
    public GameObject[] nonaktifkanSaatMulai;
    public UnityEvent onMulai;                      // bisa dihubungkan ke script lain

    GameObject root;
    Sprite spriteBulat;

    void Start()
    {
        Bangun();
    }

    // ---------- Public ----------
    public void Tampilkan() { if (root != null) root.SetActive(true); }
    public void Tutup()     { if (root != null) root.SetActive(false); }

    public void Mulai()
    {
        if (aktifkanSaatMulai != null)
            foreach (var g in aktifkanSaatMulai) if (g != null) g.SetActive(true);
        if (nonaktifkanSaatMulai != null)
            foreach (var g in nonaktifkanSaatMulai) if (g != null) g.SetActive(false);

        onMulai?.Invoke();

        if (tutupSetelahMulai) Tutup();
    }

    // ---------- Build ----------
    void Bangun()
    {
        if (root != null) return;

        // Skala UI otomatis (kalau ada CanvasScaler)
        var scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        spriteBulat = panelSprite != null ? panelSprite : BuatSpriteBulat(26);

        // Root layar penuh (juga memblok tap ke belakang)
        RectTransform rootRT = Buat("InstruksiRoot", transform);
        Rata(rootRT);
        root = rootRT.gameObject;
        Image rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0);
        rootImg.raycastTarget = true;

        // Background (menutup layar tanpa melar, sedikit transparan)
        RectTransform bgRT = Buat("Background", rootRT);
        Rata(bgRT);
        Image bg = bgRT.gameObject.AddComponent<Image>();
        bg.raycastTarget = false;
        if (backgroundSprite != null)
        {
            bg.sprite = backgroundSprite;
            bg.color = new Color(1f, 1f, 1f, opasitasBackground);
            var fit = bgRT.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
        }
        else
        {
            bg.color = DenganAlpha(warnaBackground, opasitasBackground);
        }

        // Konten tengah (susun vertikal)
        RectTransform konten = Buat("Konten", rootRT);
        konten.anchorMin = konten.anchorMax = konten.pivot = new Vector2(0.5f, 0.5f);
        konten.sizeDelta = new Vector2(1100, 1000);
        var vlg = konten.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 16;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Judul (dengan outline terang)
        TMP_Text t = BuatTeks("Judul", konten, judul, 135, FontStyles.Bold, warnaJudul, fontJudul);
        t.outlineWidth = 0.35f;
        t.outlineColor = (Color32)DenganAlpha(warnaOutlineJudul, 1f);
        AturTinggi(t.gameObject, 165);

        // Sub judul
        TMP_Text sub = BuatTeks("SubJudul", konten, subjudul, 34, FontStyles.Bold, warnaTeks, fontIsi);
        AturTinggi(sub.gameObject, 46);

        // Deskripsi
        TMP_Text desk = BuatTeks("Deskripsi", konten, deskripsi, 30, FontStyles.Bold, warnaTeks, fontIsi);
        desk.margin = new Vector4(80, 0, 80, 0);
        AturTinggi(desk.gameObject, 120);

        // Panel "CARI AKU!!!" (border putih + isi biru, sudut bulat)
        RectTransform border = Buat("PanelBorder", konten);
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = DenganAlpha(warnaBorder, 1f);
        TerapkanSprite(borderImg);
        AturTinggi(border.gameObject, 430);

        RectTransform isi = Buat("PanelIsi", border);
        Rata(isi);
        isi.offsetMin = new Vector2(5, 5);
        isi.offsetMax = new Vector2(-5, -5);
        Image isiImg = isi.gameObject.AddComponent<Image>();
        isiImg.color = DenganAlpha(warnaPanel, opasitasPanel);
        TerapkanSprite(isiImg);

        var pv = isi.gameObject.AddComponent<VerticalLayoutGroup>();
        pv.padding = new RectOffset(22, 22, 14, 22);
        pv.spacing = 12;
        pv.childControlWidth = true;
        pv.childControlHeight = true;
        pv.childForceExpandWidth = true;
        pv.childForceExpandHeight = true;

        TMP_Text lbl = BuatTeks("JudulPanel", isi, judulPanel, 38, FontStyles.Bold, warnaTeks, fontIsi);
        AturTinggi(lbl.gameObject, 52);

        // Baris 4 kartu foto
        RectTransform baris = Buat("BarisFoto", isi);
        var hlg = baris.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        var barisLE = baris.gameObject.AddComponent<LayoutElement>();
        barisLE.flexibleHeight = 1;

        for (int i = 0; i < 4; i++)
            BuatKartu(baris, i);

        // Tombol MULAI
        BuatTombolMulai(konten);
    }

    void BuatKartu(Transform parent, int index)
    {
        RectTransform kartu = Buat("Kartu_" + (index + 1), parent);
        Image kartuImg = kartu.gameObject.AddComponent<Image>();
        kartuImg.color = DenganAlpha(warnaKartu, 1f);
        TerapkanSprite(kartuImg);
        var le = kartu.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.flexibleHeight = 1;

        bool adaFoto = fotoBug != null && index < fotoBug.Length && fotoBug[index] != null;

        if (adaFoto)
        {
            RectTransform fotoRT = Buat("Foto", kartu);
            Rata(fotoRT);
            fotoRT.offsetMin = new Vector2(10, 10);
            fotoRT.offsetMax = new Vector2(-10, -10);
            Image foto = fotoRT.gameObject.AddComponent<Image>();
            foto.sprite = fotoBug[index];
            foto.color = Color.white;
            foto.preserveAspect = true;
            foto.raycastTarget = false;
        }
        else
        {
            TMP_Text lbl = BuatTeks("Label", kartu, "foto " + (index + 1), 34, FontStyles.Bold, warnaTeks, fontIsi);
            Rata(lbl.rectTransform);
        }
    }

    void BuatTombolMulai(Transform parent)
    {
        // Wadah supaya tombol tidak melebar penuh
        RectTransform wadah = Buat("WadahTombol", parent);
        AturTinggi(wadah.gameObject, 110);

        RectTransform tombol = Buat("TombolMulai", wadah);
        tombol.anchorMin = tombol.anchorMax = tombol.pivot = new Vector2(0.5f, 0.5f);
        tombol.sizeDelta = new Vector2(360, 90);

        // Border putih
        Image borderImg = tombol.gameObject.AddComponent<Image>();
        borderImg.color = DenganAlpha(warnaBorder, 1f);
        TerapkanSprite(borderImg);

        var bayangan = tombol.gameObject.AddComponent<Shadow>();
        bayangan.effectColor = new Color(0f, 0f, 0f, 0.3f);
        bayangan.effectDistance = new Vector2(0, -6);

        // Isi tombol
        RectTransform isi = Buat("Isi", tombol);
        Rata(isi);
        isi.offsetMin = new Vector2(5, 5);
        isi.offsetMax = new Vector2(-5, -5);
        Image isiImg = isi.gameObject.AddComponent<Image>();
        isiImg.color = DenganAlpha(warnaTombol, 1f);
        TerapkanSprite(isiImg);

        TMP_Text teks = BuatTeks("Teks", isi, teksTombol, 46, FontStyles.Bold, warnaTeksTombol, fontJudul != null ? fontJudul : fontIsi);
        Rata(teks.rectTransform);

        // Komponen Button
        Button btn = tombol.gameObject.AddComponent<Button>();
        btn.targetGraphic = isiImg;
        var warna = btn.colors;
        warna.normalColor = Color.white;
        warna.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        warna.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        warna.selectedColor = Color.white;
        warna.colorMultiplier = 1f;
        warna.fadeDuration = 0.08f;
        btn.colors = warna;
        btn.onClick.AddListener(Mulai);
    }

    // ---------- Helper ----------
    static Color DenganAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    RectTransform Buat(string nama, Transform parent)
    {
        var go = new GameObject(nama, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    void Rata(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    TMP_Text BuatTeks(string nama, Transform parent, string isi, float ukuran,
                      FontStyles gaya, Color warna, TMP_FontAsset font)
    {
        RectTransform rt = Buat(nama, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = isi;
        t.fontSize = ukuran;
        t.fontStyle = gaya;
        t.color = DenganAlpha(warna, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }

    void AturTinggi(GameObject go, float tinggi)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = tinggi;
        le.minHeight = tinggi;
    }

    void TerapkanSprite(Image img)
    {
        img.sprite = spriteBulat;
        img.type = Image.Type.Sliced;
    }

    // Membuat sprite kotak sudut bulat (9-sliced) lewat kode
    Sprite BuatSpriteBulat(int radius)
    {
        int size = radius * 2 + 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect,
                             new Vector4(radius, radius, radius, radius));
    }
}