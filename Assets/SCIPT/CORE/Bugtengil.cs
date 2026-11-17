using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bug jadi TENGIL, dengan tangan dan kaki yang bergerak sendiri (bukan satu badan bergetar).
///
/// - Jawaban SALAH : kedua tangan diangkat dan dikibas-kibas, kaki menghentak bergantian,
///                   antena bergoyang, dan muncul bubble kecil ngeledek.
/// - Jawaban BENAR : bug angkat bahu / sewot (tangan terbuka ke samping) + bubble nyindir.
/// - Bug KALAH     : bug jatuh telentang, mata jadi "X X", berkedip, lalu hilang.
///
/// Bagian tubuh (tangan, kaki, antena, mata) dicari otomatis dari NAMA objek di dalam model bug.
/// Hasil pencarian dicetak di Console: "[BugTengil] ...". Kalau ada yang tidak ketemu,
/// ubah daftar kata di bagian "Nama bagian" pada Inspector.
///
/// Taruh di: Assets/SCIPT/CORE/
/// Pasang ke objek "QUIZ" (yang sama dengan QUIZ.cs dan BugManager.cs).
/// </summary>
public class BugTengil : MonoBehaviour
{
    [Header("Bubble chat")]
    public float jarakAtasBug = 20f;
    public float marginTepi = 20f;

    [Header("Durasi (detik)")]
    [Tooltip("Lama bug mengejek saat jawaban SALAH. Kuis otomatis menunggu sampai animasi ini selesai.")]
    public float lamaEjek = 2.6f;
    [Tooltip("Lama bug sewot saat jawaban BENAR.")]
    public float lamaSmug = 2.0f;
    public float durasiJatuh = 0.5f;
    public float lamaBerbaring = 1.1f;

    [Header("Kehebohan")]
    [Tooltip("1 = normal. Naikkan (misal 1.4) supaya makin hiperaktif, turunkan kalau terlalu heboh.")]
    public float kehebohan = 1f;
    [Tooltip("Badan memipih saat mendarat dan memanjang saat melompat (efek kartun).")]
    public bool squashStretch = true;
    [Tooltip("Tinggi lompatan, relatif terhadap tinggi bug.")]
    public float tinggiHop = 0.13f;

    [Header("Gerakan tangan & kaki")]
    public float kecepatanTangan = 14f;
    public float sudutGoyangTangan = 18f;
    public float kecepatanKaki = 9f;
    public float sudutKaki = 22f;

    [Header("Jatuh")]
    [Tooltip("Mengangkat badan sedikit saat berbaring supaya tidak tenggelam ke lantai.")]
    public float angkatBerbaring = 0.28f;

    [Header("Mata X (saat mati)")]
    [Tooltip("Dipakai kalau mata asli tidak ketemu. Tinggi mata dari kaki: 0 = kaki, 1 = ujung antena.")]
    public float tinggiMata = 0.54f;
    public float jarakAntarMata = 0.07f;
    [Tooltip("Seberapa jauh X ada di depan badan. Kalau X tenggelam di dalam kepala, naikkan; kalau melayang, turunkan.")]
    public float jarakDepanWajah = 0.31f;
    public float ukuranMataX = 0.075f;
    public Color warnaMataX = new Color(0.1f, 0.1f, 0.14f);

    [Header("Panel kuis")]
    [Tooltip("Panel kuis disembunyikan sebentar supaya gerakan bug kelihatan.")]
    public bool sembunyikanPanelSaatTengil = true;

    [Header("Nama bagian (huruf besar/kecil tidak berpengaruh)")]
    public string[] kataTangan = { "arm", "hand", "shoulder", "clavicle", "lengan", "tangan", "bahu" };
    public string[] kataKaki = { "leg", "foot", "feet", "thigh", "kaki", "paha" };
    public string[] kataAntena = { "antenna", "antena" };
    public string[] kataMata = { "eye", "mata" };
    public bool cetakStrukturKeConsole = true;

    [Header("Teks bubble (bisa diedit di Inspector)")]
    public string[] teksSalah =
    {
        "Wkwkwk SALAH!",
        "Segitu ji saja? Kodong...",
        "MAKANYA BELAJAR",
        "AWKAOKOWK SALAHH!",
        "weh nyawa mu sisa 1!",
        "NT awokawokaokw",
    };

    public string[] teksNyawaHabis =
    {
        "GAGAL! Ulang dari awal ya, byeee~",
        "AWOKAWOK SKILL ISSUE",
        "Hahaha kalah! ",
    };

    public string[] teksBenar =
    {
        "hmm yayaya",
        "Hmph, hanya hoki si itu",
        "Ah, cuma kebetulan itu!",
        "YAYAYA TAWWA",
        "DIMAZ ANJAY PROPESIONAL!",
    };

    public string[] teksBugKalah =
    {
        "Aaargh aku kalah!",
        "yaya GG",
        "Awas ya... aku balik lagi...",
    };

    // =====================================================================
    //  Data internal
    // =====================================================================

    private enum Jenis { Tangan, Kaki, Antena }

    private class Sendi
    {
        public Transform t;
        public Jenis jenis;
        public int sisi;                 // +1 / -1 menurut sumbu kiri-kanan badan, 0 untuk antena
        public Vector3 posLokal0;
        public Quaternion rotLokal0;
        public Quaternion rotInduk0;
        public Vector3 pivotLokal;       // titik engsel dalam ruang lokal induk
        public Vector3 arah0;            // arah dari engsel ke ujung (dunia, saat awal)
    }

    private class Model
    {
        public Transform root;
        public List<Sendi> sendi = new List<Sendi>();
        public List<Renderer> mata = new List<Renderer>();
        public Animator animator;
        public bool animatorAsli;

        public Renderer[] semuaRend;
        public bool[] asliRend;

        public Vector3 posLokal0, posDunia0;
        public Quaternion rotLokal0, rotDunia0;
        public Vector3 skala0, pivotBawah;
        public Bounds bounds;

        public Vector3 lateral, jauh, camKanan, camMaju;

        public List<GameObject> mataX = new List<GameObject>();
        public Material materialX;
    }

    private QUIZ quiz;
    private BugManager manager;
    private Camera kamera;

    private readonly Dictionary<Transform, Model> cache = new Dictionary<Transform, Model>();
    private Model aktif;
    private Coroutine animCo;
    private bool panelDisembunyikan;
    private int idxTerakhir = -1;
    private Mesh meshX;

    // Bubble
    private Canvas canvas;
    private RectTransform canvasRT;
    private Camera kameraUI;
    private RectTransform bubbleRT;
    private TMP_Text bubbleTeks;
    private Coroutine bubbleCo;
    private Vector3 bubbleTargetDunia;
    private const float LebarBubble = 400f;
    private const float TinggiBubble = 120f;

    // =====================================================================
    //  Unity
    // =====================================================================

    private void Start()
    {
        quiz = FindFirstObjectByType<QUIZ>();
        manager = FindFirstObjectByType<BugManager>();
        kamera = Camera.main;
        if (kamera == null) kamera = FindFirstObjectByType<Camera>();

        if (quiz == null || manager == null || kamera == null)
        {
            Debug.LogError("BugTengil berhenti. QUIZ: " + (quiz != null) +
                           ", BugManager: " + (manager != null) +
                           ", kamera: " + (kamera != null));
            enabled = false;
            return;
        }

        BangunBubble();

        quiz.saatJawabanBenar += SaatBenar;
        quiz.saatJawabanSalah += SaatSalah;
    }

    private void OnDestroy()
    {
        if (quiz == null) return;
        quiz.saatJawabanBenar -= SaatBenar;
        quiz.saatJawabanSalah -= SaatSalah;
    }

    private void OnDisable()
    {
        BerhentiAnimasi();
    }

    // =====================================================================
    //  Reaksi jawaban
    // =====================================================================

    private void SaatBenar(bool bugAkanKalah)
    {
        // Soal terakhir: bug langsung mati (ditangani AnimasiMati dari BugManager)
        if (bugAkanKalah) return;
        Reaksi(false, Pilih(teksBenar));
    }

    private void SaatSalah(bool nyawaHabis)
    {
        Reaksi(true, Pilih(nyawaHabis ? teksNyawaHabis : teksSalah));
    }

    private void Reaksi(bool salah, string pesan)
    {
        GameObject bug = manager.BugAktif;
        if (bug == null) return;

        BerhentiAnimasi();

        Model m = Ambil(bug.transform);
        TampilkanBubble(pesan, bug.transform);

        // Minta kuis menunggu sampai animasi selesai, baru soal berikutnya muncul
        quiz.jedaTambahan = (salah ? lamaEjek : lamaSmug) - 0.03f;

        animCo = StartCoroutine(Gerak(m, salah));
    }

    private string Pilih(string[] daftar)
    {
        if (daftar == null || daftar.Length == 0) return "...";
        int i = Random.Range(0, daftar.Length);
        if (daftar.Length > 1 && i == idxTerakhir) i = (i + 1) % daftar.Length;
        idxTerakhir = i;
        return daftar[i];
    }

    // =====================================================================
    //  Animasi mengejek / sewot
    // =====================================================================

    private IEnumerator Gerak(Model m, bool salah)
    {
        Mulai(m, sembunyikanPanelSaatTengil);

        float durasi = salah ? lamaEjek : lamaSmug;
        float t = 0f;

        while (t < durasi)
        {
            t += Time.deltaTime;
            PoseTengil(m, t, durasi, salah);
            yield return null;
        }

        animCo = null;
        Pulihkan();
    }

    // Koreografi tengil (semua gerakan BESAR dan HALUS, bukan getaran):
    //   jongkok (antisipasi) -> joget lompat-lompat sambil miring & memutar badan
    //   -> lompat putar 360 derajat -> pose mengejek sambil cekikikan.
    // Waktu dinormalkan, jadi durasi bisa diubah bebas lewat Inspector.
    private void PoseTengil(Model m, float t, float durasi, bool salah)
    {
        float D0 = salah ? 2.6f : 2.0f;
        float tn = t * (D0 / Mathf.Max(0.05f, durasi));

        const float tAnt = 0.25f;
        float tAkhirDansa = salah ? 1.65f : 1.45f;
        float tAkhirPutar = salah ? 2.15f : tAkhirDansa;     // putaran hanya saat salah
        int jumlahLompat = salah ? 3 : 2;
        float hopPer = (tAkhirDansa - tAnt) / jumlahLompat;
        float omega = 2f * Mathf.PI / hopPer;

        float keh = kehebohan * (salah ? 1f : 0.6f);
        float tinggi = m.bounds.size.y;

        // ---- selubung waktu ----
        float masuk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tn / 0.3f));
        float turun = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - (D0 - 0.25f)) / 0.25f));
        float tahap = masuk * turun;
        float dansa = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - tAnt) / 0.2f))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - (tAkhirDansa - 0.1f)) / 0.2f)));

        // ---- lompatan ----
        float hopY = 0f;          // 0..1
        float skalaTinggi = 1f;
        if (tn >= tAnt && tn < tAkhirDansa)
        {
            float k = (tn - tAnt) / hopPer;
            float ph = k - Mathf.Floor(k);
            hopY = 4f * ph * (1f - ph);
        }
        else if (salah && tn >= tAkhirDansa && tn < tAkhirPutar)
        {
            float u = (tn - tAkhirDansa) / (tAkhirPutar - tAkhirDansa);
            hopY = 4f * u * (1f - u);
            skalaTinggi = 1.7f;   // lompatan putar lebih tinggi
        }
        else if (tn >= tAkhirPutar)
        {
            hopY = 0.25f * Mathf.Abs(Mathf.Sin((tn - tAkhirPutar) * 8f)) * turun;   // cekikikan: memantul pelan
        }

        // ---- memipih saat mendarat / memanjang saat di udara ----
        float squash = tn < tAnt
            ? Mathf.SmoothStep(0f, 1f, tn / tAnt)
            : Mathf.Clamp01(1f - hopY * 3.5f);
        if (tn >= tAkhirPutar)
            squash *= 0.5f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - tAkhirPutar) / 0.5f)));

        float sy = 1f;
        float sxz = 1f;
        if (squashStretch)
        {
            sy = 1f - 0.20f * squash * keh + 0.12f * hopY * keh;
            sxz = 1f + 0.12f * squash * keh - 0.05f * hopY * keh;
        }

        // ---- miring, memutar pinggang, condong ----
        float siklus = 2f * Mathf.PI * (tn - tAnt) / (2f * hopPer);
        float roll = (salah ? 16f : 13f) * keh * Mathf.Sin(siklus) * dansa;
        float yaw = (salah ? 25f : 18f) * keh * Mathf.Sin(siklus + Mathf.PI * 0.5f) * dansa;
        if (salah && tn >= tAkhirDansa)
            yaw += 360f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - tAkhirDansa) / (tAkhirPutar - tAkhirDansa)));

        float leanEnv = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - (tAkhirPutar - 0.05f)) / 0.2f)) * turun;
        // salah: condong ke arah kamera (nyolot). benar: bersandar ke belakang (sok santai).
        float pitch = ((salah ? -16f : 12f) + Mathf.Sin(tn * 6f) * 5f) * leanEnv * kehebohan;

        Quaternion q = Quaternion.AngleAxis(yaw, Vector3.up)
                     * Quaternion.AngleAxis(roll, m.jauh)
                     * Quaternion.AngleAxis(pitch, m.camKanan);

        // Badan berputar & memipih dengan titik tumpu di kaki
        Vector3 skala = new Vector3(sxz, sy, sxz);
        Vector3 d = m.posDunia0 - m.pivotBawah;
        Vector3 dSkala = new Vector3(d.x * skala.x, d.y * skala.y, d.z * skala.z);

        m.root.rotation = q * m.rotDunia0;
        m.root.position = m.pivotBawah + q * dSkala
                          + Vector3.up * (hopY * tinggiHop * tinggi * skalaTinggi * keh);
        m.root.localScale = Vector3.Scale(m.skala0, skala);

        // ---- tangan, kaki, antena ----
        float serbu = salah
            ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tn - (tAkhirDansa - 0.1f)) / 0.2f))
            : 0f;

        foreach (Sendi s in m.sendi)
        {
            Quaternion qs;
            switch (s.jenis)
            {
                case Jenis.Tangan:
                {
                    Vector3 keluar = m.lateral * s.sisi;
                    Vector3 atas = (keluar * 0.35f + Vector3.up * 0.95f).normalized;
                    Vector3 samping = (keluar * 0.95f + Vector3.up * 0.15f).normalized;
                    Vector3 angkatBahu = (keluar * 0.8f + Vector3.up * 0.5f).normalized;

                    // kiri & kanan mengepak bergantian (satu naik, satu turun)
                    float flap = 0.5f + 0.5f * Mathf.Sin(omega * (tn - tAnt) + (s.sisi > 0 ? 0f : Mathf.PI));
                    Vector3 target = salah
                        ? Vector3.Slerp(samping, atas, flap)
                        : Vector3.Slerp(samping, angkatBahu, flap);
                    if (serbu > 0f) target = Vector3.Slerp(target, atas, serbu);   // akhir: kedua tangan tinggi

                    float tanda = Mathf.Sign(Vector3.Dot(keluar, m.camKanan));
                    float goyang = Mathf.Sin(tn * 18f) * 14f * serbu * tanda * keh;
                    qs = TanganDunia(m, s, target, tahap, goyang);
                    break;
                }
                case Jenis.Kaki:
                {
                    float ampl = (salah ? 28f : 14f) * keh * (tn >= tAkhirPutar ? 0.4f : 1f);
                    float off = s.sisi > 0 ? 0f : Mathf.PI;
                    float ang = Mathf.Sin(omega * (tn - tAnt) + off) * ampl - 22f * hopY * keh;   // menghentak + menekuk di udara
                    qs = Quaternion.AngleAxis(ang * tahap, m.camKanan);
                    break;
                }
                default:
                {
                    float ang = (-roll * 1.4f + Mathf.Sin(tn * 9f) * 16f * keh) * tahap;
                    qs = Quaternion.AngleAxis(ang, m.jauh);
                    break;
                }
            }
            TerapkanSendi(s, qs);
        }
    }

    // =====================================================================
    //  Animasi mati (dipanggil BugManager)
    // =====================================================================

    /// <summary>
    /// Bug jatuh telentang dengan mata X, berkedip, lalu dinonaktifkan.
    /// Dipakai BugManager: yield return StartCoroutine(tengil.AnimasiMati(bug));
    /// </summary>
    public IEnumerator AnimasiMati(GameObject bugGO)
    {
        Transform bug = bugGO.transform;
        Model m = Ambil(bug);

        BerhentiAnimasi();
        Mulai(m, false);

        TampilkanBubble(Pilih(teksBugKalah), bug);
        BuatMataX(m);

        Vector3 pivot = new Vector3(m.bounds.center.x, m.bounds.min.y, m.bounds.center.z);
        Vector3 sumbu = Vector3.Cross(Vector3.up, m.jauh);   // jatuh ke belakang, menjauhi kamera
        float lebar = Mathf.Max(m.bounds.size.x, m.bounds.size.z);
        float total = durasiJatuh + lamaBerbaring;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;

            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, durasiJatuh));
            float sudut = 90f * u * u;                         // makin cepat (gravitasi)
            if (t > durasiJatuh)
            {
                float v = Mathf.Clamp01((t - durasiJatuh) / 0.25f);
                sudut = 90f - Mathf.Sin(v * Mathf.PI) * 7f;    // memantul sedikit saat mendarat
            }

            Quaternion q = Quaternion.AngleAxis(sudut, sumbu);
            m.root.rotation = q * m.rotDunia0;
            m.root.position = pivot + q * (m.posDunia0 - pivot)
                              + Vector3.up * (angkatBerbaring * lebar * Mathf.Sin(sudut * Mathf.Deg2Rad));

            PoseMati(m, t);
            yield return null;
        }

        // Berkedip lalu hilang (tidak ada zoom / mengecil)
        Renderer[] rend = bug.GetComponentsInChildren<Renderer>(false);
        bool[] status = new bool[rend.Length];
        for (int i = 0; i < rend.Length; i++) status[i] = rend[i] != null && rend[i].enabled;

        for (int k = 0; k < 7; k++)
        {
            bool tampil = (k % 2 == 1);
            for (int i = 0; i < rend.Length; i++)
                if (rend[i] != null) rend[i].enabled = tampil && status[i];
            yield return new WaitForSeconds(0.08f);
        }

        bugGO.SetActive(false);
        Pulihkan();   // kembalikan posisi, mata, dan tangan/kaki supaya bug siap muncul lagi
    }

    private void PoseMati(Model m, float t)
    {
        float fase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
        float tahap = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.12f));
        float u = Mathf.Clamp01(t / Mathf.Max(0.01f, durasiJatuh));

        foreach (Sendi s in m.sendi)
        {
            Quaternion q;
            switch (s.jenis)
            {
                case Jenis.Tangan:
                {
                    Vector3 keluar = m.lateral * s.sisi;
                    Vector3 atas = (keluar * 0.35f + Vector3.up * 0.95f).normalized;
                    Vector3 samping = (keluar * 0.95f + Vector3.up * 0.05f).normalized;
                    Vector3 target = Vector3.Slerp(atas, samping, fase);
                    float tanda = Mathf.Sign(Vector3.Dot(keluar, m.camKanan));
                    float goyang = Mathf.Sin(t * kecepatanTangan * 1.3f) * sudutGoyangTangan * (1f - fase) * tanda;
                    q = TanganDunia(m, s, target, tahap, goyang);
                    break;
                }
                case Jenis.Kaki:
                {
                    float f = Mathf.Sin(t * kecepatanKaki * 1.6f + (s.sisi > 0 ? 0f : Mathf.PI));
                    q = Quaternion.AngleAxis(f * sudutKaki * 1.2f * (1f - u), m.camKanan);
                    break;
                }
                default:
                {
                    q = Quaternion.AngleAxis(Mathf.Sin(t * 18f) * 25f * (1f - fase), m.camMaju);
                    break;
                }
            }
            TerapkanSendi(s, q);
        }
    }

    // =====================================================================
    //  Mulai / pulihkan
    // =====================================================================

    private void Mulai(Model m, bool sembunyikanPanel)
    {
        aktif = m;
        Transform root = m.root;

        m.posLokal0 = root.localPosition;
        m.rotLokal0 = root.localRotation;
        m.posDunia0 = root.position;
        m.rotDunia0 = root.rotation;
        m.skala0 = root.localScale;

        m.semuaRend = root.GetComponentsInChildren<Renderer>(true);
        m.asliRend = new bool[m.semuaRend.Length];
        for (int i = 0; i < m.semuaRend.Length; i++)
            m.asliRend[i] = m.semuaRend[i] != null && m.semuaRend[i].enabled;

        // Animator dimatikan sementara supaya tidak menimpa gerakan kita
        m.animatorAsli = false;
        if (m.animator != null && m.animator.enabled)
        {
            m.animator.enabled = false;
            m.animatorAsli = true;
        }

        Bounds b;
        if (!AmbilBounds(root, out b)) b = new Bounds(root.position, Vector3.one * 0.2f);
        m.bounds = b;
        m.pivotBawah = new Vector3(b.center.x, b.min.y, b.center.z);

        Vector3 jauh = root.position - kamera.transform.position;
        jauh.y = 0f;
        if (jauh.sqrMagnitude < 1e-6f)
        {
            jauh = kamera.transform.forward;
            jauh.y = 0f;
        }
        m.jauh = jauh.sqrMagnitude < 1e-6f ? Vector3.forward : jauh.normalized;

        Vector3 kanan = kamera.transform.right;
        kanan.y = 0f;
        m.camKanan = kanan.sqrMagnitude < 1e-6f ? Vector3.right : kanan.normalized;
        m.camMaju = kamera.transform.forward;

        Vector3 lat = root.right;
        lat.y = 0f;
        m.lateral = lat.sqrMagnitude < 1e-6f ? Vector3.right : lat.normalized;

        foreach (Sendi s in m.sendi) Tangkap(s, m);

        if (sembunyikanPanel && quiz != null)
        {
            quiz.SembunyikanPanel(true);
            panelDisembunyikan = true;
        }
    }

    private void BerhentiAnimasi()
    {
        if (animCo != null)
        {
            StopCoroutine(animCo);
            animCo = null;
        }
        Pulihkan();
    }

    private void Pulihkan()
    {
        SembunyikanBubble();

        if (aktif != null)
        {
            Model m = aktif;
            aktif = null;

            if (m.root != null)
            {
                m.root.localPosition = m.posLokal0;
                m.root.localRotation = m.rotLokal0;
                m.root.localScale = m.skala0;
            }

            foreach (Sendi s in m.sendi)
            {
                if (s.t == null) continue;
                s.t.localPosition = s.posLokal0;
                s.t.localRotation = s.rotLokal0;
            }

            if (m.semuaRend != null)
            {
                for (int i = 0; i < m.semuaRend.Length; i++)
                    if (m.semuaRend[i] != null) m.semuaRend[i].enabled = m.asliRend[i];
            }

            foreach (GameObject x in m.mataX)
                if (x != null) Destroy(x);
            m.mataX.Clear();

            if (m.materialX != null)
            {
                Destroy(m.materialX);
                m.materialX = null;
            }

            if (m.animator != null && m.animatorAsli) m.animator.enabled = true;
        }

        if (panelDisembunyikan && quiz != null)
        {
            quiz.SembunyikanPanel(false);
            panelDisembunyikan = false;
        }
    }

    // =====================================================================
    //  Analisis model bug (cari tangan, kaki, antena, mata dari nama objek)
    // =====================================================================

    private Model Ambil(Transform bug)
    {
        Model m;
        if (cache.TryGetValue(bug, out m)) return m;

        m = new Model { root = bug };
        m.animator = bug.GetComponentInChildren<Animator>(true);

        Bounds b;
        if (!AmbilBounds(bug, out b)) b = new Bounds(bug.position, Vector3.one * 0.2f);
        Vector3 pusat = b.center;
        float tinggi = Mathf.Max(0.01f, b.size.y);

        Vector3 lateral = bug.right;
        lateral.y = 0f;
        lateral = lateral.sqrMagnitude < 1e-6f ? Vector3.right : lateral.normalized;

        List<Transform> calonTangan = new List<Transform>();
        List<Transform> calonKaki = new List<Transform>();
        Transform antena = null;

        foreach (Transform t in bug.GetComponentsInChildren<Transform>(true))
        {
            if (t == bug) continue;
            string n = t.name.ToLowerInvariant();
            if (n.Contains("armature")) continue;

            if (Cocok(n, kataTangan)) calonTangan.Add(t);
            else if (Cocok(n, kataKaki)) calonKaki.Add(t);
            else if (Cocok(n, kataAntena))
            {
                if (antena == null || Kedalaman(t, bug) < Kedalaman(antena, bug)) antena = t;
            }

            if (Cocok(n, kataMata))
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null) m.mata.Add(r);
            }
        }

        TambahSisi(m, calonTangan, Jenis.Tangan, pusat, lateral, tinggi);
        TambahSisi(m, calonKaki, Jenis.Kaki, pusat, lateral, tinggi);
        if (antena != null && antena.GetComponent<SkinnedMeshRenderer>() == null)
            m.sendi.Add(new Sendi { t = antena, jenis = Jenis.Antena, sisi = 0 });

        // Laporan ke Console
        StringBuilder sb = new StringBuilder();
        sb.Append("[BugTengil] ").Append(bug.name).Append(" -> ");
        foreach (Sendi s in m.sendi)
            sb.Append(s.jenis).Append(s.sisi != 0 ? (s.sisi > 0 ? "(+)" : "(-)") : "").Append("=").Append(s.t.name).Append("  ");
        sb.Append("| mata ditemukan: ").Append(m.mata.Count);
        Debug.Log(sb.ToString());

        bool adaTangan = false;
        foreach (Sendi s in m.sendi) if (s.jenis == Jenis.Tangan) adaTangan = true;

        if (cetakStrukturKeConsole || !adaTangan)
        {
            StringBuilder st = new StringBuilder();
            int sisa = 250;
            CetakStruktur(bug, st, 0, ref sisa);
            if (!adaTangan)
                Debug.LogWarning("[BugTengil] Tangan TIDAK ketemu di " + bug.name +
                                 ". Cek nama bagian di struktur ini, lalu sesuaikan 'Nama bagian' di Inspector:\n" + st);
            else
                Debug.Log("[BugTengil] Struktur " + bug.name + ":\n" + st);
        }

        cache[bug] = m;
        return m;
    }

    private void TambahSisi(Model m, List<Transform> calon, Jenis jenis, Vector3 pusat, Vector3 lateral, float tinggi)
    {
        Transform kanan = null, kiri = null;
        int dKanan = int.MaxValue, dKiri = int.MaxValue;

        foreach (Transform t in calon)
        {
            if (t.GetComponent<SkinnedMeshRenderer>() != null) continue;

            float dx = Vector3.Dot(TitikPusat(t) - pusat, lateral);
            if (Mathf.Abs(dx) < 0.04f * tinggi) continue;   // wadah di tengah badan, bukan satu sisi

            int d = Kedalaman(t, m.root);
            if (dx > 0f) { if (d < dKanan) { dKanan = d; kanan = t; } }
            else { if (d < dKiri) { dKiri = d; kiri = t; } }
        }

        if (kanan != null) m.sendi.Add(new Sendi { t = kanan, jenis = jenis, sisi = 1 });
        if (kiri != null) m.sendi.Add(new Sendi { t = kiri, jenis = jenis, sisi = -1 });
    }

    // Catat pose awal & titik engsel sebuah sendi (dipanggil tiap animasi mulai)
    private void Tangkap(Sendi s, Model m)
    {
        Transform t = s.t;
        Transform induk = t.parent;

        s.posLokal0 = t.localPosition;
        s.rotLokal0 = t.localRotation;
        s.rotInduk0 = induk != null ? induk.rotation : Quaternion.identity;

        float tinggi = m.bounds.size.y;
        Vector3 keluar = m.lateral * s.sisi;
        Vector3 pivot, ujung;

        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
        {
            // Bagian berupa mesh terpisah: engsel di pinggir yang dekat badan
            Bounds b = r.bounds;
            if (s.jenis == Jenis.Kaki)
            {
                pivot = b.center + Vector3.up * b.extents.y;
                ujung = b.center - Vector3.up * b.extents.y;
            }
            else if (s.jenis == Jenis.Antena)
            {
                pivot = b.center - Vector3.up * b.extents.y;
                ujung = b.center + Vector3.up * b.extents.y;
            }
            else
            {
                Vector3 keBadan = m.bounds.center - b.center;
                keBadan.y = 0f;
                keBadan = keBadan.sqrMagnitude < 1e-6f ? -keluar : keBadan.normalized;
                pivot = b.center + keBadan * Mathf.Min(b.extents.x, b.extents.z) + Vector3.up * b.extents.y * 0.7f;
                ujung = 2f * b.center - pivot;
            }
        }
        else
        {
            // Bagian berupa tulang (bone): engsel di posisi tulang, ujung = tulang paling ujung
            pivot = t.position;
            Transform c = t;
            while (c.childCount > 0) c = c.GetChild(0);
            ujung = c.position;
            if ((ujung - pivot).sqrMagnitude < 1e-8f)
            {
                Vector3 arahCadangan = s.jenis == Jenis.Antena ? Vector3.up : (keluar * 0.5f + Vector3.down);
                ujung = pivot + arahCadangan * tinggi * 0.1f;
            }
        }

        Vector3 d = ujung - pivot;
        s.arah0 = d.sqrMagnitude < 1e-8f ? (keluar + Vector3.down).normalized : d.normalized;
        s.pivotLokal = induk != null ? induk.InverseTransformPoint(pivot) : pivot;
    }

    // Rotasi tangan: dari arah awal menuju arah target, ditambah kibasan di bidang yang menghadap kamera
    private Quaternion TanganDunia(Model m, Sendi s, Vector3 target, float tahap, float goyang)
    {
        Quaternion naik = Quaternion.FromToRotation(s.arah0, target);
        Quaternion q = Quaternion.AngleAxis(goyang, m.camMaju) * naik;
        return Quaternion.Slerp(Quaternion.identity, q, tahap);
    }

    // qDunia = rotasi (di ruang dunia, saat awal) yang diputar di sekitar titik engsel sendi
    private void TerapkanSendi(Sendi s, Quaternion qDunia)
    {
        Quaternion ql = Quaternion.Inverse(s.rotInduk0) * qDunia * s.rotInduk0;
        s.t.localRotation = ql * s.rotLokal0;
        s.t.localPosition = s.pivotLokal + ql * (s.posLokal0 - s.pivotLokal);
    }

    // =====================================================================
    //  Mata X
    // =====================================================================

    private void BuatMataX(Model m)
    {
        float tinggi = m.bounds.size.y;
        float lebar = Mathf.Max(m.bounds.size.x, m.bounds.size.z);
        float ukuran = ukuranMataX * tinggi;
        Vector3 keKamera = -m.jauh;

        List<Vector3> posisi = new List<Vector3>();

        if (m.mata.Count >= 2)
        {
            // Dua mata asli: X dipasang di tempat masing-masing, mata asli disembunyikan
            for (int i = 0; i < 2; i++)
            {
                Renderer r = m.mata[i];
                Bounds b = r.bounds;
                float geser = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)) + 0.01f * tinggi;
                posisi.Add(b.center + keKamera * geser);
            }
            foreach (Renderer r in m.mata) r.enabled = false;
        }
        else if (m.mata.Count == 1)
        {
            Renderer r = m.mata[0];
            Bounds b = r.bounds;
            float geser = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)) + 0.01f * tinggi;
            Vector3 c = b.center + keKamera * geser;
            posisi.Add(c - m.camKanan * jarakAntarMata * lebar);
            posisi.Add(c + m.camKanan * jarakAntarMata * lebar);
            r.enabled = false;
        }
        else
        {
            // Mata tidak ketemu (mungkin bagian dari tekstur): X ditaruh di perkiraan posisi wajah
            Vector3 c = new Vector3(m.bounds.center.x, m.bounds.min.y + tinggiMata * tinggi, m.bounds.center.z)
                        + keKamera * (jarakDepanWajah * lebar);
            posisi.Add(c - m.camKanan * jarakAntarMata * lebar);
            posisi.Add(c + m.camKanan * jarakAntarMata * lebar);
        }

        if (meshX == null) meshX = BuatMeshX();
        m.materialX = KopiMaterial(m.root, warnaMataX);

        Quaternion rot = Quaternion.LookRotation(m.jauh, Vector3.up);   // sisi depan X menghadap kamera
        foreach (Vector3 p in posisi)
        {
            GameObject go = new GameObject("MataX", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = meshX;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m.materialX;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            go.transform.position = p;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * ukuran;
            go.transform.SetParent(m.root, true);   // ikut jatuh bersama badan

            m.mataX.Add(go);
        }
    }

    // Huruf X dari dua batang pipih (dua sisi, tanpa perlu collider/physics)
    private static Mesh BuatMeshX()
    {
        List<Vector3> v = new List<Vector3>();
        List<Vector3> n = new List<Vector3>();
        List<int> tri = new List<int>();

        TambahBatang(v, n, tri, 45f);
        TambahBatang(v, n, tri, -45f);

        Mesh mesh = new Mesh();
        mesh.name = "MeshX";
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void TambahBatang(List<Vector3> v, List<Vector3> n, List<int> tri, float sudut)
    {
        const float setengahPanjang = 0.62f;
        const float setengahTebal = 0.11f;
        Quaternion q = Quaternion.Euler(0f, 0f, sudut);
        Vector3[] sudutLokal =
        {
            new Vector3(-setengahPanjang, -setengahTebal, 0f),
            new Vector3(-setengahPanjang,  setengahTebal, 0f),
            new Vector3( setengahPanjang,  setengahTebal, 0f),
            new Vector3( setengahPanjang, -setengahTebal, 0f),
        };

        // sisi depan (menghadap -Z)
        int a = v.Count;
        for (int i = 0; i < 4; i++) { v.Add(q * sudutLokal[i]); n.Add(Vector3.back); }
        tri.AddRange(new[] { a, a + 1, a + 2, a, a + 2, a + 3 });

        // sisi belakang (menghadap +Z)
        int b = v.Count;
        for (int i = 0; i < 4; i++) { v.Add(q * sudutLokal[i]); n.Add(Vector3.forward); }
        tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
    }

    // Salin material milik bug (pasti ikut ter-build), lalu warnai
    private Material KopiMaterial(Transform bug, Color warna)
    {
        Material mat;
        Renderer sumber = bug.GetComponentInChildren<Renderer>();
        if (sumber != null && sumber.sharedMaterial != null)
        {
            mat = new Material(sumber.sharedMaterial);
        }
        else
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh);
        }

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warna);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", warna);
        return mat;
    }

    // =====================================================================
    //  Helper
    // =====================================================================

    private static bool Cocok(string nama, string[] kata)
    {
        if (kata == null) return false;
        foreach (string k in kata)
        {
            if (!string.IsNullOrEmpty(k) && nama.Contains(k.ToLowerInvariant())) return true;
        }
        return false;
    }

    private static int Kedalaman(Transform t, Transform root)
    {
        int d = 0;
        while (t != null && t != root)
        {
            d++;
            t = t.parent;
        }
        return d;
    }

    private static Vector3 TitikPusat(Transform t)
    {
        Renderer r = t.GetComponent<Renderer>();
        return r != null ? r.bounds.center : t.position;
    }

    private bool AmbilBounds(Transform bug, out Bounds hasil)
    {
        hasil = new Bounds();
        bool ada = false;
        foreach (Renderer r in bug.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled) continue;
            if (!ada) { hasil = r.bounds; ada = true; }
            else hasil.Encapsulate(r.bounds);
        }
        return ada;
    }

    private static void CetakStruktur(Transform t, StringBuilder sb, int dalam, ref int sisa)
    {
        if (sisa <= 0) return;
        sisa--;

        sb.Append(' ', dalam * 2).Append(t.name);
        Renderer r = t.GetComponent<Renderer>();
        if (r != null) sb.Append(r is SkinnedMeshRenderer ? "  [skinned]" : "  [mesh]");
        sb.Append('\n');

        for (int i = 0; i < t.childCount; i++)
            CetakStruktur(t.GetChild(i), sb, dalam + 1, ref sisa);
    }

    // =====================================================================
    //  Bubble chat (kecil, muncul di atas kepala bug, hilang bersama animasinya)
    // =====================================================================

    private void TampilkanBubble(string pesan, Transform bug)
    {
        if (bubbleRT == null) return;

        Bounds b;
        bubbleTargetDunia = bug.position;
        if (AmbilBounds(bug, out b))
            bubbleTargetDunia = b.center + Vector3.up * b.extents.y;

        if (bubbleCo != null) StopCoroutine(bubbleCo);
        bubbleCo = StartCoroutine(BubbleCo(pesan));
    }

    private void SembunyikanBubble()
    {
        if (bubbleCo != null)
        {
            StopCoroutine(bubbleCo);
            bubbleCo = null;
        }
        if (bubbleRT != null) bubbleRT.gameObject.SetActive(false);
    }

    private IEnumerator BubbleCo(string pesan)
    {
        bubbleTeks.text = pesan;
        bubbleRT.gameObject.SetActive(true);
        bubbleRT.SetAsLastSibling();

        const float lamaPop = 0.2f;
        float t = 0f;
        while (true)   // bubble tetap tampil sampai SembunyikanBubble() dipanggil
        {
            t += Time.deltaTime;
            PosisikanBubble();
            float s = t < lamaPop ? EaseOutBack(t / lamaPop) : 1f;
            bubbleRT.localScale = Vector3.one * s;
            yield return null;
        }
    }

    private void PosisikanBubble()
    {
        Vector2 setengah = canvasRT.rect.size * 0.5f;
        Vector2 pos = new Vector2(0f, setengah.y - TinggiBubble - marginTepi - 20f);

        Vector3 sp = kamera.WorldToScreenPoint(bubbleTargetDunia);
        if (sp.z > 0f)
        {
            Vector2 lokal;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, kameraUI, out lokal))
                pos = lokal - canvasRT.rect.center + Vector2.up * jarakAtasBug;
        }

        pos.x = Mathf.Clamp(pos.x, -setengah.x + LebarBubble * 0.5f + marginTepi, setengah.x - LebarBubble * 0.5f - marginTepi);
        pos.y = Mathf.Clamp(pos.y, -setengah.y + marginTepi, setengah.y - TinggiBubble - marginTepi);
        bubbleRT.anchoredPosition = pos;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void BangunBubble()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) c = FindFirstObjectByType<Canvas>();
        if (c == null)
        {
            Debug.LogError("BugTengil: Canvas tidak ditemukan, bubble tidak dibuat.");
            return;
        }

        canvas = c.rootCanvas;
        canvasRT = (RectTransform)canvas.transform;
        kameraUI = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Color garis = new Color32(24, 64, 115, 255);
        Color isiBubble = Color.white;
        Sprite bulat = BuatSpriteBulat(22);

        bubbleRT = Rect("BubbleBug", canvasRT);
        bubbleRT.anchorMin = bubbleRT.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRT.pivot = new Vector2(0.5f, 0f);      // ujung bawah (ekor) = titik acuan
        bubbleRT.sizeDelta = new Vector2(LebarBubble, TinggiBubble);

        CanvasGroup cg = bubbleRT.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform ekorGelap = Rect("EkorGaris", bubbleRT);
        TitikBawah(ekorGelap, 30f, 22f);
        Gambar(ekorGelap, null, garis);

        RectTransform badan = Rect("BadanGaris", bubbleRT);
        badan.anchorMin = Vector2.zero;
        badan.anchorMax = Vector2.one;
        badan.offsetMin = new Vector2(0f, 22f);
        badan.offsetMax = Vector2.zero;
        Gambar(badan, bulat, garis);

        RectTransform isi = Rect("BadanIsi", badan);
        isi.anchorMin = Vector2.zero;
        isi.anchorMax = Vector2.one;
        isi.offsetMin = new Vector2(4f, 4f);
        isi.offsetMax = new Vector2(-4f, -4f);
        Gambar(isi, bulat, isiBubble);

        RectTransform ekorPutih = Rect("EkorIsi", bubbleRT);
        TitikBawah(ekorPutih, 24f, 26f);
        Gambar(ekorPutih, null, isiBubble);

        RectTransform teks = Rect("Teks", isi);
        teks.anchorMin = Vector2.zero;
        teks.anchorMax = Vector2.one;
        teks.offsetMin = new Vector2(16f, 8f);
        teks.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI t = teks.gameObject.AddComponent<TextMeshProUGUI>();
        t.fontStyle = FontStyles.Bold;
        t.color = garis;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 18f;
        t.fontSizeMax = 30f;
        t.raycastTarget = false;
        bubbleTeks = t;

        bubbleRT.gameObject.SetActive(false);
    }

    private static void TitikBawah(RectTransform rt, float ukuran, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ukuran, ukuran);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static RectTransform Rect(string nama, Transform parent)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image Gambar(RectTransform rt, Sprite sprite, Color warna)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = warna;
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    // Sprite kotak sudut bulat (9-slice) dibuat lewat kode
    private static Sprite BuatSpriteBulat(int radius)
    {
        int size = radius * 2 + 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect,
                             new Vector4(radius, radius, radius, radius));
    }
}