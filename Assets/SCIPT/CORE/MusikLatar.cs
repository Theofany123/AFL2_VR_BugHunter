using UnityEngine;

/// <summary>
/// Memutar musik latar saat kamera AR terbuka.
/// Pasang ke GameObject kosong bernama "MUSIK".
/// Hubungkan MulaiMusik() ke InstruksiUI > On Mulai.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusikLatar : MonoBehaviour
{
    [Header("Musik")]
    public AudioClip musik;
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("Pengaturan")]
    [Tooltip("Centang jika musik harus langsung main saat scene dimulai (tanpa layar instruksi).")]
    public bool mainOtomatisSaatStart = false;

    [Tooltip("Lama musik membesar pelan-pelan saat mulai (detik). 0 = langsung.")]
    public float durasiFadeIn = 1.5f;

    private AudioSource sumber;
    private float waktuFade;
    private bool sedangFade;

    private void Awake()
    {
        sumber = GetComponent<AudioSource>();
        sumber.clip = musik;
        sumber.loop = true;
        sumber.playOnAwake = false;
        sumber.volume = 0f;
    }

    private void Start()
    {
        if (mainOtomatisSaatStart) MulaiMusik();
    }

    // Hubungkan ke InstruksiUI > On Mulai ()
    public void MulaiMusik()
    {
        if (sumber.isPlaying) return;

        sumber.volume = durasiFadeIn > 0f ? 0f : volume;
        sumber.Play();
        waktuFade = 0f;
        sedangFade = durasiFadeIn > 0f;
    }

    public void HentikanMusik()
    {
        sumber.Stop();
    }

    public void AturVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        sumber.volume = volume;
    }

    private void Update()
    {
        if (!sedangFade) return;

        waktuFade += Time.deltaTime;
        sumber.volume = Mathf.Lerp(0f, volume, waktuFade / durasiFadeIn);
        if (waktuFade >= durasiFadeIn) sedangFade = false;
    }
}