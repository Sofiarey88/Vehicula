using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject panelMenu;

    void Start()
    {
        panelMenu.SetActive(true);
    }

    public void Jugar()
    {
        panelMenu.SetActive(false);
    }
}