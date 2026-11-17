using System.Collections;
using UnityEngine;

/// <summary>
/// Memutar satu suara (misalnya "YEAY") sekali saat scene ini terbuka.
/// Taruh di: Assets/SCIPT/CORE/
/// Pasang ke GameObject kosong bernama "SUARA" di scene Penutup.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SuaraPenutup : MonoBehaviour
{
    [Header("Suara")]
    public AudioClip suara;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Pengaturan")]
    [Tooltip("Jeda sebelum suara bunyi (detik). 0 = langsung saat scene terbuka.")]
    public float jeda = 0f;

    private IEnumerator Start()
    {
        if (suara == null) yield break;

        AudioSource sumber = GetComponent<AudioSource>();
        sumber.playOnAwake = false;
        sumber.loop = false;

        if (jeda > 0f) yield return new WaitForSeconds(jeda);

        sumber.PlayOneShot(suara, volume);
    }
}