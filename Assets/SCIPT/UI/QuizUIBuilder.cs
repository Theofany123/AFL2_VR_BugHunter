using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Berisi referensi ke semua elemen UI popup kuis yang dibuat lewat kode.
/// </summary>
public class QuizUIRefs
{
    public GameObject panel;
    public TMP_Text judul;
    public TMP_Text pertanyaan;
    public Button[] tombol;
    public Image[] hati;
}

/// <summary>
/// Membangun seluruh tampilan popup kuis lewat kode (tanpa drag di Unity).
/// Taruh di: Assets/SCIPT/UI/
/// Mengubah tampilan = ubah angka/warna di file ini.
/// </summary>
public static class QuizUIBuilder
{
    // ---------- Warna (sesuai desain) ----------
    private static readonly Color BiruTua    = Hex("2E7396");
    private static readonly Color BiruPanel  = Hex("DCEAF3");
    private static readonly Color BiruTombol = Hex("C5DEEA");
    private static readonly Color MerahHati  = Hex("CC0000");

    // ---------- Ukuran ----------
    private static readonly Vector2 UkuranPanel  = new Vector2(960, 600);
    private static readonly Vector2 UkuranJudul  = new Vector2(360, 100);
    private static readonly Vector2 UkuranTombol = new Vector2(430, 140);
    private const float UkuranHati = 56f;

    private static readonly string[] Huruf = { "A", "B", "C", "D" };

    public static QuizUIRefs Bangun(Transform parent)
    {
        var refs = new QuizUIRefs();

        Sprite spritePanel  = BuatSpriteKotak(128, 36, 6, BiruPanel, BiruTua);
        Sprite spriteJudul  = BuatSpriteKotak(96, 30, 4, BiruTua, BiruTua);
        Sprite spriteTombol = BuatSpriteKotak(64, 16, 4, BiruTombol, BiruTua);
        Sprite spriteHati   = BuatSpriteHati(64, MerahHati);

        // ----- Panel utama (di tengah layar) -----
        RectTransform panel = BuatRect("PanelQuiz", parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, UkuranPanel);
        TambahImage(panel, spritePanel, Color.white, true);
        refs.panel = panel.gameObject;

        // ----- Judul "QUIZ n" (menempel di tepi atas panel) -----
        RectTransform judul = BuatRect("Judul", panel,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), UkuranJudul);
        TambahImage(judul, spriteJudul, Color.white, true);
        refs.judul = BuatTeks("TeksJudul", judul, "QUIZ 1", 66, BiruPanel, FontStyles.Bold);

        // ----- Hati / nyawa (kiri atas) -----
        refs.hati = new Image[2];
        for (int i = 0; i < refs.hati.Length; i++)
        {
            RectTransform h = BuatRect("Hati" + (i + 1), panel,
                new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(70 + i * 68, -75), new Vector2(UkuranHati, UkuranHati));
            Image img = TambahImage(h, spriteHati, Color.white, false);
            img.preserveAspect = true;
            img.raycastTarget = false;
            refs.hati[i] = img;
        }

        // ----- Teks pertanyaan -----
        RectTransform soal = BuatRect("TeksPertanyaan", panel,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(880, 100));
        refs.pertanyaan = BuatTeks("Teks", soal, "Pertanyaan", 42, BiruTua, FontStyles.Bold, true, 28f, 0f);

        // ----- 4 tombol jawaban (grid 2 x 2) -----
        refs.tombol = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -225f : 225f;
            float y = (i < 2) ? -315f : -475f;

            RectTransform rt = BuatRect("Btn" + Huruf[i], panel,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), UkuranTombol);
            Image img = TambahImage(rt, spriteTombol, Color.white, true);

            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            cb.pressedColor     = new Color(0.80f, 0.80f, 0.80f, 1f);
            cb.selectedColor    = Color.white;
            cb.disabledColor    = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            BuatTeks("Teks", rt, Huruf[i] + ".", 42, BiruTua, FontStyles.Bold, true, 24f, 18f);
            refs.tombol[i] = btn;
        }

        return refs;
    }

    // =====================================================================
    //  Helper pembuat objek UI
    // =====================================================================

    private static RectTransform BuatRect(string nama, Transform parent,
        Vector2 anchor, Vector2 pivot, Vector2 posisi, Vector2 ukuran)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = posisi;
        rt.sizeDelta = ukuran;
        return rt;
    }

    private static Image TambahImage(RectTransform rt, Sprite sprite, Color warna, bool sliced)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = warna;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    private static TMP_Text BuatTeks(string nama, RectTransform parent, string isi,
        float ukuran, Color warna, FontStyles gaya,
        bool ukuranOtomatis = false, float ukuranMin = 0f, float margin = 0f)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin > 0f ? 8f : 0f);
        rt.offsetMax = new Vector2(-margin, margin > 0f ? -8f : 0f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = isi;
        t.fontSize = ukuran;
        t.color = warna;
        t.fontStyle = gaya;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        // Teks panjang otomatis mengecil sampai muat di dalam kotaknya
        if (ukuranOtomatis)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = ukuranMin;
            t.fontSizeMax = ukuran;
        }
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // =====================================================================
    //  Sprite dibuat lewat kode (tidak perlu file gambar)
    // =====================================================================

    // Kotak bersudut membulat dengan garis tepi, siap dipakai sebagai 9-slice
    private static Sprite BuatSpriteKotak(int ukuran, float radius, float tebalGaris, Color isi, Color garis)
    {
        Texture2D tex = new Texture2D(ukuran, ukuran, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float setengah = ukuran / 2f;
        Vector2 pusat = new Vector2(setengah, setengah);
        Vector2 batas = new Vector2(setengah - radius, setengah - radius);

        for (int y = 0; y < ukuran; y++)
        {
            for (int x = 0; x < ukuran; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - pusat;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - batas;

                float d = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;

                float alpha = Mathf.Clamp01(0.5f - d);
                float campur = Mathf.Clamp01(-d - tebalGaris + 0.5f);

                Color c = Color.Lerp(garis, isi, campur);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, ukuran, ukuran), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // Bentuk hati merah
    private static Sprite BuatSpriteHati(int ukuran, Color warna)
    {
        Texture2D tex = new Texture2D(ukuran, ukuran, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < ukuran; y++)
        {
            for (int x = 0; x < ukuran; x++)
            {
                int kena = 0;
                for (int sy = 0; sy < 3; sy++)
                {
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float fx = (x + (sx + 0.5f) / 3f) / ukuran;
                        float fy = (y + (sy + 0.5f) / 3f) / ukuran;
                        float hx = (fx * 2f - 1f) * 1.3f;
                        float hy = (fy * 2f - 1f) * 1.3f + 0.1f;

                        float a = hx * hx + hy * hy - 1f;
                        if (a * a * a - hx * hx * hy * hy * hy <= 0f) kena++;
                    }
                }
                Color c = warna;
                c.a = kena / 9f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, ukuran, ukuran), new Vector2(0.5f, 0.5f), 100f);
    }
}