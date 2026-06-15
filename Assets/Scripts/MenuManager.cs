using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject panelMenu;

    void Start()
    {
        panelMenu.SetActive(true);
        Time.timeScale = 0f; // Juego pausado hasta que el jugador pulse Jugar
    }

    public void Jugar()
    {
        panelMenu.SetActive(false);
        Time.timeScale = 1f; // Reanuda la simulación
    }
}