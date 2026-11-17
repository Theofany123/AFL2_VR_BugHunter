using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Memindahkan ke scene lain. Dipanggil dari On Click() pada tombol.
/// Taruh di: Assets/SCIPT/UI/
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // Isi namaScene persis sama dengan nama file scene (huruf besar/kecil berpengaruh)
    public void PindahScene(string namaScene)
    {
        SceneManager.LoadScene(namaScene);
    }
}