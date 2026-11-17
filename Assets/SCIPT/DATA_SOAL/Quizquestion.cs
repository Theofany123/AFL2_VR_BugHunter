using System;
using UnityEngine;

/// <summary>
/// Model satu soal. Hanya berisi data, tanpa logika.
/// Taruh di: Assets/SCIPT/DATA_SOAL/
/// </summary>
[Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string pertanyaan;

    // Isi 4 pilihan (tanpa huruf A/B/C/D, huruf ditambahkan otomatis oleh QUIZ.cs)
    public string[] pilihan = new string[4];

    // Index jawaban benar: 0 = A, 1 = B, 2 = C, 3 = D
    [Range(0, 3)]
    public int jawabanBenar;

    public QuizQuestion(string pertanyaan, string[] pilihan, int jawabanBenar)
    {
        this.pertanyaan = pertanyaan;
        this.pilihan = pilihan;
        this.jawabanBenar = jawabanBenar;
    }
}