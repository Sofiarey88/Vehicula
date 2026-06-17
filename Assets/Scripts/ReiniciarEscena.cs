using UnityEngine;
using UnityEngine.SceneManagement;

public class ReiniciarEscena : MonoBehaviour
{
    public void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
