// using UnityEngine;
// using UnityEngine.EventSystems;
// #if ENABLE_INPUT_SYSTEM
// using UnityEngine.InputSystem;
// #endif

// /// <summary>
// /// Menampilkan hantu satu per satu.
// /// Awal: hantu pertama muncul, yang lain hilang.
// /// Setiap layar disentuh: hantu berikutnya muncul, yang sebelumnya hilang.
// /// Pasang di GameObject "GhostManager" (Empty) di scene ARGame.
// /// </summary>
// public class GhostSequence : MonoBehaviour
// {
//     [Header("Isi dengan 4 hantu, urut dari yang pertama")]
//     public GameObject[] ghosts;

//     [Header("Pengaturan")]
//     [Tooltip("Jika dicentang, setelah hantu terakhir kembali ke hantu pertama")]
//     public bool loop = false;

//     [Tooltip("Abaikan sentuhan yang mengenai tombol UI")]
//     public bool ignoreUITouch = true;

//     int current = 0;
//     bool stageReady = false;

//     void Start()
//     {
//         ShowGhost(0);
//     }

//     void Update()
//     {
//         if (ghosts == null || ghosts.Length == 0) return;

//         // Tunggu sampai Ground Plane Stage aktif (lantai sudah terdeteksi
//         // dan objek sudah diletakkan). Sentuhan yang dipakai untuk meletakkan
//         // stage tidak dihitung sebagai "hantu berikutnya".
//         if (!ghosts[current].activeInHierarchy)
//         {
//             stageReady = false;
//             return;
//         }
//         if (!stageReady)
//         {
//             stageReady = true;
//             return;
//         }

//         if (TapThisFrame())
//         {
//             if (ignoreUITouch && EventSystem.current != null &&
//                 EventSystem.current.IsPointerOverGameObject())
//                 return;

//             NextGhost();
//         }
//     }

//     /// <summary>Tampilkan hantu berikutnya. Bisa juga dipanggil dari tombol UI.</summary>
//     public void NextGhost()
//     {
//         int next = current + 1;

//         if (next >= ghosts.Length)
//         {
//             if (!loop) return; // berhenti di hantu terakhir
//             next = 0;
//         }

//         ShowGhost(next);
//     }

//     /// <summary>Kembali ke hantu pertama (misalnya untuk tombol Ulangi).</summary>
//     public void ResetSequence()
//     {
//         ShowGhost(0);
//     }

//     void ShowGhost(int index)
//     {
//         current = index;

//         for (int i = 0; i < ghosts.Length; i++)
//         {
//             if (ghosts[i] != null)
//                 ghosts[i].SetActive(i == index);
//         }
//     }

//     bool TapThisFrame()
//     {
// #if ENABLE_INPUT_SYSTEM
//         if (Touchscreen.current != null &&
//             Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
//             return true;
//         if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//             return true;
//         return false;
// #else
//         if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
//             return true;
//         return Input.GetMouseButtonDown(0);
// #endif
//     }
// }