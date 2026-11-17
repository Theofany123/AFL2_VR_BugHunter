using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengatur nyawa pemain dan tampilan ikon hati.
/// Tidak tahu apa-apa soal kuis, hanya dipanggil oleh QUIZ.cs.
/// Taruh di: Assets/SCIPT/CORE/
/// </summary>
public class nyawa : MonoBehaviour
{
    [Header("Pengaturan")]
    public int nyawaMaksimal = 2;

    [Header("UI Hati (urutkan dari kiri ke kanan)")]
    public Image[] ikonHati;

    [Tooltip("Opsional. Kosongkan jika hati yang hilang cukup disembunyikan.")]
    public Sprite hatiPenuh;
    public Sprite hatiKosong;

    public int NyawaSekarang { get; private set; }

    private void Awake()
    {
        ResetNyawa();
    }

    public void KurangiNyawa()
    {
        if (NyawaSekarang <= 0) return;

        NyawaSekarang--;
        PerbaruiTampilan();
    }

    public void ResetNyawa()
    {
        NyawaSekarang = nyawaMaksimal;
        PerbaruiTampilan();
    }

    private void PerbaruiTampilan()
    {
        if (ikonHati == null) return;

        for (int i = 0; i < ikonHati.Length; i++)
        {
            if (ikonHati[i] == null) continue;

            bool masihAda = i < NyawaSekarang;

            if (hatiPenuh != null && hatiKosong != null)
            {
                ikonHati[i].enabled = true;
                ikonHati[i].sprite = masihAda ? hatiPenuh : hatiKosong;
            }
            else
            {
                ikonHati[i].enabled = masihAda;
            }
        }
    }
}