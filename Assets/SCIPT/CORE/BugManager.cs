using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Satu bug beserta soal yang harus dijawab untuk mengalahkannya.
/// </summary>
[System.Serializable]
public class BugEntry
{
    public GameObject bug;

    [Tooltip("Nomor soal pertama untuk bug ini. 0 = soal ke-1, 2 = soal ke-3, 4 = soal ke-5.")]
    public int soalMulai;

    [Tooltip("Jumlah soal yang harus dijawab untuk mengalahkan bug ini.")]
    public int jumlahSoal = 2;
}

/// <summary>
/// Mengatur 3 babak bug:
/// 1. Hanya satu bug yang tampil. Pemain men-tap bug itu -> kuis terbuka.
/// 2. Semua soal bug itu benar -> bug mengecil & hilang -> bug berikutnya muncul.
/// 3. Gagal (nyawa habis) -> kembali ke bug pertama dan kuis pertama.
/// 4. Bug terakhir kalah -> event "On Semua Bug Selesai" (misalnya pindah ke Penutup).
///
/// Taruh di: Assets/SCIPT/CORE/
/// Pasang ke objek "QUIZ" (yang sama dengan script QUIZ).
/// Hubungkan tap layar: Plane Finder > Anchor Input Listener Behaviour >
/// On Input Received Event > tambah QUIZ > BugManager.SaatLayarDitap
/// </summary>
public class BugManager : MonoBehaviour
{
    [Header("Daftar bug (urut dari bug pertama)")]
    public List<BugEntry> daftarBug = new List<BugEntry>();

    [Header("Pengaturan")]
    [Tooltip("Lama animasi bug mengecil saat terbunuh.")]
    public float durasiMati = 0.5f;

    [Tooltip("Jeda setelah satu bug hilang sebelum bug berikutnya muncul.")]
    public float jedaBugBerikutnya = 0.6f;

    [Tooltip("Area tap diperluas sekian pixel supaya bug mudah disentuh.")]
    public float paddingTap = 60f;

    [Tooltip("Untuk tes: kalau dicentang, tap di mana saja di layar membuka kuis (tidak harus kena bug).")]
    public bool tapDimanaSaja = false;

    [Header("Event")]
    public UnityEvent onBugMati;            // dipanggil tiap kali satu bug kalah
    public UnityEvent onGagal;              // dipanggil saat pemain gagal dan kembali ke bug pertama
    public UnityEvent onSemuaBugSelesai;    // dipanggil setelah bug terakhir kalah

    private QUIZ quiz;
    private Camera kamera;
    private int indexBug;
    private bool kuisJalan;
    private bool sedangPindah;
    private Renderer[] renderers;

    // [BARU] Dipakai BugTengil.cs untuk tahu bug mana yang sedang tampil
    public GameObject BugAktif => daftarBug[indexBug].bug;

    private BugTengil tengil;

    private void Start()
    {
        quiz = FindFirstObjectByType<QUIZ>();
        kamera = Camera.main;
        if (kamera == null) kamera = FindFirstObjectByType<Camera>();

        bool ada = quiz != null && kamera != null && daftarBug.Count > 0;
        foreach (BugEntry e in daftarBug)
        {
            if (e.bug == null) ada = false;
        }

        if (!ada)
        {
            Debug.LogError("BugManager berhenti. QUIZ ditemukan: " + (quiz != null) +
                           ", kamera ditemukan: " + (kamera != null) +
                           ", jumlah bug: " + daftarBug.Count +
                           ". Pastikan semua kolom Bug di Daftar Bug sudah terisi.");
            enabled = false;
            return;
        }

        // Gagal = kuis ditutup, lalu BugManager yang mengatur ulang dari bug pertama
        quiz.ulangOtomatisSaatGagal = false;
        quiz.onQuizSelesai.AddListener(SaatKuisSelesai);
        quiz.onQuizGagal.AddListener(SaatKuisGagal);

        tengil = FindFirstObjectByType<BugTengil>();

        // Semua bug disembunyikan dulu, hanya bug pertama yang muncul
        foreach (BugEntry e in daftarBug) e.bug.SetActive(false);
        TampilkanBug(0);
        Debug.LogWarning("[BugManager] Siap. Jumlah bug: " + daftarBug.Count);
    }

    private void OnDestroy()
    {
        if (quiz == null) return;
        quiz.onQuizSelesai.RemoveListener(SaatKuisSelesai);
        quiz.onQuizGagal.RemoveListener(SaatKuisGagal);
    }

    // ---------- Tap layar ----------

    // Dipanggil dari Anchor Input Listener Behaviour (Vector2 = posisi tap di layar, dalam pixel)
    public void SaatLayarDitap(Vector2 posisiLayar)
    {
        Debug.LogWarning("[BugManager] Tap diterima di " + posisiLayar);

        if (kuisJalan || sedangPindah || quiz.SedangTerbuka)
        {
            Debug.LogWarning("[BugManager] Tap diabaikan (kuis sedang jalan atau bug sedang berganti).");
            return;
        }

        bool kena = tapDimanaSaja || TerkenaBug(posisiLayar);
        Debug.LogWarning("[BugManager] Kena bug: " + kena);
        if (!kena) return;

        MulaiKuis();
    }

    // Juga bisa dipanggil manual untuk tes: klik kanan judul komponen di Inspector saat Play
    [ContextMenu("Tes: Buka Kuis Bug Saat Ini")]
    public void MulaiKuis()
    {
        if (kuisJalan || sedangPindah) return;

        kuisJalan = true;
        BugEntry e = daftarBug[indexBug];
        quiz.BukaQuiz(e.soalMulai, e.jumlahSoal);
    }

    private bool TerkenaBug(Vector2 posisiLayar)
    {
        bool adaRenderer = false;
        Bounds gabungan = new Bounds();

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;

            if (!adaRenderer)
            {
                gabungan = r.bounds;
                adaRenderer = true;
            }
            else
            {
                gabungan.Encapsulate(r.bounds);
            }
        }
        if (!adaRenderer) return false;

        // Proyeksikan 8 sudut kotak pembatas bug ke layar, lalu cek apakah tap ada di dalamnya
        Vector3 c = gabungan.center;
        Vector3 e = gabungan.extents;
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        bool adaTitik = false;

        for (int i = 0; i < 8; i++)
        {
            Vector3 sudut = c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);

            Vector3 p = kamera.WorldToScreenPoint(sudut);
            if (p.z <= 0f) continue;   // di belakang kamera

            adaTitik = true;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }
        if (!adaTitik) return false;

        Rect area = Rect.MinMaxRect(minX - paddingTap, minY - paddingTap,
                                    maxX + paddingTap, maxY + paddingTap);
        return area.Contains(posisiLayar);
    }

    // ---------- Alur babak ----------

    private void TampilkanBug(int index)
    {
        indexBug = index;
        GameObject bug = daftarBug[index].bug;
        bug.SetActive(true);

        renderers = bug.GetComponentsInChildren<Renderer>(true);
        kuisJalan = false;
    }

    private void SaatKuisSelesai()
    {
        if (!kuisJalan) return;
        StartCoroutine(BugMatiLaluLanjut());
    }

    private void SaatKuisGagal()
    {
        if (!kuisJalan) return;
        kuisJalan = false;

        // Kembali ke babak pertama: bug pertama, kuis pertama
        daftarBug[indexBug].bug.SetActive(false);
        onGagal.Invoke();
        TampilkanBug(0);
    }

    private IEnumerator BugMatiLaluLanjut()
    {
        sedangPindah = true;
        kuisJalan = false;

        GameObject bug = daftarBug[indexBug].bug;

        if (tengil != null)
        {
            // Bug jatuh telentang dengan mata X, berkedip, lalu hilang (tanpa mengecil / zoom)
            yield return StartCoroutine(tengil.AnimasiMati(bug));
        }
        else
        {
            // Cadangan kalau BugTengil tidak terpasang: bug mengecil
            Transform t = bug.transform;
            Vector3 skalaAwal = t.localScale;
            float waktu = 0f;

            while (waktu < durasiMati)
            {
                waktu += Time.deltaTime;
                t.localScale = Vector3.Lerp(skalaAwal, Vector3.zero, waktu / durasiMati);
                yield return null;
            }
            t.localScale = skalaAwal;
        }

        bug.SetActive(false);
        onBugMati.Invoke();

        // Bug terakhir sudah kalah
        if (indexBug + 1 >= daftarBug.Count)
        {
            onSemuaBugSelesai.Invoke();
            yield break;
        }

        // Munculkan bug berikutnya (menunggu di-tap)
        yield return new WaitForSeconds(jedaBugBerikutnya);
        TampilkanBug(indexBug + 1);
        sedangPindah = false;
    }
}