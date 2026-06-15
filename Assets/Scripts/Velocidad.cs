using TMPro;
using UnityEngine;

public class Velocidad : MonoBehaviour
{

    public Rigidbody coche;
    public TextMeshProUGUI textoVelocidad;

    void Update()
    {
        float velocidad = coche.linearVelocity.magnitude * 3.6f;
        textoVelocidad.text = Mathf.RoundToInt(velocidad) + " KM/H";
    }
}
