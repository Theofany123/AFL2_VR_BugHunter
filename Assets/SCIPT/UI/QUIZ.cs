using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Logika kuis: membangun popup lewat kode, menampilkan soal, mengecek jawaban,
/// mengurangi nyawa, dan kembali ke soal pertama jika nyawa habis.
/// Taruh di: Assets/SCIPT/UI/
///
/// Cukup pasang script ini ke satu GameObject (misalnya objek "QUIZ" di dalam Canvas).
/// QuizDatabase, nyawa, dan seluruh tampilan popup dibuat otomatis.
/// </summary>
public class QUIZ : MonoBehaviour
{
    [Header("Pengaturan")]
    [Tooltip("Centang untuk langsung memunculkan kuis saat Play (berguna untuk tes).")]
    public bool bukaOtomatisSaatStart = true;
    public float jedaFeedback = 1.35f;   // [DIUBAH] sedikit lebih lama dari durasi animasi bug tengil (1.3 detik)
    [Tooltip("[BARU] Jeda setelah jawaban benar di soal terakhir sebelum bug jatuh.")]
    public float jedaBenarTerakhir = 0.4f;

    // [BARU] Diisi BugTengil: lama animasi bug. Kuis menunggu sampai animasi selesai
    // (diambil yang lebih besar antara Jeda Feedback dan nilai ini).
    [System.NonSerialized] public float jedaTambahan;

    [Tooltip("Ukuran popup. 1 = ukuran penuh, makin kecil angkanya makin kecil popup-nya.")]
    [Range(0.4f, 1f)]
    public float skalaPopup = 0.75f;

    [Tooltip("Dicentang: saat nyawa habis, kuis mengulang sendiri dari soal pertama. " +
             "BugManager mematikan ini supaya gagal berarti kembali ke bug pertama.")]
    public bool ulangOtomatisSaatGagal = true;
    public Color warnaBenar = new Color(0.55f, 0.90f, 0.55f);
    public Color warnaSalah = new Color(1.00f, 0.55f, 0.55f);

    // [SUARA] Tambahan 1: variabel suara FAH
    [Header("Suara")]
    public AudioClip suaraKurangNyawa;
    [Range(0f, 1f)] public float volumeSuara = 1f;
    // [WOW] Tambahan 1: variabel suara WOW saat jawaban benar
    public AudioClip suaraBenar;
    [Range(0f, 1f)] public float volumeSuaraBenar = 1f;
    // [AKH] Tambahan 1: variabel suara AKH saat bug kalah (pindah ke bug berikutnya)
    public AudioClip suaraBugKalah;   // suara cadangan kalau daftar di bawah kosong
    [Range(0f, 1f)] public float volumeSuaraBugKalah = 1f;
    // [GETOUT] Tambahan 1: suara berbeda untuk tiap bug. Element 0 = bug 1, Element 1 = bug 2, dst.
    [Tooltip("Suara saat tiap bug kalah. Element 0 = bug 1, 1 = bug 2, 2 = bug 3. Kosong = pakai Suara Bug Kalah.")]
    public AudioClip[] suaraBugKalahPerBug;
    private AudioSource sumberEfek;

    [Header("Event")]
    public UnityEvent onQuizGagal;      // dipanggil saat nyawa habis (jika Ulang Otomatis Saat Gagal dimatikan)
    public UnityEvent onQuizSelesai;    // dipanggil saat semua soal berhasil dijawab

    // [BARU] Dipakai BugTengil.cs
    public System.Action<bool> saatJawabanBenar;   // true = ini soal terakhir untuk bug tersebut
    public System.Action<bool> saatJawabanSalah;   // true = nyawa habis

    private QuizDatabase database;
    private nyawa sistemNyawa;
    private QuizUIRefs ui;

    private readonly string[] huruf = { "A", "B", "C", "D" };
    private int indexSoal;
    private int indexMulai;     // soal pertama untuk sesi kuis ini
    private int indexAkhir;     // batas akhir (tidak ikut) untuk sesi kuis ini
    private bool terkunci;

    // Dipakai script lain (BugQuizTrigger) untuk tahu apakah kuis sedang tampil
    public bool SedangTerbuka => ui != null && ui.panel != null && ui.panel.activeSelf;

    private void Awake()
    {
        // [SUARA] Tambahan 2: siapkan pemutar suara
        sumberEfek = gameObject.AddComponent<AudioSource>();
        sumberEfek.playOnAwake = false;

        // Data soal & nyawa: pakai yang sudah ada, kalau belum ada dibuat otomatis
        database = GetComponent<QuizDatabase>();
        if (database == null) database = gameObject.AddComponent<QuizDatabase>();

        sistemNyawa = GetComponent<nyawa>();
        if (sistemNyawa == null) sistemNyawa = gameObject.AddComponent<nyawa>();

        // Bangun tampilan popup di dalam Canvas
        ui = QuizUIBuilder.Bangun(CariAtauBuatCanvas());

        sistemNyawa.ikonHati = ui.hati;
        sistemNyawa.ResetNyawa();

        for (int i = 0; i < ui.tombol.Length; i++)
        {
            int pilihan = i; // salinan lokal supaya tidak tertukar
            ui.tombol[i].onClick.AddListener(() => Jawab(pilihan));
        }

        ui.panel.SetActive(false);
    }

    private void Start()
    {
        if (bukaOtomatisSaatStart) BukaQuiz();
    }

    // ---------- Dipanggil dari luar (misalnya BugController) ----------

    // Membuka kuis dengan semua soal
    public void BukaQuiz()
    {
        BukaQuiz(0, database.JumlahSoal);
    }

    // Membuka kuis dengan sebagian soal: mulai dari soal ke-"mulai" (0 = soal pertama)
    // sebanyak "jumlah" soal. Dipakai BugManager untuk membagi soal per bug.
    public void BukaQuiz(int mulai, int jumlah)
    {
        indexMulai = Mathf.Clamp(mulai, 0, database.JumlahSoal - 1);
        indexAkhir = Mathf.Clamp(indexMulai + jumlah, indexMulai + 1, database.JumlahSoal);

        indexSoal = indexMulai;
        terkunci = false;
        sistemNyawa.ResetNyawa();
        ui.panel.SetActive(true);
        SesuaikanUkuran();
        TampilkanSoal();
    }

    public void TutupQuiz()
    {
        ui.panel.SetActive(false);
    }

    // [BARU] Dipakai BugTengil.cs untuk menyembunyikan panel sebentar saat bug beraksi
    public void SembunyikanPanel(bool sembunyi)
    {
        if (ui == null || ui.panel == null) return;

        CanvasGroup cg = ui.panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = ui.panel.AddComponent<CanvasGroup>();

        cg.alpha = sembunyi ? 0f : 1f;
        cg.blocksRaycasts = !sembunyi;
    }

    // ---------- Internal ----------

    // Mengecilkan popup otomatis kalau layar terlalu pendek (misalnya tablet landscape)
    // supaya judul dan tombol tidak terpotong.
    private void SesuaikanUkuran()
    {
        RectTransform panelRt = (RectTransform)ui.panel.transform;
        RectTransform canvasRt = panelRt.parent as RectTransform;
        if (canvasRt == null) return;

        Canvas.ForceUpdateCanvases();

        float lebar = canvasRt.rect.width;
        float tinggi = canvasRt.rect.height;
        if (lebar <= 0f || tinggi <= 0f) return;

        const float judulMenonjol = 60f;   // bagian judul yang keluar dari tepi atas panel
        float tinggiTotal = panelRt.sizeDelta.y + judulMenonjol;

        float skala = Mathf.Min(1f,
            tinggi * 0.92f / tinggiTotal,
            lebar * 0.94f / panelRt.sizeDelta.x) * skalaPopup;

        panelRt.localScale = Vector3.one * skala;
        panelRt.anchoredPosition = new Vector2(0f, -(judulMenonjol / 2f) * skala);
    }

    private Transform CariAtauBuatCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas.transform;

        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return go.transform;
    }

    private void TampilkanSoal()
    {
        QuizQuestion soal = database.AmbilSoal(indexSoal);
        if (soal == null) return;

        ui.judul.text = "QUIZ " + (indexSoal + 1);
        ui.pertanyaan.text = soal.pertanyaan;

        for (int i = 0; i < ui.tombol.Length; i++)
        {
            bool adaPilihan = i < soal.pilihan.Length;
            ui.tombol[i].gameObject.SetActive(adaPilihan);
            if (!adaPilihan) continue;

            ui.tombol[i].interactable = true;
            ui.tombol[i].image.color = Color.white;

            TMP_Text label = ui.tombol[i].GetComponentInChildren<TMP_Text>();
            label.text = huruf[i] + ". " + soal.pilihan[i];
        }
    }

    private void Jawab(int pilihan)
    {
        if (terkunci) return;
        StartCoroutine(ProsesJawaban(pilihan));
    }

    private IEnumerator ProsesJawaban(int pilihan)
    {
        terkunci = true;

        bool benar = pilihan == database.AmbilSoal(indexSoal).jawabanBenar;

        if (benar)
        {
            ui.tombol[pilihan].image.color = warnaBenar;

            // [BARU] bug ikut bereaksi (true kalau ini soal terakhir untuk bug ini)
            bool terakhir = indexSoal + 1 >= indexAkhir;

            // [WOW] Tambahan 2: WOW hanya untuk jawaban benar BIASA. Soal terakhir memakai AKH saja.
            if (!terakhir && suaraBenar != null) sumberEfek.PlayOneShot(suaraBenar, volumeSuaraBenar);
            jedaTambahan = 0f;
            saatJawabanBenar?.Invoke(terakhir);

            // Soal terakhir: jeda singkat saja, lalu bug langsung jatuh.
            // Selain itu: tunggu sampai animasi bug selesai.
            float jedaBenar = terakhir ? jedaBenarTerakhir : Mathf.Max(jedaFeedback, jedaTambahan);
            jedaTambahan = 0f;
            yield return new WaitForSeconds(jedaBenar);

            indexSoal++;
            if (indexSoal >= indexAkhir)
            {
                SelesaiQuiz();
            }
            else
            {
                TampilkanSoal();
            }
        }
        else
        {
            ui.tombol[pilihan].image.color = warnaSalah;
            ui.tombol[pilihan].interactable = false;
            sistemNyawa.KurangiNyawa();

            // [SUARA] Tambahan 3: bunyikan FAH saat nyawa berkurang
            if (suaraKurangNyawa != null) sumberEfek.PlayOneShot(suaraKurangNyawa, volumeSuara);

            // [BARU] bug ikut bereaksi (true kalau nyawa habis)
            jedaTambahan = 0f;
            saatJawabanSalah?.Invoke(sistemNyawa.NyawaSekarang <= 0);

            // Tunggu sampai animasi bug selesai, baru soal berikutnya muncul
            float jedaSalah = Mathf.Max(jedaFeedback, jedaTambahan);
            jedaTambahan = 0f;
            yield return new WaitForSeconds(jedaSalah);

            if (sistemNyawa.NyawaSekarang <= 0)
            {
                if (ulangOtomatisSaatGagal)
                {
                    // Gagal: ulang dari soal pertama dengan nyawa penuh
                    indexSoal = indexMulai;
                    sistemNyawa.ResetNyawa();
                    TampilkanSoal();
                }
                else
                {
                    // Gagal: kuis ditutup, pengaturan ulang diserahkan ke script lain (BugManager)
                    sistemNyawa.ResetNyawa();
                    TutupQuiz();
                    onQuizGagal.Invoke();
                }
            }
        }

        terkunci = false;
    }

    private void SelesaiQuiz()
    {
        // [AKH] Tambahan 2: bunyikan AKH saat semua soal untuk bug ini benar (bug kalah)
        // [GETOUT] Tambahan 2: pilih suara sesuai bug yang baru kalah
        AudioClip klip = suaraBugKalah;
        BugManager bm = FindFirstObjectByType<BugManager>();
        if (bm != null && suaraBugKalahPerBug != null)
        {
            for (int i = 0; i < bm.daftarBug.Count; i++)
            {
                if (bm.daftarBug[i].soalMulai != indexMulai) continue;
                if (i < suaraBugKalahPerBug.Length && suaraBugKalahPerBug[i] != null)
                    klip = suaraBugKalahPerBug[i];
                break;
            }
        }
        if (klip != null) sumberEfek.PlayOneShot(klip, volumeSuaraBugKalah);

        ui.panel.SetActive(false);
        onQuizSelesai.Invoke();
    }
}