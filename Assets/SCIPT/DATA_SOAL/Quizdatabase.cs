using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tempat menyimpan semua soal. Bisa diedit lewat Inspector.
/// Taruh di: Assets/SCIPT/DATA_SOAL/
/// </summary>
public class QuizDatabase : MonoBehaviour
{
    public List<QuizQuestion> daftarSoal = new List<QuizQuestion>();

    public int JumlahSoal => daftarSoal.Count;

    public QuizQuestion AmbilSoal(int index)
    {
        if (index < 0 || index >= daftarSoal.Count)
        {
            Debug.LogError("QuizDatabase: index soal di luar jangkauan: " + index);
            return null;
        }
        return daftarSoal[index];
    }

    // Kalau daftar soal masih kosong saat Play, otomatis diisi soal contoh
    private void Awake()
    {
        if (daftarSoal == null || daftarSoal.Count == 0) IsiSoalContoh();
    }

    // Otomatis terisi saat script pertama kali dipasang ke GameObject
    private void Reset()
    {
        IsiSoalContoh();
    }

    // Klik kanan judul komponen di Inspector > "Isi Soal Contoh" untuk mengisi ulang
    [ContextMenu("Isi Soal Contoh")]
    private void IsiSoalContoh()
    {
        daftarSoal = new List<QuizQuestion>
        {
            new QuizQuestion(
                "Apa fungsi dari hard disk?",
                new[] { "Mengolah data", "Menyimpan data", "Menampilkan gambar di layar", "Mengetik huruf" },
                1),

            new QuizQuestion(
                "Kompleksitas waktu yang tidak berubah saat n bertambah disebut...",
                new[] { "O(n)", "O(1)", "O(log n)", "O(n²)" },
                1),

            new QuizQuestion(
                "Komponen yang hanya dimiliki graph search tetapi tidak dimiliki tree search adalah...",
                new[] { "Goal test", "Initial state", "Explored set", "Frontier" },
                2),

            new QuizQuestion(
                "4 - 3 × (8 - 12) - 6 = ...",
                new[] { "13", "4", "10", "25" },
                2),

            new QuizQuestion(
                "Apa kepanjangan dari BIOS?",
                new[] { "Basic Input/Output System", "Basic Inbox/Outbox System", "Between Input/Output System", "Between Input/Output Service" },
                0),

            new QuizQuestion(
                "12,5% dari 104 adalah...",
                new[] { "13", "12", "15", "16" },
                0),
        };
    }
}